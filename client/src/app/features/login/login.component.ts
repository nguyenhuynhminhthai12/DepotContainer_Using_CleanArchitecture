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
    <div class="login-wrap">
      <form class="card login" (ngSubmit)="submit()">
        <h2>Sign in</h2>
        <p class="muted">TechSpherex Container Depot</p>

        <label>Email
          <input type="email" [(ngModel)]="email" name="email" required />
        </label>
        <label>Password
          <input type="password" [(ngModel)]="password" name="password" required />
        </label>
        <label>Tenant
          <input [(ngModel)]="tenant" name="tenant" placeholder="default" />
        </label>

        <button type="submit" [disabled]="loading()">{{ loading() ? 'Signing in…' : 'Sign in' }}</button>
        <p class="error" *ngIf="error()">{{ error() }}</p>

        <p class="muted small">Default seed: admin&#64;TechSpherex.dev / Admin&#64;123</p>
      </form>
    </div>
  `,
  styles: [`
    .login-wrap { display: grid; place-items: center; height: 60vh; }
    .login { width: 360px; display: flex; flex-direction: column; gap: 12px; }
    .login h2 { margin: 0; }
    .login label { display: flex; flex-direction: column; gap: 4px; font-size: 13px; }
    .login button { margin-top: 8px; }
    .small { font-size: 12px; }
  `],
})
export class LoginComponent {
  private authSvc = inject(AuthService);
  private authStore = inject(AuthStore);
  private router = inject(Router);

  email = 'admin@TechSpherex.dev';
  password = 'Admin@123';
  tenant = 'default';
  loading = signal(false);
  error = signal<string | null>(null);

  submit(): void {
    this.loading.set(true);
    this.error.set(null);
    if (this.tenant) this.authStore.setTenant(this.tenant);
    this.authSvc.login({ email: this.email, password: this.password }).subscribe({
      next: () => { this.loading.set(false); this.router.navigate(['/yard-map']); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.detail ?? err?.error?.title ?? 'Login failed.');
      },
    });
  }
}
