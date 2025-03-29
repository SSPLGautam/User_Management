import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.getToken()) {
    const cloned = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${authService.getToken()}`)
    });

    return next(cloned).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401) {
          authService.refreshToken({
            email: authService.getUserDetail()?.email,
            token: authService.getToken() || null,
            refreshToken: authService.getRefreshToken() || null
          }).subscribe({
            next: (response) => {
              if (response.isSuccess) {
                localStorage.setItem('user', response.token);
                const cloned = req.clone({
                  setHeaders: {
                    Authorization: `Bearer ${response.token}`
                  }
                });
                location.reload();
              }
            },
            error: () => {
              authService.logout();
              router.navigate(['/login']);
            }
          });
        }
        return throwError(error);
      })
    );
  }
  return next(req);
};
