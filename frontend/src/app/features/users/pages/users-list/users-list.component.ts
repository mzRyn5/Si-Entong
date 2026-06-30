import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { UsersService, UserItem, UserCreateRequest, UserUpdateRequest } from '../../users.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { NgIf, NgFor, NgClass, AsyncPipe, DatePipe } from '@angular/common';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [
    FormsModule,
    BadgeComponent,
    LoadingStateComponent,
    PaginationComponent,
    TranslatePipe,
    NgIf,
    NgFor,
    NgClass,
    AsyncPipe,
    DatePipe
  ],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss']
})
export class UsersListComponent implements OnInit, OnDestroy {
  readonly users = signal<UserItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  searchQuery = '';
  filterRole = '';
  filterActive = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = signal(0);
  totalPages = signal(1);

  // Form Drawer (Create / Edit)
  showFormDrawer = signal(false);
  isEditMode = signal(false);
  selectedUserId = '';
  formModel = {
    name: '',
    username: '',
    password: '',
    role: 'admin',
    isActive: true,
    storeId: '' as string | null
  };
  saving = signal(false);

  // Reset Password Modal
  showResetModal = signal(false);
  resetTarget = signal<UserItem | null>(null);
  resetPasswordValue = '';
  resetting = signal(false);

  // Delete Dialog
  showDeleteDialog = signal(false);
  deleteTarget = signal<UserItem | null>(null);
  deleting = signal(false);

