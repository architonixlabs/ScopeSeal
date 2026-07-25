import { readFileSync, readdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { JSDOM } from 'jsdom';

const __dirname = dirname(fileURLToPath(import.meta.url));
const fixturesDir = join(__dirname, 'a11y-fixtures');

const fixtureFiles = readdirSync(fixturesDir).filter((name) => name.endsWith('.html'));
let failed = false;

for (const file of fixtureFiles) {
  const html = readFileSync(join(fixturesDir, file), 'utf8');
  const dom = new JSDOM(html);
  const doc = dom.window.document;
  const violations = [];

  if (!doc.documentElement.getAttribute('lang')) {
    violations.push('html element missing lang attribute');
  }

  if (!doc.querySelector('main, [role="main"]')) {
    violations.push('page missing main landmark');
  }

  for (const input of doc.querySelectorAll('input, select, textarea')) {
    const id = input.getAttribute('id');
    const labelledBy = input.getAttribute('aria-labelledby');
    const label = id ? doc.querySelector(`label[for="${id}"]`) : null;
    const ariaLabel = input.getAttribute('aria-label');
    if (!label && !ariaLabel && !labelledBy) {
      violations.push(`control missing accessible name: ${input.getAttribute('name') ?? id ?? 'unknown'}`);
    }
  }

  if (violations.length > 0) {
    failed = true;
    console.error(`[a11y] ${file}: ${violations.length} violation(s)`);
    for (const violation of violations) {
      console.error(`  - ${violation}`);
    }
  } else {
    console.log(`[a11y] ${file}: OK`);
  }
}

if (failed) {
  process.exit(1);
}
