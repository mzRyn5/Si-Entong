import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { SettingsService } from '../../settings.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { StoreProfile, StoreSettings } from '../../../../core/models/store.model';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './settings-page.component.html',
  styleUrl: './settings-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SettingsPageComponent implements OnInit {
  activeTab: 'profile' | 'settings' | 'security' = 'profile';

  profileForm!: FormGroup;
  settingsForm!: FormGroup;
  passwordForm!: FormGroup;

  isProfileLoading = false;
  isSettingsLoading = false;
  isPasswordLoading = false;

  constructor(
    private readonly fb: FormBuilder,
    private readonly settingsService: SettingsService,
    private readonly authService: AuthService,
    private readonly toastService: ToastService,
    private readonly cdr: ChangeDetectorRef,
    private readonly languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.initForms();
    this.loadProfile();
    this.loadSettings();
  }

  private initForms(): void {
    this.profileForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      address: ['', [Validators.maxLength(500)]],
      phone: ['', [Validators.maxLength(20)]],
      currency: ['IDR', [Validators.required, Validators.maxLength(10)]],
      timezone: ['Asia/Jakarta', [Validators.required, Validators.maxLength(50)]],
      logoUrl: ['', [Validators.maxLength(500)]],
      receiptFooter: ['', [Validators.maxLength(500)]]
    });

    this.settingsForm = this.fb.group({
      allowNegativeStock: [false],
      requireCashSessionForSales: [true],
      defaultLowStockThreshold: [10, [Validators.required, Validators.min(0)]],
      enableBarcode: [true],
      enablePurchasePriceTracking: [true],
      defaultPaymentMethod: ['Cash', [Validators.required]]
    });

    this.passwordForm = this.fb.group({
      oldPassword: ['', [Validators.required]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  private passwordMatchValidator(g: FormGroup) {
    const newPwd = g.get('newPassword')?.value;
    const confirmPwd = g.get('confirmPassword')?.value;
    return newPwd === confirmPwd ? null : { mismatch: true };
  }

  setTab(tab: 'profile' | 'settings' | 'security'): void {
    this.activeTab = tab;
  }

  loadProfile(): void {
    this.isProfileLoading = true;
    this.settingsService.getProfile().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.profileForm.patchValue(res.data);
        }
        this.isProfileLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toastService.error(this.languageService.translate('Gagal mengambil profil toko.'));
        this.isProfileLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  loadSettings(): void {
    this.isSettingsLoading = true;
    this.settingsService.getSettings().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.settingsForm.patchValue(res.data);
        }
        this.isSettingsLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toastService.error(this.languageService.translate('Gagal mengambil pengaturan toko.'));
        this.isSettingsLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;

    this.isProfileLoading = true;
    const data: StoreProfile = this.profileForm.value;

    this.settingsService.updateProfile(data).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastService.success(this.languageService.translate('Profil toko berhasil diperbarui.'));
        } else {
          this.toastService.error(res.message || this.languageService.translate('Gagal memperbarui profil toko.'));
        }
        this.isProfileLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toastService.error(this.languageService.translate('Terjadi kesalahan saat memperbarui profil toko.'));
        this.isProfileLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  saveSettings(): void {
    if (this.settingsForm.invalid) return;

    this.isSettingsLoading = true;
    const data: StoreSettings = this.settingsForm.value;

    this.settingsService.updateSettings(data).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastService.success(this.languageService.translate('Pengaturan toko berhasil diperbarui.'));
        } else {
          this.toastService.error(res.message || this.languageService.translate('Gagal memperbarui pengaturan toko.'));
        }
        this.isSettingsLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.toastService.error(this.languageService.translate('Terjadi kesalahan saat memperbarui pengaturan toko.'));
        this.isSettingsLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) return;

    this.isPasswordLoading = true;
    const { oldPassword, newPassword } = this.passwordForm.value;

    this.authService.changePassword({ oldPassword, newPassword }).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastService.success(this.languageService.translate('Password Anda berhasil diubah.'));
          this.passwordForm.reset();
        } else {
          this.toastService.error(res.message || this.languageService.translate('Gagal mengubah password.'));
        }
        this.isPasswordLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        const errMsg = err.error?.message || this.languageService.translate('Password lama Anda salah atau terjadi kesalahan server.');
        this.toastService.error(errMsg);
        this.isPasswordLoading = false;
        this.cdr.markForCheck();
      }
    });
  }
}
