import { Injectable } from '@angular/core';
import { AuthTokens } from '../api/models';

const ACCESS_KEY = 'hrms.access';
const REFRESH_KEY = 'hrms.refresh';

@Injectable({ providedIn: 'root' })
export class TokenStorage {
  private memoryAccessToken: string | null = null;

  get accessToken(): string | null {
    if (this.memoryAccessToken) {
      return this.memoryAccessToken;
    }

    this.memoryAccessToken = this.read(ACCESS_KEY);

    return this.memoryAccessToken;
  }

  get refreshToken(): string | null {
    return this.read(REFRESH_KEY);
  }

  save(tokens: AuthTokens): void {
    this.memoryAccessToken = tokens.accessToken;
    this.write(ACCESS_KEY, tokens.accessToken);
    this.write(REFRESH_KEY, tokens.refreshToken);
  }

  clear(): void {
    this.memoryAccessToken = null;

    try {
      localStorage.removeItem(ACCESS_KEY);
      localStorage.removeItem(REFRESH_KEY);
    } catch {
      // storage can be unavailable in private mode; ignore
    }
  }

  permissions(): string[] {
    const token = this.accessToken;

    if (!token) {
      return [];
    }

    try {
      const payload = JSON.parse(
        atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))
      );
      const claim = payload['permission'];

      return Array.isArray(claim) ? claim : claim ? [claim] : [];
    } catch {
      return [];
    }
  }

  private read(key: string): string | null {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  }

  private write(key: string, value: string): void {
    try {
      localStorage.setItem(key, value);
    } catch {
      // ignore
    }
  }
}
