import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

// Resets learnhub_e2e (never `learnhub`) before the test run.
export default function globalSetup() {
  const script = fileURLToPath(new URL('./scripts/reset-e2e-db.sh', import.meta.url));
  execFileSync('bash', [script], { stdio: 'inherit', env: process.env });
}
