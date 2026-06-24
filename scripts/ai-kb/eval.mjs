import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const API = process.env.AI_HELPER_URL ?? 'http://localhost:5178';
const here = dirname(fileURLToPath(import.meta.url));

const lc = (value) => (value ?? '').toLowerCase();

async function ask(question, locale) {
  const res = await fetch(`${API}/api/v1/ai-helper/ask`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question, locale }),
  });
  if (!res.ok || !res.body) {
    throw new Error(`HTTP ${res.status}`);
  }

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let sources = [];
  let answer = '';

  for (;;) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }
    buffer += decoder.decode(value, { stream: true });
    let separator = buffer.indexOf('\n\n');
    while (separator !== -1) {
      const frame = buffer.slice(0, separator);
      buffer = buffer.slice(separator + 2);
      let event = 'message';
      let data = '';
      for (const line of frame.split('\n')) {
        if (line.startsWith('event:')) {
          event = line.slice(6).trim();
        } else if (line.startsWith('data:')) {
          data += line.slice(5).trim();
        }
      }
      if (data) {
        try {
          const payload = JSON.parse(data);
          if (event === 'sources' && Array.isArray(payload.sources)) {
            sources = payload.sources;
          } else if (event === 'token' && payload.text) {
            answer += payload.text;
          }
        } catch {
          // ignore non-JSON keepalive frames
        }
      }
      separator = buffer.indexOf('\n\n');
    }
  }

  return { sources, answer };
}

function firstMatchRank(sources, expectAny) {
  for (let i = 0; i < sources.length; i++) {
    const hay = `${lc(sources[i].title)} ${lc(sources[i].sourceRef)}`;
    if (expectAny.some((expected) => hay.includes(lc(expected)))) {
      return i + 1;
    }
  }
  return 0;
}

const golden = JSON.parse(await readFile(join(here, 'golden-eval.json'), 'utf8'));
const topK = golden.topK ?? 8;
let hits = 0;
let mrrSum = 0;
let forbiddenCount = 0;
let failed = 0;
let refusalPass = 0;
let refusalTotal = 0;

console.log(`AI Helper eval — ${golden.cases.length} cases @ ${API} (top-${topK})\n`);

for (const testCase of golden.cases) {
  let result;
  try {
    result = await ask(testCase.question, testCase.locale);
  } catch (error) {
    console.log(`x ${testCase.id}: request failed (${error.message})`);
    failed++;
    continue;
  }

  if (testCase.expectAnswerContains) {
    refusalTotal++;
    const answerLc = (result.answer ?? '').toLowerCase();
    const answerOk = testCase.expectAnswerContains.some((phrase) => answerLc.includes(phrase.toLowerCase()));
    const sourceLeak = firstMatchRank(result.sources, testCase.forbidAny ?? []) > 0;
    const passed = answerOk && !sourceLeak;
    if (passed) {
      refusalPass++;
    } else {
      failed++;
    }
    console.log(`${passed ? 'OK ' : 'XX '} ${testCase.id} [${testCase.locale}] refusal${sourceLeak ? ' SOURCE-LEAK' : ''}${answerOk ? '' : ' NO-REFUSAL-PHRASE'}`);
    console.log(`    answer: ${(result.answer ?? '').slice(0, 140)}`);
    continue;
  }

  const rank = firstMatchRank(result.sources, testCase.expectAny);
  const hit = rank > 0 && rank <= topK;
  const forbiddenRank = firstMatchRank(result.sources, testCase.forbidAny ?? []);
  const misleads = forbiddenRank > 0 && (rank === 0 || forbiddenRank < rank);
  if (hit) {
    hits++;
    mrrSum += 1 / rank;
  }
  if (misleads) {
    forbiddenCount++;
  }
  const ok = hit && !misleads;
  if (!ok) {
    failed++;
  }

  const titles = result.sources.slice(0, topK).map((source) => source.title).join(' | ');
  console.log(`${ok ? 'OK ' : 'XX '} ${testCase.id} [${testCase.locale}] rank=${rank || '-'}${misleads ? ` MISLEADING@${forbiddenRank}` : ''}`);
  console.log(`    sources: ${titles || '(none)'}`);
}

const retrievalTotal = golden.cases.length - refusalTotal;
console.log(
  `\nrecall@${topK}: ${hits}/${retrievalTotal} (${retrievalTotal > 0 ? ((100 * hits) / retrievalTotal).toFixed(0) : '0'}%)` +
    `  |  MRR: ${retrievalTotal > 0 ? (mrrSum / retrievalTotal).toFixed(3) : '0'}` +
    `  |  misleading-source cases: ${forbiddenCount}` +
    `  |  refusal cases: ${refusalPass}/${refusalTotal}` +
    `  |  failed: ${failed}`,
);

process.exit(failed > 0 ? 1 : 0);
