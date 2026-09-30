import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { CurrentUserService } from './current-user.service';

export const userHeaderInterceptor: HttpInterceptorFn = (req, next) => {
  const user = inject(CurrentUserService).name();
  return next(req.clone({ setHeaders: { 'X-User-Name': user } }));
};
