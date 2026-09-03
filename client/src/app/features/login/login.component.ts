import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AuthStore } from '../../core/services/auth.store';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="login-page">
      <div class="login-card card">
        <div class="login-header">
          <div class="brand-badge-icon">⚓</div>
          <h2>{{ isRegisterMode() ? 'Create an Account' : 'Sign in to TechSpherex' }}</h2>
          <p class="muted">Container Depot & Yard Terminal Operating System</p>
        </div>

        <!-- Mode Toggle Tabs -->
        <div class="auth-tabs">
          <button
            type="button"
            class="auth-tab-btn"
            [class.active]="!isRegisterMode()"
            (click)="switchMode(false)"
          >
            Sign In
          </button>
          <button
            type="button"
            class="auth-tab-btn"
            [class.active]="isRegisterMode()"
            (click)="switchMode(true)"
          >
            Create Account
          </button>
        </div>

        <!-- Sign In Form -->
        <form *ngIf="!isRegisterMode()" (ngSubmit)="submitLogin()" class="login-form">
          <label>Email Address
            <input type="email" [(ngModel)]="email" name="email" placeholder="admin@TechSpherex.dev" required />
          </label>
          <label>Password
            <input type="password" [(ngModel)]="password" name="password" placeholder="••••••••" required />
          </label>
          <label>Tenant ID
            <input [(ngModel)]="tenant" name="tenant" placeholder="default" />
          </label>

          <button type="submit" class="btn-submit" [disabled]="loading()">
            {{ loading() ? 'Signing in…' : 'Sign in to Terminal' }} ➔
          </button>

          <div *ngIf="error()" class="error-banner">⚠️ {{ error() }}</div>
          <div *ngIf="successMsg()" class="success-banner">✓ {{ successMsg() }}</div>
        </form>

        <!-- Register / Sign Up Form -->
        <form *ngIf="isRegisterMode()" (ngSubmit)="submitRegister()" class="login-form">
          <div class="row-2">
            <label>First Name
              <input [(ngModel)]="regFirstName" name="regFirstName" placeholder="John" required />
            </label>
            <label>Last Name
              <input [(ngModel)]="regLastName" name="regLastName" placeholder="Doe" required />
            </label>
          </div>

          <label>Email Address
            <input type="email" [(ngModel)]="regEmail" name="regEmail" placeholder="user@TechSpherex.dev" required />
          </label>

          <label>Password
            <input type="password" [(ngModel)]="regPassword" name="regPassword" placeholder="Min 6 characters" required />
          </label>

          <label>Tenant ID
            <input [(ngModel)]="tenant" name="regTenant" placeholder="default" />
          </label>

          <button type="submit" class="btn-submit btn-register" [disabled]="loading()">
            {{ loading() ? 'Creating Account…' : 'Register New User' }} ➔
          </button>

          <div *ngIf="error()" class="error-banner">⚠️ {{ error() }}</div>
        </form>

        <!-- Quick Demo Box -->
        <div class="demo-box" *ngIf="!isRegisterMode()">
          <div class="demo-header">
            <span>⚡ Quick Demo Access</span>
            <button type="button" class="btn-fill-demo" (click)="fillDemo()">Auto-Fill</button>
          </div>
          <div class="demo-creds">
            <code>admin&#64;TechSpherex.dev</code> / <code>Admin&#64;123</code>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-page {
      display: grid;
      place-items: center;
      min-height: calc(100vh - 120px);
      padding: 20px;
    }

    .login-card {
      width: 100%;
      max-width: 420px;
      padding: 32px 28px;
      border-radius: 16px;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.08), 0 8px 10px -6px rgba(0, 0, 0, 0.04);
      background: #ffffff;
      border: 1px solid #e2e8f0;
      animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
    }

    @keyframes slideUp {
      from { opacity: 0; transform: translateY(12px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .login-header {
      text-align: center;
      margin-bottom: 20px;
    }

    .brand-badge-icon {
      width: 48px;
      height: 48px;
      background: linear-gradient(135deg, #2563eb, #1d4ed8);
      color: #ffffff;
      border-radius: 12px;
      display: grid;
      place-items: center;
      font-size: 24px;
      margin: 0 auto 12px auto;
      box-shadow: 0 4px 10px rgba(37, 99, 235, 0.3);
    }

    .login-header h2 {
      margin-bottom: 4px;
      font-size: 20px;
      font-weight: 800;
    }

    .login-header p {
      font-size: 13px;
      margin: 0;
    }

    .auth-tabs {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 4px;
      background: #f1f5f9;
      padding: 4px;
      border-radius: 8px;
      margin-bottom: 20px;
    }

    .auth-tab-btn {
      background: none;
      border: none;
      color: #64748b;
      font-weight: 600;
      font-size: 13px;
      padding: 8px 12px;
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .auth-tab-btn.active {
      background: #ffffff;
      color: #0f172a;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
    }

    .login-form {
      display: flex;
      flex-direction: column;
      gap: 14px;
    }

    .row-2 {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
    }

    .btn-submit {
      margin-top: 6px;
      padding: 10px 16px;
      font-size: 14px;
      font-weight: 600;
      background: #2563eb;
      color: #fff;
      border: none;
      border-radius: 8px;
      cursor: pointer;
    }

    .btn-submit:hover {
      background: #1d4ed8;
    }

    .btn-register {
      background: #10b981;
    }
    .btn-register:hover {
      background: #059669;
    }

    .error-banner {
      background: #fef2f2;
      color: #b91c1c;
      padding: 8px 12px;
      border-radius: 6px;
      font-size: 12px;
      border: 1px solid #fecaca;
    }

    .success-banner {
      background: #ecfdf5;
      color: #047857;
      padding: 8px 12px;
      border-radius: 6px;
      font-size: 12px;
      border: 1px solid #a7f3d0;
    }

    .demo-box {
      margin-top: 24px;
      padding: 12px 14px;
      background: #f8fafc;
      border-radius: 8px;
      border: 1px dashed #cbd5e1;
    }

    .demo-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 12px;
      font-weight: 600;
      color: #475569;
      margin-bottom: 6px;
    }

    .btn-fill-demo {
      padding: 2px 8px;
      font-size: 11px;
      background: #e2e8f0;
      color: #0f172a;
      border: 1px solid #cbd5e1;
      border-radius: 4px;
      cursor: pointer;
    }

    .btn-fill-demo:hover {
      background: #cbd5e1;
    }

    .demo-creds {
      font-size: 11px;
      color: #64748b;
    }

    .demo-creds code {
      background: #e2e8f0;
      padding: 1px 5px;
      border-radius: 4px;
      color: #0f172a;
      font-family: var(--font-mono);
    }
  `],
})
export class LoginComponent {
  private readonly authSvc = inject(AuthService);
  private readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  isRegisterMode = signal(false);

  // Login Form
  email = 'admin@TechSpherex.dev';
  password = 'Admin@123';
  tenant = 'default';

  // Register Form
  regFirstName = '';
  regLastName = '';
  regEmail = '';
  regPassword = '';

  loading = signal(false);
  error = signal<string | null>(null);
  successMsg = signal<string | null>(null);

  switchMode(register: boolean): void {
    this.isRegisterMode.set(register);
    this.error.set(null);
    this.successMsg.set(null);
  }

  fillDemo(): void {
    this.email = 'admin@TechSpherex.dev';
    this.password = 'Admin@123';
    this.tenant = 'default';
  }

  submitLogin(): void {
    this.loading.set(true);
    this.error.set(null);
    this.successMsg.set(null);
    if (this.tenant) this.authStore.setTenant(this.tenant);
    this.authSvc.login({ email: this.email, password: this.password }).subscribe({
      next: () => { this.loading.set(false); this.router.navigate(['/yard-map']); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.title ?? 'Login failed. Please check credentials.');
      },
    });
  }

  submitRegister(): void {
    if (!this.regFirstName || !this.regLastName || !this.regEmail || !this.regPassword) {
      this.error.set('Please fill out all required registration fields.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.successMsg.set(null);

    if (this.tenant) this.authStore.setTenant(this.tenant);

    this.authSvc.register({
      firstName: this.regFirstName,
      lastName: this.regLastName,
      email: this.regEmail,
      password: this.regPassword,
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.successMsg.set('Account registered successfully! Please sign in with your new credentials.');
        this.email = this.regEmail;
        this.password = this.regPassword;
        this.isRegisterMode.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.title ?? 'Registration failed.');
      }
    });
  }
}
