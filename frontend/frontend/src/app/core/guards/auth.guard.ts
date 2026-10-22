import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    return true;
  }

  router.navigate(['/login']);
  return false;
};

export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const expectedRoles = route.data?.['roles'] as string[] | undefined;
  if (!expectedRoles || expectedRoles.length === 0) {
    return true;
  }

  if (authService.hasRole(expectedRoles)) {
    return true;
  }

  router.navigate(['/libros']);
  return false;
};
