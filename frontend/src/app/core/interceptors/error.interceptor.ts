import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Don't intercept auth login/refresh requests — let the login component handle those errors directly
      const isAuthRequest = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');

      if (error.error instanceof ErrorEvent) {
        // Client-side error (network, etc.)
        // Pass through the original error so components can inspect status
        return throwError(() => error);
      }

      // Server-side error
      if (error.status === 401 && !isAuthRequest) {
        // Session expired — clear token and redirect to login
        localStorage.removeItem('access_token');
        localStorage.removeItem('refresh_token');
        localStorage.removeItem('auth_user');
        window.location.href = '/login';
      }

      // Propagate the original HttpErrorResponse so subscribers can inspect .status, .error, etc.
      return throwError(() => error);
    })
  );
};
