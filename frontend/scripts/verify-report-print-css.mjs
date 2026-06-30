import { readFileSync } from 'node:fs';
import { join } from 'node:path';

const root = process.cwd();

function read(relativePath) {
  return readFileSync(join(root, relativePath), 'utf8');
}

function assert(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

const indexHtml = read('src/index.html');
const globalStyles = read('src/styles/styles.scss');
const reportTemplate = read('src/app/features/reports/pages/reports-page/reports-page.component.html');
const reportComponent = read('src/app/features/reports/pages/reports-page/reports-page.component.ts');
const reportStyles = read('src/app/features/reports/pages/reports-page/reports-page.component.scss');

assert(
  !indexHtml.includes('@media print'),
  'Report print CSS should not be duplicated inline in index.html because it can override component print flow.',
);

assert(
  !/@page\s*\{[^}]*margin:\s*0\s*!important/i.test(globalStyles),
  'Printed reports should use page margin instead of forcing @page margin to 0.',
);

assert(
  /@page\s*\{[^}]*margin:\s*(?:0|[1-4]mm)\s+12mm\s+12mm\s+12mm\s*;/i.test(globalStyles),
  'Printed reports should keep the top page margin small so the store name starts near the top.',
);

const printBodyBlock = globalStyles.match(/@media print[\s\S]*?body\s*\{(?<bodyStyles>[^}]*)\}/i)?.groups?.bodyStyles ?? '';

assert(
  /padding:\s*0\s*!important/i.test(printBodyBlock),
  'Printed reports should keep body padding at 0 and rely on @page margin.',
);

assert(
  reportTemplate.includes('reports-print-root'),
  'Reports page should expose a dedicated print root for page-flow rules.',
);

assert(
  /\.reports-print-root[\s\S]*page-break-before:\s*auto\s*!important[\s\S]*break-before:\s*auto\s*!important/i.test(reportStyles),
  'Reports print root should explicitly avoid starting on a new page.',
);

assert(
  /\.reports-data-card[\s\S]*page-break-before:\s*auto\s*!important[\s\S]*break-before:\s*auto\s*!important/i.test(reportStyles),
  'Report data card should not force or inherit a leading page break.',
);

assert(
  reportComponent.includes('body class="reports-printing"') && reportComponent.includes('cleanup'),
  'Export PDF should add print mode to the temporary report document and clean up the iframe.',
);

assert(
  reportComponent.includes('reports-print-frame') && reportComponent.includes('reports-print-root'),
  'Export PDF should print a temporary document containing only the reports print root.',
);

assert(
  !/window\.print\(\)/.test(reportComponent),
  'Export PDF should not print the full application window because app header/sidebar/footer can be included.',
);

assert(
  reportComponent.includes('printWindow.print()'),
  'Export PDF should print through the temporary report-only iframe.',
);

assert(
  /body\.reports-printing[\s\S]*app-shell\s+aside[\s\S]*display:\s*none\s*!important/i.test(globalStyles),
  'Report print mode should strongly hide the app sidebar.',
);

assert(
  /body\.reports-printing[\s\S]*app-shell\s+header[\s\S]*display:\s*none\s*!important/i.test(globalStyles),
  'Report print mode should strongly hide the app topbar.',
);

assert(
  /body\.reports-printing[\s\S]*app-root[\s\S]*display:\s*contents\s*!important/i.test(globalStyles),
  'Report print mode should remove app root wrapper layout boxes so the report can start on page 1.',
);

assert(
  /body\.reports-printing[\s\S]*app-shell\s+main[\s\S]*display:\s*contents\s*!important/i.test(globalStyles),
  'Report print mode should remove main wrapper layout boxes so the report can start on page 1.',
);

assert(
  /body\.reports-printing[\s\S]*\.reports-print-root[\s\S]*margin:\s*8mm\s+0\s+0\s+0\s*!important/i.test(globalStyles),
  'Report print mode should add a small top margin to the report content.',
);

console.log('Report print CSS checks passed.');
