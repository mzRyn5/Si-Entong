import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('normalizes sysAdmin role returned by login response', () => {
    service.login({ username: 'sysAdmin', password: 'password' }).subscribe(session => {
      expect(session.user.role).toBe('sysadmin');
      expect(service.getCurrentUser()?.role).toBe('sysadmin');
      expect(service.hasAnyRole(['sysadmin'])).toBeTrue();
    });

    const req = httpMock.expectOne('/api/v1/auth/login');
    req.flush({
      success: true,
      data: {
        accessToken: 'access-token',
        refreshToken: 'refresh-token',
        expiresIn: 3600,
        user: {
          id: 'user-1',
          name: 'System Admin',
          username: 'sysAdmin',
          role: 'SysAdmin'
        }
      }
    });
  });
});
