import { Injectable, signal, computed } from '@angular/core';

/**
 * Holds the JWT access token + refresh token in browser sessionStorage
 * so they're available across page reloads but cleared when the tab closes.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  private static readonly TOKEN_KEY = 'techspherex.access_token';
  private static readonly REFRESH_KEY = 'techspherex.refresh_token';
  private static readonly TENANT_KEY = 'techspherex.tenant_id';
  private static readonly USER_KEY = 'techspherex.user_email';

  readonly accessToken = signal<string | null>(sessionStorage.getItem(AuthStore.TOKEN_KEY));
  readonly tenantId = signal<string>(sessionStorage.getItem(AuthStore.TENANT_KEY) ?? 'default');
  readonly userEmail = signal<string | null>(sessionStorage.getItem(AuthStore.USER_KEY));
  readonly isAuthenticated = computed(() => !!this.accessToken());

  setSession(token: string, refresh: string, email: string): void {
    sessionStorage.setItem(AuthStore.TOKEN_KEY, token);
    sessionStorage.setItem(AuthStore.REFRESH_KEY, refresh);
    sessionStorage.setItem(AuthStore.USER_KEY, email);
    this.accessToken.set(token);
    this.userEmail.set(email);
  }

  setTenant(id: string): void {
    sessionStorage.setItem(AuthStore.TENANT_KEY, id);
    this.tenantId.set(id);
  }

  clear(): void {
    sessionStorage.removeItem(AuthStore.TOKEN_KEY);
    sessionStorage.removeItem(AuthStore.REFRESH_KEY);
    sessionStorage.removeItem(AuthStore.USER_KEY);
    this.accessToken.set(null);
    this.userEmail.set(null);
  }
}
