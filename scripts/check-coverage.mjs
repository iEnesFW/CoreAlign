#!/usr/bin/env node
import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join, resolve } from 'node:path';
import { argv, exit, env } from 'node:process';

const DEFAULT_THRESHOLD = 60;
const EXEMPT_LABEL = 'coverage-exempt';

function parseArgs(args) {
  const opts = { threshold: DEFAULT_THRESHOLD, path: null, label: null };
  for (let i = 0; i < args.length; i += 1) {
    const a = args[i];
    if (a === '--threshold' || a === '-t') {
      opts.threshold = Number(args[i + 1]);
      i += 1;
    } else if (a === '--path' || a === '-p') {
      opts.path = args[i + 1];
      i += 1;
    } else if (a === '--label' || a === '-l') {
      opts.label = args[i + 1];
      i += 1;
    } else if (a === '--help' || a === '-h') {
      printHelp();
      exit(0);
    }
  }
  if (Number.isNaN(opts.threshold) || opts.threshold < 0 || opts.threshold > 100) {
    console.error(
      `error: --threshold must be a number between 0 and 100 (got "${opts.threshold}")`,
    );
    exit(2);
  }
  return opts;
}

function printHelp() {
  console.log(`Usage: node scripts/check-coverage.mjs [options]

Options:
  -t, --threshold <pct>   Minimum line coverage percentage (default ${DEFAULT_THRESHOLD}).
  -p, --path <dir>        Root directory to scan for Cobertura XML reports
                          (default: ./TestResults).
  -l, --label <name>      Active PR labels (comma-separated). If "${EXEMPT_LABEL}"
                          is present the gate exits 0 with a warning.
  -h, --help              Show this help text.

Environment overrides:
  COVERAGE_THRESHOLD      Same as --threshold.
  COVERAGE_PATH           Same as --path.
  PR_LABELS               Same as --label.

Exit codes:
  0  Coverage meets threshold (or exempt label present).
  1  Coverage below threshold.
  2  Misconfiguration (no report found, bad args).
`);
}

function findCoberturaReports(root) {
  const reports = [];
  if (!existsSync(root)) return reports;
  const stack = [root];
  while (stack.length) {
    const current = stack.pop();
    let entries;
    try {
      entries = readdirSync(current);
    } catch {
      continue;
    }
    for (const entry of entries) {
      const full = join(current, entry);
      let s;
      try {
        s = statSync(full);
      } catch {
        continue;
      }
      if (s.isDirectory()) {
        stack.push(full);
      } else if (/cobertura.*\.xml$/i.test(entry) || /coverage\.cobertura\.xml$/i.test(entry)) {
        reports.push(full);
      }
    }
  }
  return reports;
}

function parseLineRate(xml) {
  const match = xml.match(/<coverage\b[^>]*\bline-rate="([0-9.]+)"/);
  if (!match) return null;
  const rate = Number(match[1]);
  if (Number.isNaN(rate)) return null;
  return rate;
}

function parseCounts(xml) {
  const valid = xml.match(/lines-valid="([0-9]+)"/);
  const covered = xml.match(/lines-covered="([0-9]+)"/);
  if (!valid || !covered) return null;
  return { valid: Number(valid[1]), covered: Number(covered[1]) };
}

function main() {
  const opts = parseArgs(argv.slice(2));
  if (env.COVERAGE_THRESHOLD) opts.threshold = Number(env.COVERAGE_THRESHOLD);
  if (env.COVERAGE_PATH) opts.path = env.COVERAGE_PATH;
  if (env.PR_LABELS) opts.label = env.PR_LABELS;

  if (
    opts.label &&
    opts.label
      .split(',')
      .map((s) => s.trim())
      .includes(EXEMPT_LABEL)
  ) {
    console.warn(`coverage gate skipped: "${EXEMPT_LABEL}" label present.`);
    exit(0);
  }

  const root = resolve(opts.path ?? 'TestResults');
  const reports = findCoberturaReports(root);
  if (reports.length === 0) {
    console.error(`error: no Cobertura coverage XML found under ${root}`);
    exit(2);
  }

  let totalValid = 0;
  let totalCovered = 0;
  let usedRateFallback = false;

  for (const reportPath of reports) {
    const xml = readFileSync(reportPath, 'utf8');
    const counts = parseCounts(xml);
    if (counts) {
      totalValid += counts.valid;
      totalCovered += counts.covered;
      continue;
    }
    const rate = parseLineRate(xml);
    if (rate === null) {
      console.error(`error: ${reportPath} is not a recognised Cobertura report`);
      exit(2);
    }
    usedRateFallback = true;
    totalValid += 100;
    totalCovered += Math.round(rate * 100);
  }

  if (totalValid === 0) {
    console.error('error: cobertura report parsed but no line counts were found');
    exit(2);
  }

  const percent = (totalCovered / totalValid) * 100;
  const display = percent.toFixed(2);
  console.log(
    `coverage: ${display}% (${totalCovered}/${totalValid} lines) across ${reports.length} report(s)`,
  );
  if (usedRateFallback) {
    console.log(
      'note: at least one report lacked lines-valid/covered attributes; used line-rate fallback.',
    );
  }
  console.log(`threshold: ${opts.threshold}%`);

  if (percent + 1e-9 < opts.threshold) {
    console.error(`FAIL: coverage ${display}% is below threshold ${opts.threshold}%.`);
    console.error(
      `override (temporary only): add the "${EXEMPT_LABEL}" label to the PR. See docs/coverage-policy.md.`,
    );
    exit(1);
  }

  console.log('PASS: coverage meets the configured threshold.');
  exit(0);
}

main();
