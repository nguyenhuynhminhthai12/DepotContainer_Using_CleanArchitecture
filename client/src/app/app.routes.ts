import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'yard-map' },
  {
    path: 'login',
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'yard-map',
    loadComponent: () => import('./features/yard-map/yard-map.component').then((m) => m.YardMapComponent),
    canActivate: [authGuard],
  },
  {
    path: 'containers',
    loadComponent: () => import('./features/containers/containers.component').then((m) => m.ContainersComponent),
    canActivate: [authGuard],
  },
  {
    path: 'gate',
    loadComponent: () => import('./features/gate/gate.component').then((m) => m.GateComponent),
    canActivate: [authGuard],
  },
  {
    path: 'delivery-orders',
    loadComponent: () => import('./features/delivery-orders/delivery-orders.component').then((m) => m.DeliveryOrdersComponent),
    canActivate: [authGuard],
  },
  {
    path: 'reports',
    loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: 'yard-map' },
];
