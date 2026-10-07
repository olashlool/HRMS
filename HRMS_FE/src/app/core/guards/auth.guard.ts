import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../auth/auth.service';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  if (!auth.hasStoredSession) {
    return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
  }

  return auth.loadCurrentUser().pipe(
    map(() => true),
    catchError(() => {
      auth.clearSession();

      return of(router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } }));
    })
  );
};

/**
 * Hides routes the API would refuse anyway. This is navigation comfort, not a
 * security control: the permission claim lives in a token the user can read and
 * the real check happens server side on every request.
 */
export const permissionGuard = (permission: string): CanActivateFn => () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.can(permission) ? true : router.createUrlTree(['/dashboard']);
};
