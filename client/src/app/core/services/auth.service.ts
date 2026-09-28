/**
 * Dịch vụ xác thực (Authentication Service) cho ứng dụng Angular.
 * Cung cấp các phương thức: đăng nhập (login), đăng ký (register), làm mới token (refresh), và đăng xuất (logout).
 * Giao tiếp với API qua HttpClient và lưu trữ phiên qua AuthStore.
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest } from '../models/api.models';
import { AuthStore } from './auth.store';

/**
 * Yêu cầu đăng ký tài khoản người dùng mới.
 */
export interface RegisterRequest {
  /** Họ và tên đệm */
  firstName: string;
  /** Tên */
  lastName: string;
  /** Địa chỉ email đăng ký */
  email: string;
  /** Mật khẩu đăng nhập */
  password: string;
}

/**
 * Service xử lý đăng nhập, cấp mới token và phiên làm việc của người dùng.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  /** Đường dẫn API xác thực */
  private readonly base = '/api/auth';

  /**
   * Khởi tạo AuthService.
   * @param http Client gửi các HTTP request.
   * @param auth Store lưu trữ token và trạng thái đăng nhập trong sessionStorage.
   */
  constructor(private readonly http: HttpClient, private readonly auth: AuthStore) {}

  /**
   * Đăng nhập vào hệ thống bằng Email và Mật khẩu.
   * Lưu Access Token và Refresh Token vào AuthStore khi thành công.
   * @param req Thông tin đăng nhập gồm email và password.
   * @returns Observable phát ra AuthResponse chứa JWT Token và thời hạn.
   */
  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, req).pipe(
      tap((res) => this.auth.setSession(res.accessToken, res.refreshToken, req.email)),
    );
  }

  /**
   * Đăng ký tài khoản người dùng mới vào hệ thống.
   * @param req Dữ liệu họ tên, email và mật khẩu của người dùng.
   * @returns Observable hoàn thành khi đăng ký thành công.
   */
  register(req: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/register`, req);
  }

  /**
   * Làm mới Access Token đã hết hạn bằng Refresh Token đang lưu trữ.
   * @returns Observable phát ra bộ Token mới.
   */
  refresh(): Observable<AuthResponse> {
    const refresh = sessionStorage.getItem('techspherex.refresh_token');
    return this.http.post<AuthResponse>(`${this.base}/refresh`, { refreshToken: refresh });
  }

  /**
   * Đăng xuất khỏi hệ thống và xóa toàn bộ token khỏi sessionStorage.
   */
  logout(): void {
    this.auth.clear();
  }
}
