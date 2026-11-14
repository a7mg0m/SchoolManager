// Adds `permission` to route data in app-routing.module.ts (save -> add, save/:id -> edit, assign -> add)
const fs = require('fs');
const file = process.argv[2];
let src = fs.readFileSync(file, 'utf8');
let count = 0;

src = src.replace(/\{path: '([^']+)', component: (\w+), data: \{extraParameter: '(\w+)'\}\}/g, (all, path, comp, extra) => {
  let perm = null;
  if (/\/save$/.test(path) || /\/assign$/.test(path)) perm = 'add';
  else if (/\/save\/:id$/.test(path)) perm = 'edit';
  if (!perm) return all;
  count++;
  return `{path: '${path}', component: ${comp}, data: {extraParameter: '${extra}', permission: '${perm}'}}`;
});

src = src.replace(
  "{path: 'my-profile', component: MyProfileComponent},",
  "{path: 'my-profile', component: MyProfileComponent, data: {permission: false}},"
);
src = src.replace(
  "canActivateChild: [AuthGuard],",
  "// AuthGuard: signed in; PermissionGuard: group permissions (route data `permission`, default 'view')\n    canActivateChild: [AuthGuard, PermissionGuard],"
);
src = src.replace(
  "import { AuthGuard } from './core/guards/auth.guard';",
  "import { AuthGuard } from './core/guards/auth.guard';\nimport { PermissionGuard } from './core/guards/permission.guard';"
);

fs.writeFileSync(file, src);
console.log('routes tagged: ' + count);
