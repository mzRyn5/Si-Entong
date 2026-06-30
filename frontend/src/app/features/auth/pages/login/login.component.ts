import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';

import { AuthService } from '../../../../core/auth/auth.service';
import { LanguageService } from '../../../../core/services/language.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, TranslatePipe, NgIf],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginComponent {
  username = '';
  password = '';
  loading = signal(false);
  showPassword = signal(false);

  constructor(
    private readonly authService: AuthService,
    readonly languageService: LanguageService,
    private readonly toastService: ToastService,
    private readonly router: Router
  ) {
    // If already logged in, redirect to the right landing area.
    if (this.authService.isAuthenticated()) {
      this.redirectAfterLogin();
    }
  }

  onSubmit(form: NgForm): void {
    if (form.invalid || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.authService.login({
      username: this.username.trim(),
      password: this.password
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.toastService.show(this.languageService.translate('Login berhasil!'), 'success');
        this.redirectAfterLogin();
      },
      error: (err: any) => {
        this.loading.set(false);
        // err is now the original HttpErrorResponse from the interceptor
        const errMsg = err?.error?.message || err?.message || this.languageService.translate('Login gagal, cek kembali username/password Anda.');
        this.toastService.show(errMsg, 'error');
      }
    });
  }

  toggleLanguage(): void {
    const current = this.languageService.currentLang();
    this.languageService.setLanguage(current === 'id' ? 'en' : 'id');
  }

  togglePasswordVisibility(): void {
    this.showPassword.update(v => !v);
  }

  private redirectAfterLogin(): void {
    const user = this.authService.getCurrentUser();
    const target = user?.role?.toLowerCase() === 'sysadmin'
      ? '/sysadmin/stores'
      : '/dashboard';

    void this.router.navigate([target]);
  }
}
