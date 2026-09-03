/**
 * Component chính của ứng dụng (App Root Component).
 * Hiển thị topbar điều hướng với logo, menu nav, thông tin tenant/user, và nút đăng xuất.
 * Render <router-outlet> để hiển thị các trang con theo tuyến đường hiện tại.
 * Bản quyền (c) 2026 TechSpherex.
 */
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
      <div class="brand-wrap">
        <div class="brand-logo">⚓</div>
        <div>
          <div class="brand-title">TechSpherex <span class="brand-badge">TOS</span></div>
          <div class="brand-sub">Container Depot Operating System</div>
        </div>
      </div>

      <nav *ngIf="auth.isAuthenticated()">
        <a routerLink="/yard-map" routerLinkActive="active">
          <span class="nav-icon">🗺️</span> Yard Map
        </a>
        <a routerLink="/containers" routerLinkActive="active">
          <span class="nav-icon">📦</span> Containers
        </a>
        <a routerLink="/gate" routerLinkActive="active">
          <span class="nav-icon">🚪</span> Gate Operations
        </a>
        <a routerLink="/delivery-orders" routerLinkActive="active">
          <span class="nav-icon">📋</span> Delivery Orders
        </a>
        <a routerLink="/reports" routerLinkActive="active">
          <span class="nav-icon">📊</span> Reports
        </a>
      </nav>

      <div class="user-wrap" *ngIf="auth.isAuthenticated()">
        <div class="tenant-tag">
          <span class="tenant-dot"></span>
          Tenant: <b>{{ auth.tenantId() }}</b>
        </div>
        <div class="user-badge" [title]="auth.userEmail() ?? ''">
          <span class="user-avatar">👤</span>
          <span class="user-email">{{ auth.userEmail() }}</span>
        </div>
        <button class="secondary btn-logout" (click)="logout()">
          <span>Sign out</span> ➔
        </button>
      </div>
    </header>

    <main class="content">
      <router-outlet />
    </main>
  `,
  styles: [`
    .topbar {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 20px;
      padding: 10px 24px;
      background: #ffffff;
      border-bottom: 1px solid var(--color-border);
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
      position: sticky;
      top: 0;
      z-index: 100;
    }

    .brand-wrap {
      display: flex;
      align-items: center;
      gap: 10px;
      text-decoration: none;
    }

    .brand-logo {
      width: 36px;
      height: 36px;
      background: linear-gradient(135deg, #2563eb, #1d4ed8);
      color: #ffffff;
      border-radius: 8px;
      display: grid;
      place-items: center;
      font-size: 18px;
      box-shadow: 0 2px 4px rgba(37, 99, 235, 0.3);
    }

    .brand-title {
      font-weight: 800;
      font-size: 15px;
      color: #0f172a;
      letter-spacing: -0.02em;
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .brand-badge {
      font-size: 10px;
      font-weight: 700;
      background: #eff6ff;
      color: #2563eb;
      padding: 1px 6px;
      border-radius: 4px;
      letter-spacing: 0.05em;
    }

    .brand-sub {
      font-size: 11px;
      color: #64748b;
      line-height: 1;
    }

    nav {
      display: flex;
      gap: 6px;
      background: #f8fafc;
      padding: 4px;
      border-radius: 8px;
      border: 1px solid #e2e8f0;
    }

    nav a {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      color: #475569;
      padding: 6px 12px;
      border-radius: 6px;
      font-size: 13px;
      font-weight: 600;
      transition: all 0.15s ease;
      text-decoration: none;
    }

    nav a:hover {
      color: #0f172a;
      background: #f1f5f9;
    }

    nav a.active {
      background: #2563eb;
      color: #ffffff;
      box-shadow: 0 1px 2px rgba(37, 99, 235, 0.2);
    }

    nav a.active .nav-icon {
      filter: none;
    }

    .nav-icon {
      font-size: 14px;
    }

    .user-wrap {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .tenant-tag {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
      color: #475569;
      background: #f8fafc;
      padding: 4px 10px;
      border-radius: 20px;
      border: 1px solid #e2e8f0;
    }

    .tenant-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #10b981;
    }

    .user-badge {
      display: flex;
      align-items: center;
      gap: 6px;
      font-size: 12px;
      color: #334155;
      font-weight: 500;
    }

    .user-avatar {
      width: 26px;
      height: 26px;
      background: #e2e8f0;
      border-radius: 50%;
      display: grid;
      place-items: center;
      font-size: 13px;
    }

    .user-email {
      max-width: 150px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .btn-logout {
      padding: 5px 10px;
      font-size: 12px;
      display: inline-flex;
      align-items: center;
      gap: 4px;
    }

    .content {
      padding: 24px;
      max-width: 1440px;
      margin: 0 auto;
      animation: fadeIn 0.2s ease;
    }

    @keyframes fadeIn {
      from { opacity: 0; transform: translateY(4px); }
      to { opacity: 1; transform: translateY(0); }
    }
  `],
})
export class AppComponent {
  constructor(public auth: AuthStore, private authService: AuthService) {}

  logout(): void {
    this.authService.logout();
    window.location.href = '/login';
  }
}
