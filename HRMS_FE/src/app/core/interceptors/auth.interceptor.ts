import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';
import { TokenStorage } from '../auth/token-storage';

let refreshInFlight = false;
const refreshed = new BehaviorSubject<string | null>(null);

const isAuthEndpoint = (url: string): boolean =>
  url.includes('/auth/login') || url.includes('/auth/refresh') || url.includes('/auth/two-factor');

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const storage = inject(TokenStorage);
  const auth = inject(AuthService);
  const router = inject(Router);

  const withToken = (req: HttpRequest<unknown>, token: string | null) =>
    token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  if (!request.url.startsWith(environment.apiBase)) {
    return next(request);
  }

  const authorized = withToken(request, storage.accessToken);

  return next(authorized).pipe(
    catchError((error: unknown) => {
      const is401 = error instanceof HttpErrorResponse && error.status === 401;

      if (!is401 || isAuthEndpoint(request.url) || !storage.refreshToken) {
        return throwError(() => error);
      }

      if (refreshInFlight) {
        return waitForRefresh(request, next, withToken);
      }

      refreshInFlight = true;
      refreshed.next(null);

      return auth.refresh().pipe(
        switchMap(tokens => {
          refreshInFlight = false;
          refreshed.next(tokens.accessToken);

          return next(withToken(request, tokens.accessToken));
        }),
        catchError(refreshError => {
          refreshInFlight = false;
          auth.clearSession();
          void router.navigate(['/login']);

          return throwError(() => refreshError);
        })
      );
    })
  );
};

function waitForRefresh(
  request: HttpRequest<unknown>,
  next: (req: HttpRequest<unknown>) => Observable<unknown>,
  withToken: (req: HttpRequest<unknown>, token: string | null) => HttpRequest<unknown>
): Observable<never> {
  return refreshed.pipe(
    filter((token): token is string => token !== null),
    take(1),
    switchMap(token => next(withToken(request, token)))
  ) as Observable<never>;
}
