import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthStore } from '../services/auth.store';

/**
 * Attaches the JWT bearer token and X-Tenant-Id header to every outgoing API call.
 * The dev proxy (proxy.conf.json) rewrites /api/* → http://localhost:8080/* so this
 * works for both REST endpoints exposed by the .NET API.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStore);
  const headers: Record<string, string> = {};

  const token = auth.accessToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const tenant = auth.tenantId();
  if (tenant) headers['X-Tenant-Id'] = tenant;

  return next(req.clone({ setHeaders: headers }));
};