  // Store Dropdown for SysAdmin
  storesList = signal<any[]>([]);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly usersService: UsersService,
    readonly authService: AuthService,
    private readonly toastService: ToastService,
    private readonly languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadUsers();
      });

    this.loadUsers();

    if (this.isSysAdmin()) {
      this.loadStoresDropdown();
    } else {
      // For Owner, role can only be admin
      this.formModel.role = 'admin';
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  isSysAdmin(): boolean {
    return this.authService.hasAnyRole(['sysadmin']);
  }

  isOwner(): boolean {
    return this.authService.hasAnyRole(['owner']);
  }

  loadUsers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.usersService.getAll({
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchQuery.trim() || undefined,
      role: this.filterRole || undefined,
      isActive: this.filterActive === 'true' ? true : this.filterActive === 'false' ? false : undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          // SysAdmin sees all users (owner and admin), Owner sees only their store's users
          this.users.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || this.languageService.translate('Gagal memuat data pengguna.'));
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || this.languageService.translate('Gagal memuat data pengguna.'));
      }
    });
  }

  loadStoresDropdown(): void {
    this.usersService.getStoresDropdown()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.storesList.set(res.data);
          }
        },
        error: (err) => {
          this.toastService.show(err?.error?.message || this.languageService.translate('Gagal memuat daftar toko.'), 'error');
        }
      });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.filterRole = '';
    this.filterActive = '';
    this.currentPage = 1;
    this.loadUsers();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadUsers();
  }

  openCreateForm(): void {
    this.isEditMode.set(false);
    this.selectedUserId = '';
    this.formModel = {
      name: '',
      username: '',
      password: '',
      role: this.isSysAdmin() ? 'owner' : 'admin',
      isActive: true,
      storeId: ''
    };
    this.showFormDrawer.set(true);
  }

  openEditForm(u: UserItem): void {
    this.isEditMode.set(true);
    this.selectedUserId = u.id;
    this.formModel = {
      name: u.name,
      username: u.username,
      password: '', // Password is not modified here
      role: u.role,
      isActive: u.isActive,
      storeId: u.storeId || ''
    };
    this.showFormDrawer.set(true);
  }

  closeFormDrawer(): void {
    this.showFormDrawer.set(false);
  }

  onSubmitForm(form: NgForm): void {
    if (form.invalid) return;

    this.saving.set(true);

    if (this.isEditMode()) {
      const request: UserUpdateRequest = {
        name: this.formModel.name.trim(),
        role: this.formModel.role,
        isActive: this.formModel.isActive,
        storeId: this.isSysAdmin() && this.formModel.storeId ? this.formModel.storeId : null
      };

      this.usersService.update(this.selectedUserId, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show(this.languageService.translate('User berhasil diperbarui.'), 'success');
              this.loadUsers();
            } else {
              this.toastService.show(res.message || this.languageService.translate('Gagal menyimpan user.'), 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || this.languageService.translate('Gagal menyimpan user.'), 'error');
          }
        });
    } else {
      const request: UserCreateRequest = {
        name: this.formModel.name.trim(),
        username: this.formModel.username.trim(),
        password: this.formModel.password,
        role: this.formModel.role,
        isActive: this.formModel.isActive,
        storeId: this.isSysAdmin() && this.formModel.storeId ? this.formModel.storeId : null
      };

      this.usersService.create(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show(this.languageService.translate('User berhasil ditambahkan.'), 'success');
              this.loadUsers();
            } else {
              this.toastService.show(res.message || this.languageService.translate('Gagal menambahkan user.'), 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || this.languageService.translate('Gagal menambahkan user.'), 'error');
          }
        });
    }
  }

  openResetPassword(u: UserItem): void {
    this.resetTarget.set(u);
    this.resetPasswordValue = '';
    this.showResetModal.set(true);
  }

  closeResetModal(): void {
    this.showResetModal.set(false);
    this.resetTarget.set(null);
  }

  executeResetPassword(): void {
    const target = this.resetTarget();
    if (!target || !this.resetPasswordValue || this.resetting()) return;

    this.resetting.set(true);
    this.usersService.resetPassword(target.id, { newPassword: this.resetPasswordValue.trim() })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.resetting.set(false);
          this.showResetModal.set(false);
          if (res.success) {
            this.toastService.show(this.languageService.translate('Password berhasil direset.'), 'success');
          } else {
            this.toastService.show(res.message || this.languageService.translate('Gagal mereset password.'), 'error');
          }
        },
        error: (err) => {
          this.resetting.set(false);
          this.toastService.show(err?.error?.message || this.languageService.translate('Gagal mereset password.'), 'error');
        }
      });
  }

  openDeleteConfirm(u: UserItem): void {
    this.deleteTarget.set(u);
    this.showDeleteDialog.set(true);
  }

  closeDeleteDialog(): void {
    this.showDeleteDialog.set(false);
    this.deleteTarget.set(null);
  }

  executeDelete(): void {
    const target = this.deleteTarget();
    if (!target || this.deleting()) return;

    this.deleting.set(true);
    this.usersService.delete(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleting.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show(this.languageService.translate('User berhasil dihapus.'), 'success');
            this.loadUsers();
          } else {
            this.toastService.show(res.message || this.languageService.translate('Gagal menghapus user.'), 'error');
          }
        },
        error: (err) => {
          this.deleting.set(false);
          this.toastService.show(err?.error?.message || this.languageService.translate('Gagal menghapus user.'), 'error');
        }
      });
  }

  toggleUserStatus(u: UserItem): void {
    if (this.saving()) return;

    if (!u.isActive && u.role === 'Admin') {
      const activeAdminCount = this.users().filter(usr => usr.storeId === u.storeId && usr.role === 'Admin' && usr.isActive).length;
      if (activeAdminCount >= 3) {
        this.toastService.show(this.languageService.translate('Toko ini sudah mencapai batas maksimum 3 admin penjaga aktif.'), 'error');
        return;
      }
    }

    const newStatus = !u.isActive;
    this.saving.set(true);

    const request: UserUpdateRequest = {
      name: u.name,
      role: u.role,
      isActive: newStatus,
      storeId: u.storeId
    };

    this.usersService.update(u.id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          if (res.success) {
            const successMsg = newStatus
              ? this.languageService.translate('User berhasil diaktifkan.')
              : this.languageService.translate('User berhasil dinonaktifkan.');
            this.toastService.show(successMsg, 'success');
            this.loadUsers();
          } else {
            this.toastService.show(res.message || this.languageService.translate('Gagal mengubah status user.'), 'error');
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.show(err?.error?.message || this.languageService.translate('Gagal mengubah status user.'), 'error');
        }
      });
  }

  getStoreName(storeId?: string | null): string {
    if (!storeId) return 'Global (SysAdmin)';
    const store = this.storesList().find(s => s.id === storeId);
    return store ? store.name : `Toko (${storeId.substring(0, 8)})`;
  }
}
