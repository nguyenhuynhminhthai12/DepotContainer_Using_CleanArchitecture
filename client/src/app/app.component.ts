import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthStore } from './core/services/auth.store';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <div class="brand">🏗️ TechSpherex Depot</div>
      <nav *ngIf="auth.isAuthenticated()">
        <a routerLink="/yard-map" routerLinkActive="active">Yard Map</a>
        <a routerLink="/containers" routerLinkActive="active">Containers</a>
        <a routerLink="/gate" routerLinkActive="active">Gate</a>
        <a routerLink="/delivery-orders" routerLinkActive="active">Delivery Orders</a>
        <a routerLink="/reports" routerLinkActive="active">Reports</a>
      </nav>
      <div class="user" *ngIf="auth.isAuthenticated() as ok">
        <span class="muted">Tenant: <b>{{ auth.tenantId() }}</b></span>
        <span class="muted">{{ auth.userEmail() }}</span>
        <button class="secondary" (click)="logout()">Sign out</button>
      </div>
    </header>

    <main class="content">
      <router-outlet />
    </main>
  `,
  styles: [`
    .topbar {
      display: flex; align-items: center; gap: 24px;
      padding: 12px 24px;
      background: #fff;
      border-bottom: 1px solid var(--color-border);
      box-shadow: var(--shadow);
    }
    .brand { font-weight: 700; font-size: 16px; }
    nav { display: flex; gap: 16px; flex: 1; }
    nav a {
      color: var(--color-text);
      padding: 6px 10px;
      border-radius: var(--radius);
    }
    nav a.active { background: var(--color-primary); color: #fff; }
    .user { display: flex; gap: 12px; align-items: center; }
    .content { padding: 24px; max-width: 1400px; margin: 0 auto; }
  `],
})
export class AppComponent {
  constructor(public auth: AuthStore, private readonly authService: AuthService) {}

  logout(): void {
    this.authService.logout();
    window.location.href = '/login';
  }
}
