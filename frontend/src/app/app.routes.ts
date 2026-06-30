import { Routes } from '@angular/router';
import { roleGuard } from './core/guards/role.guard';
import { sysAdminGuard } from './core/guards/sys-admin.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/pages/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    loadComponent: () => import('./layout/app-shell/app-shell.component').then(m => m.AppShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'products',
        loadChildren: () => import('./features/products/products.routes').then(m => m.PRODUCTS_ROUTES)
      },
      {
        path: 'categories',
        loadChildren: () => import('./features/categories/categories.routes').then(m => m.CATEGORIES_ROUTES)
      },
      {
        path: 'units',
        loadChildren: () => import('./features/units/units.routes').then(m => m.UNITS_ROUTES)
      },
      {
        path: 'suppliers',
        loadChildren: () => import('./features/suppliers/suppliers.routes').then(m => m.SUPPLIERS_ROUTES)
      },
      {
        path: 'customers',
        loadChildren: () => import('./features/customers/customers.routes').then(m => m.CUSTOMERS_ROUTES)
      },
      {
        path: 'inventory',
        loadChildren: () => import('./features/inventory/inventory.routes').then(m => m.INVENTORY_ROUTES)
      },
      {
        path: 'purchases',
        loadChildren: () => import('./features/purchases/purchases.routes').then(m => m.PURCHASES_ROUTES)
      },
      {
        path: 'sales',
        loadChildren: () => import('./features/sales/sales.routes').then(m => m.SALES_ROUTES)
      },
      {
        path: 'receivables',
        loadComponent: () => import('./features/receivables/pages/receivables-list/receivables-list.component').then(m => m.ReceivablesListComponent)
      },
      {
        path: 'payables',
        loadComponent: () => import('./features/payables/pages/payables-list/payables-list.component').then(m => m.PayablesListComponent)
      },
      {
        path: 'expenses',
        loadChildren: () => import('./features/expenses/expenses.routes').then(m => m.EXPENSES_ROUTES)
      },
      {
        path: 'reports',
        canActivate: [roleGuard],
        loadChildren: () => import('./features/reports/reports.routes').then(m => m.REPORTS_ROUTES)
      },
      {
        path: 'settings',
        canActivate: [roleGuard],
        loadChildren: () => import('./features/settings/settings.routes').then(m => m.SETTINGS_ROUTES)
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        loadChildren: () => import('./features/users/users.routes').then(m => m.USERS_ROUTES)
      },
      {
        path: 'audit-logs',
        canActivate: [roleGuard],
        loadComponent: () => import('./features/audit-logs/pages/audit-logs-list/audit-logs-list.component').then(m => m.AuditLogsListComponent)
      },
      {
        path: 'sysadmin/stores',
        canActivate: [sysAdminGuard],
        loadComponent: () => import('./features/sysadmin/pages/stores/sysadmin-stores.component').then(m => m.SysadminStoresComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
