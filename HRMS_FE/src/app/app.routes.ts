import { Routes } from '@angular/router';
import { authGuard, permissionGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login').then(m => m.Login)
  },
  {
    path: '',
    loadComponent: () => import('./shared/shell').then(m => m.Shell),
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard) },
      {
        path: 'employees',
        canActivate: [permissionGuard('employees.read')],
        loadComponent: () => import('./features/employees/employees').then(m => m.Employees)
      },
      { path: 'billing', loadComponent: () => import('./features/billing/billing').then(m => m.Billing) },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' }
    ]
  },
  { path: '**', redirectTo: '' }
];
