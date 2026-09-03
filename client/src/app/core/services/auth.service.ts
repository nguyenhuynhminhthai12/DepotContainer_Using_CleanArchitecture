import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { AuthResponse, LoginRequest } from '../models/api.models';
import { AuthStore } from './auth.store';

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly base = '/api/auth';

  constructor(private readonly http: HttpClient, private readonly auth: AuthStore) {}

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.base}/login`, req).pipe(
      tap((res) => this.auth.setSession(res.accessToken, res.refreshToken, req.email)),
    );
  }

  register(req: RegisterRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/register`, req);
  }

  refresh(): Observable<AuthResponse> {
    const refresh = sessionStorage.getItem('techspherex.refresh_token');
    return this.http.post<AuthResponse>(`${this.base}/refresh`, { refreshToken: refresh });
  }

  logout(): void {
    this.auth.clear();
  }
}
