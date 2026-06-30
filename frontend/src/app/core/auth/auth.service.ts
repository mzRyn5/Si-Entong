import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';

import { ApiResponse } from '../models/api-response.model';
import { AuthSession, AuthUser, ChangePasswordRequest, LoginRequest, LoginResponse } from '../models/auth.model';
import { ApiClientService } from '../services/api-client.service';

const ACCESS_TOKEN_KEY = 'access_token';
const REFRESH_TOKEN_KEY = 'refresh_token';
const AUTH_USER_KEY = 'auth_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSubject = new BehaviorSubject<AuthUser | null>(this.readStoredUser());
  readonly currentUser$ = this.currentUserSubject.asObservable();

  constructor(private readonly apiClient: ApiClientService) {}

  login(request: LoginRequest): Observable<AuthSession> {
    return this.apiClient.post<ApiResponse<LoginResponse>>('/auth/login', request).pipe(
      map(response => this.normalizeSession(response.data)),
      tap(session => this.storeSession(session))
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<ApiResponse<void>> {
    return this.apiClient.post<ApiResponse<void>>('/auth/change-password', request);
  }

  logout(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(AUTH_USER_KEY);
    this.currentUserSubject.next(null);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    return Boolean(this.getAccessToken());
  }

  getCurrentUser(): AuthUser | null {
    return this.currentUserSubject.value;
  }

  hasAnyRole(roles: readonly string[]): boolean {
    const user = this.currentUserSubject.value;
    if (!user || !user.role) return false;
    const userRoleLower = user.role.toLowerCase();
    return roles.map(r => r.toLowerCase()).includes(userRoleLower);
  }

  private storeSession(session: AuthSession): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken);
    localStorage.setItem(AUTH_USER_KEY, JSON.stringify(session.user));
    this.currentUserSubject.next(session.user);
  }

  private readStoredUser(): AuthUser | null {
    const rawUser = localStorage.getItem(AUTH_USER_KEY);

    if (!rawUser) {
      return null;
    }

    try {
      return this.normalizeUser(JSON.parse(rawUser) as AuthUser);
    } catch {
      localStorage.removeItem(AUTH_USER_KEY);
      return null;
    }
  }

  private normalizeSession(session: AuthSession): AuthSession {
    return {
      ...session,
      user: this.normalizeUser(session.user)
    };
  }

  private normalizeUser(user: AuthUser): AuthUser {
    return {
      ...user,
      role: user.role.toLowerCase() as AuthUser['role']
    };
  }
}
