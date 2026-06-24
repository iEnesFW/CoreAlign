import { execFileSync } from 'node:child_process';
import { existsSync } from 'node:fs';

let raw = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', (chunk) => {
  raw += chunk;
});
process.stdin.on('end', () => {
  const filePath = extractFilePath(raw);
  if (filePath && shouldFormat(filePath)) {
    formatQuietly(filePath);
  }
  process.exit(0);
});

function extractFilePath(input) {
  try {
    const data = JSON.parse(input || '{}');
    const ti = data.tool_input || {};
    return ti.file_path || ti.filePath || ti.path || null;
  } catch {
    return null;
  }
}

function shouldFormat(filePath) {
  if (!existsSync(filePath)) return false;
  if (/(node_modules|[\\/](dist|bin|obj|playwright-report)[\\/])/.test(filePath)) return false;
  return /\.(ts|tsx|js|jsx|mjs|cjs|json|css|md)$/i.test(filePath);
}

function formatQuietly(filePath) {
  try {
    execFileSync('npx', ['--no-install', 'prettier', '--write', filePath], {
      stdio: 'ignore',
    });
  } catch {
    return;
  }
}
