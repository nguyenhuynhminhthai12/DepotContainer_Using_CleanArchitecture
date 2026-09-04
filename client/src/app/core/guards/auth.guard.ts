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

  // 1. Kiểm tra trạng thái đăng nhập từ AuthStore (Signal)
  if (auth.isAuthenticated()) return true;

  // 2. Nếu chưa đăng nhập -> Chuyển hướng ngay về trang /login
  router.navigate(['/login']);
  return false;
};
