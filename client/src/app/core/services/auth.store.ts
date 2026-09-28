/**
 * Lưu trữ trạng thái xác thực (Authentication State) trong Angular.
 * Chứa JWT access token và refresh token trong sessionStorage của trình duyệt,
 * giúp duy trì đăng nhập qua các lần reload trang nhưng xóa khi đóng tab.
 * Cung cấp các signal để theo dõi trạng thái isAuthenticated, tenantId, userEmail.
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable, signal, computed } from '@angular/core';

/**
 * Quản lý trạng thái đăng nhập, lưu trữ Token và TenantId trong sessionStorage.
 * Sử dụng Angular Signals để tự động cập nhật UI phản ứng nhanh.
 */
@Injectable({ providedIn: 'root' })
export class AuthStore {
  /** Khóa lưu trữ Access Token trong sessionStorage */
  private static readonly TOKEN_KEY = 'techspherex.access_token';
  /** Khóa lưu trữ Refresh Token trong sessionStorage */
  private static readonly REFRESH_KEY = 'techspherex.refresh_token';
  /** Khóa lưu trữ Tenant ID trong sessionStorage */
  private static readonly TENANT_KEY = 'techspherex.tenant_id';
  /** Khóa lưu trữ Email người dùng trong sessionStorage */
  private static readonly USER_KEY = 'techspherex.user_email';

  /** Signal chứa chuỗi Access Token hiện tại (hoặc null nếu chưa đăng nhập) */
  readonly accessToken = signal<string | null>(sessionStorage.getItem(AuthStore.TOKEN_KEY));
  /** Signal chứa Tenant ID hiện tại (mặc định: 'default') */
  readonly tenantId = signal<string>(sessionStorage.getItem(AuthStore.TENANT_KEY) ?? 'default');
  /** Signal chứa Email của người dùng đang đăng nhập */
  readonly userEmail = signal<string | null>(sessionStorage.getItem(AuthStore.USER_KEY));
  /** Computed Signal kiểm tra người dùng đã đăng nhập hay chưa (true nếu có accessToken) */
  readonly isAuthenticated = computed(() => !!this.accessToken());

  /**
   * Lưu phiên làm việc mới khi đăng nhập thành công.
   * @param token Chuỗi JWT Access Token.
   * @param refresh Chuỗi Refresh Token.
   * @param email Địa chỉ email của người dùng.
   */
  setSession(token: string, refresh: string, email: string): void {
    sessionStorage.setItem(AuthStore.TOKEN_KEY, token);
    sessionStorage.setItem(AuthStore.REFRESH_KEY, refresh);
    sessionStorage.setItem(AuthStore.USER_KEY, email);
    this.accessToken.set(token);
    this.userEmail.set(email);
  }

  /**
   * Thay đổi Tenant hiện tại (Chi nhánh / Bãi container).
   * @param id Mã định danh Tenant mới.
   */
  setTenant(id: string): void {
    sessionStorage.setItem(AuthStore.TENANT_KEY, id);
    this.tenantId.set(id);
  }

  /**
   * Xóa toàn bộ token và thông tin phiên đăng nhập khỏi sessionStorage.
   */
  clear(): void {
    sessionStorage.removeItem(AuthStore.TOKEN_KEY);
    sessionStorage.removeItem(AuthStore.REFRESH_KEY);
    sessionStorage.removeItem(AuthStore.USER_KEY);
    this.accessToken.set(null);
    this.userEmail.set(null);
  }
}
