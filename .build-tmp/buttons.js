// Adds *appCan / [appCanDisable] to action buttons in the page templates, based on their (click)/(change) handler.
const fs = require('fs');
const path = require('path');

const root = process.argv[2]; // school-admin/src/app
const folders = ['management', 'financial', 'notification', 'exam', 'users'];
const skip = /my-profile|shared[\\/]audience-picker/;

const can = (action, route) => route ? `*appCan="'${action}'; route: '${route}'"` : `*appCan="'${action}'"`;

// [handler regex (matched at the start of the attribute value), directive, optional file filter]
const rules = [
  [/navigateToCreate\(/, () => can('add')],
  [/navigateToAssign\(/, () => can('add')],
  [/(modal\.close\(\); )?navigateToEdit\(/, () => can('edit')],
  [/editNotification\(/, () => can('edit')],
  [/openDeleteConfirmation\(/, () => can('delete')],
  [/toggleActive\(/, () => can('edit')],
  [/open(Payment|Restore|Waive)\(/, () => can('edit')],
  [/editViaParent\(/, () => can('edit', '/management/parents')],
  [/manageFees\(/, () => can('view', '/financial/student-fees')],
  [/assignFees\(/, () => can('add', '/financial/student-fees')],
  [/viewStudent\(/, () => can('view', '/management/students')],
  [/navigateToCategories\(/, (f) => can('view', /revenue/.test(f) ? '/financial/revenue-categories' : '/financial/expense-categories')],
  [/(save|resetChanges)\(\)/, () => can('edit'), /exam-results/],
  [/(save|discardChanges)\(\)/, () => can('edit'), /group-permissions/],
  [/(removeLogo\(|onReset\(\))/, () => can('edit'), /school-info/]
];

const changeRules = [
  [/toggle\w*Status\(/, () => `[appCanDisable]="'edit'"`]
];

function walk(dir, out) {
  for (const name of fs.readdirSync(dir)) {
    const full = path.join(dir, name);
    if (fs.statSync(full).isDirectory()) walk(full, out);
    else if (name.endsWith('.html') && !skip.test(full)) out.push(full);
  }
  return out;
}

const report = [];
for (const folder of folders) {
  for (const file of walk(path.join(root, folder), [])) {
    let src = fs.readFileSync(file, 'utf8');
    let n = 0;

    const apply = (event, list) => {
      src = src.replace(new RegExp(`(\\s)\\(${event}\\)="([^"]*)"`, 'g'), (all, ws, value) => {
        for (const [re, make, only] of list) {
          if (only && !only.test(file)) continue;
          if (new RegExp('^' + re.source).test(value)) {
            n++;
            return `${ws}${make(file)} (${event})="${value}"`;
          }
        }
        return all;
      });
    };

    if (src.includes('appCan')) { report.push(`SKIP (already tagged) ${file}`); continue; }
    apply('click', rules);
    apply('change', changeRules);

    if (n) {
      fs.writeFileSync(file, src);
      report.push(`${n}\t${path.relative(root, file)}`);
    }
  }
}
console.log(report.join('\n'));
