#!/usr/bin/env node
import { spawnSync } from 'node:child_process';
import { resolve, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { mkdirSync, existsSync } from 'node:fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const apiProject = resolve(repoRoot, 'server', 'src', 'CoreAlign.API', 'CoreAlign.API.csproj');
const apiAssembly = resolve(
  repoRoot,
  'server',
  'src',
  'CoreAlign.API',
  'bin',
  'Debug',
  'net10.0',
  'CoreAlign.API.dll',
);
const outputDir = resolve(repoRoot, 'openapi');
const outputFile = resolve(outputDir, 'v1.json');

function run(cmd, args, opts = {}) {
  const isWin = process.platform === 'win32';
  const useShell = isWin;
  const quoted = (s) => (/[\s"']/.test(s) ? `"${s.replaceAll('"', '\\"')}"` : s);
  const command = useShell ? [cmd, ...args].map(quoted).join(' ') : cmd;
  const result = spawnSync(useShell ? command : cmd, useShell ? [] : args, {
    stdio: 'inherit',
    cwd: repoRoot,
    shell: useShell,
    ...opts,
  });
  if (result.status !== 0) {
    process.stderr.write(`[nswag-spec] ${cmd} ${args.join(' ')} exited with ${result.status}\n`);
    process.exit(result.status ?? 1);
  }
}

if (!existsSync(outputDir)) {
  mkdirSync(outputDir, { recursive: true });
}

process.stdout.write('[nswag-spec] Building CoreAlign.API...\n');
run('dotnet', ['build', apiProject, '-nologo']);

process.stdout.write('[nswag-spec] Generating openapi/v1.json...\n');

const env = {
  ...process.env,
  NSWAG_GENERATION: '1',
  ASPNETCORE_ENVIRONMENT: 'Production',
  DOTNET_ENVIRONMENT: 'Production',
  Database__Provider: 'Sqlite',
  ConnectionStrings__DefaultConnection: 'Data Source=corealign.designtime.db',
  Cors__AllowedOrigins__0: 'http://localhost:5173',
  Jwt__SecretKey:
    'designtimeSecretXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX',
  Jwt__Issuer: 'corealign-designtime',
  Jwt__Audience: 'corealign-designtime',
  Auth__AutoConfirmEmail: 'false',
};

run('swagger', ['tofile', '--output', outputFile, apiAssembly, 'v1'], { env });

process.stdout.write(`[nswag-spec] OpenAPI spec written to ${outputFile}\n`);
