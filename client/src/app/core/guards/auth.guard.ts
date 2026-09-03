/**
 * Route Guard bảo vệ các trang yêu cầu xác thực (Authentication Guard).
 * Chuyển hướng người dùng chưa đăng nhập về trang /login.
 * Sử dụng CanActivateFn của Angular Router.
 * Bản quyền (c) 2026 TechSpherex.
 */
import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthStore } from '../services/auth.store';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthStore);
  const router = inject(Router);
  if (auth.isAuthenticated()) return true;
  router.navigate(['/login']);
  return false;
};
