import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const roleGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  if (authService.isAuthenticated() && authService.hasAnyRole(['owner', 'sysadmin'])) {
    return true;
  }
  
  return router.createUrlTree(['/dashboard']);
};
