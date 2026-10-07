import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthTokens, CurrentUser, LoginResult } from '../api/models';
import { TokenStorage } from './token-storage';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorage);
  private readonly router = inject(Router);

  private readonly currentUser = signal<CurrentUser | null>(null);
  private readonly grantedPermissions = signal<string[]>(this.storage.permissions());

  readonly user = this.currentUser.asReadonly();
  readonly permissions = this.grantedPermissions.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  get hasStoredSession(): boolean {
    return this.storage.accessToken !== null;
  }

  login(email: string, password: string): Observable<LoginResult> {
    return this.http
      .post<LoginResult>(`${environment.apiBase}/auth/login`, { email, password })
      .pipe(tap(result => result.tokens && this.accept(result.tokens)));
  }

  completeTwoFactor(challengeToken: string, code: string): Observable<AuthTokens> {
    return this.http
      .post<AuthTokens>(`${environment.apiBase}/auth/two-factor`, { challengeToken, code })
      .pipe(tap(tokens => this.accept(tokens)));
  }

  refresh(): Observable<AuthTokens> {
    return this.http
      .post<AuthTokens>(`${environment.apiBase}/auth/refresh`, {
        refreshToken: this.storage.refreshToken ?? ''
      })
      .pipe(tap(tokens => this.accept(tokens)));
  }

  loadCurrentUser(): Observable<CurrentUser> {
    return this.http
      .get<CurrentUser>(`${environment.apiBase}/auth/me`)
      .pipe(tap(user => this.currentUser.set(user)));
  }

  logout(): void {
    const refreshToken = this.storage.refreshToken;

    if (refreshToken) {
      this.http.post(`${environment.apiBase}/auth/logout`, { refreshToken }).subscribe({
        error: () => undefined
      });
    }

    this.clearSession();
    void this.router.navigate(['/login']);
  }

  clearSession(): void {
    this.storage.clear();
    this.currentUser.set(null);
    this.grantedPermissions.set([]);
  }

  /**
   * Convenience for templates only. Hiding a control the server would refuse is
   * a usability nicety, never a security boundary: the API re-checks every call.
   */
  can(permission: string): boolean {
    return this.grantedPermissions().includes(permission);
  }

  private accept(tokens: AuthTokens): void {
    this.storage.save(tokens);
    this.grantedPermissions.set(this.storage.permissions());
  }
}
