import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthStore } from '../services/auth.store';
import { catchError, throwError } from 'rxjs';

/**
 * Attaches the JWT bearer token and X-Tenant-Id header to every outgoing API call.
 * Automatically catches 401 Unauthorized errors (e.g. token expired after restart)
 * and navigates to the login screen.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  const headers: Record<string, string> = {};

  const token = auth.accessToken();
  if (token) headers['Authorization'] = `Bearer ${token}`;

  const tenant = auth.tenantId();
  if (tenant) headers['X-Tenant-Id'] = tenant;

  return next(req.clone({ setHeaders: headers })).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/api/identity/')) {
        auth.clear();
        router.navigate(['/login'], { queryParams: { expired: 'true' } });
      }
      return throwError(() => error);
    })
  );
};
