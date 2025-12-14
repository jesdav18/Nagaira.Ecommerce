import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const user = authService.currentUser();
  
  if (!authService.isAuthenticated() || !user) {
    router.navigate(['/login']);
    return false;
  }

  const isAdmin = user.role === 'Admin' || user.role === 'SuperAdmin';
  
  if (!isAdmin) {
    router.navigate(['/']);
    return false;
  }

  return true;
};

