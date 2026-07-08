import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiClientService } from '../../../../core/services/api-client.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { StoreProfileDto, SysadminStoresService } from '../../sysadmin-stores.service';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-sysadmin-stores',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    BadgeComponent,
    LoadingStateComponent,
    TranslatePipe
  ],
  templateUrl: './sysadmin-stores.component.html',
  styleUrls: ['./sysadmin-stores.component.scss']
})
export class SysadminStoresComponent implements OnInit {
  readonly stores = signal<StoreProfileDto[]>([]);
  readonly filteredStores = signal<StoreProfileDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly searchTerm = signal('');

  // Modals
  readonly showCreateModal = signal(false);
  readonly showEditModal = signal(false);
  readonly submitting = signal(false);
  readonly modalError = signal<string | null>(null);

  createForm!: FormGroup;
  editForm!: FormGroup;
  selectedStoreId: string | null = null;

  readonly timezones = [
    { value: 'Asia/Jakarta', label: 'WIB (Asia/Jakarta)' },
    { value: 'Asia/Makassar', label: 'WITA (Asia/Makassar)' },
    { value: 'Asia/Jayapura', label: 'WIT (Asia/Jayapura)' },
    { value: 'UTC', label: 'UTC' }
  ];

  constructor(
    private readonly apiClient: ApiClientService,
    private readonly storesService: SysadminStoresService,
    private readonly fb: FormBuilder,
    private readonly languageService: LanguageService
  ) {
    this.initForms();
  }

  ngOnInit(): void {
    this.loadStores();
  }

  initForms(): void {
    this.createForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      address: ['', [Validators.maxLength(500)]],
      phone: ['', [Validators.maxLength(20)]],
      currency: ['IDR', [Validators.required, Validators.maxLength(10)]],
      timezone: ['Asia/Jakarta', [Validators.required, Validators.maxLength(50)]],
      logoUrl: ['', [Validators.maxLength(500)]],
      receiptFooter: ['', [Validators.maxLength(500)]],
      ownerName: ['', [Validators.required, Validators.maxLength(100)]],
      ownerUsername: ['', [Validators.required, Validators.maxLength(50)]],
      ownerPassword: ['', [Validators.required, Validators.minLength(6)]]
    });

    this.editForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      address: ['', [Validators.maxLength(500)]],
      phone: ['', [Validators.maxLength(20)]],
      currency: ['IDR', [Validators.required, Validators.maxLength(10)]],
      timezone: ['Asia/Jakarta', [Validators.required, Validators.maxLength(50)]],
      logoUrl: ['', [Validators.maxLength(500)]],
      receiptFooter: ['', [Validators.maxLength(500)]]
    });
  }

  loadStores(): void {
    this.loading.set(true);
    this.error.set(null);
    this.storesService.getStores().subscribe({
      next: (stores) => {
        this.loading.set(false);
        this.stores.set(stores);
        this.filterStores();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || err?.message || this.languageService.translate('Terjadi kesalahan saat memuat data toko.'));
      }
    });
  }

  filterStores(): void {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      this.filteredStores.set(this.stores());
      return;
    }

    const filtered = this.stores().filter(s =>
      s.name.toLowerCase().includes(term) ||
      s.address.toLowerCase().includes(term) ||
      s.phone.toLowerCase().includes(term)
    );
    this.filteredStores.set(filtered);
  }

  onSearchChange(val: string): void {
    this.searchTerm.set(val);
    this.filterStores();
  }

  openCreateModal(): void {
    this.createForm.reset({
      currency: 'IDR',
      timezone: 'Asia/Jakarta'
    });
    this.modalError.set(null);
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    this.showCreateModal.set(false);
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.modalError.set(null);
    this.apiClient.post<any>('/sysadmin/stores', this.createForm.value).subscribe({
      next: (res) => {
        this.submitting.set(false);
        if (res.success) {
          this.showCreateModal.set(false);
          this.loadStores();
        } else {
          this.modalError.set(res.message || this.languageService.translate('Gagal mendaftarkan toko baru.'));
        }
      },
      error: (err) => {
        this.submitting.set(false);
        this.modalError.set(err?.error?.message || this.languageService.translate('Terjadi kesalahan sistem saat mendaftarkan toko.'));
      }
    });
  }

  openEditModal(store: StoreProfileDto): void {
    this.selectedStoreId = store.id;
    this.editForm.setValue({
      name: store.name,
      address: store.address || '',
      phone: store.phone || '',
      currency: store.currency || 'IDR',
      timezone: store.timezone || 'Asia/Jakarta',
      logoUrl: store.logoUrl || '',
      receiptFooter: store.receiptFooter || ''
    });
    this.modalError.set(null);
    this.showEditModal.set(true);
  }

  closeEditModal(): void {
    this.showEditModal.set(false);
    this.selectedStoreId = null;
  }

  submitEdit(): void {
    if (this.editForm.invalid || !this.selectedStoreId) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.modalError.set(null);
    this.apiClient.put<any>(`/sysadmin/stores/${this.selectedStoreId}`, this.editForm.value).subscribe({
      next: (res) => {
        this.submitting.set(false);
        if (res.success) {
          this.showEditModal.set(false);
          this.selectedStoreId = null;
          this.loadStores();
        } else {
          this.modalError.set(res.message || this.languageService.translate('Gagal memperbarui profil toko.'));
        }
      },
      error: (err) => {
        this.submitting.set(false);
        this.modalError.set(err?.error?.message || this.languageService.translate('Terjadi kesalahan saat memperbarui profil toko.'));
      }
    });
  }

  toggleStoreStatus(store: StoreProfileDto): void {
    const confirmTemplate = store.isActive
      ? 'Apakah Anda yakin ingin menonaktifkan toko {0}? Semua akun user dari toko ini akan ikut terpengaruh.'
      : 'Apakah Anda yakin ingin mengaktifkan toko {0}? Semua akun user dari toko ini akan ikut terpengaruh.';
    
    const confirmMsg = this.languageService.translate(confirmTemplate).replace('{0}', `"${store.name}"`);
    
    if (!confirm(confirmMsg)) {
      return;
    }

    this.apiClient.post<any>(`/sysadmin/stores/${store.id}/toggle-status`, {}).subscribe({
      next: (res) => {
        if (res.success) {
          this.loadStores();
          
          const successTemplate = store.isActive
            ? 'Toko berhasil dinonaktifkan.'
            : 'Toko berhasil diaktifkan.';
          alert(this.languageService.translate(successTemplate));
        } else {
          alert(res.message || this.languageService.translate('Gagal mengubah status toko.'));
        }
      },
      error: (err) => {
        alert(err?.error?.message || this.languageService.translate('Terjadi kesalahan saat mengubah status toko.'));
      }
    });
  }

  deleteStore(store: StoreProfileDto): void {
    const confirmMsg = this.languageService.translate('Apakah Anda yakin ingin menghapus toko {0}? Semua data yang berkaitan dengan toko ini termasuk data transaksi, produk, dan akun user akan dihapus secara permanen.').replace('{0}', `"${store.name}"`);
    
    if (!confirm(confirmMsg)) {
      return;
    }

    this.apiClient.delete<any>(`/sysadmin/stores/${store.id}`).subscribe({
      next: (res) => {
        if (res.success) {
          this.loadStores();
          alert(this.languageService.translate('Toko berhasil dihapus.'));
        } else {
          alert(res.message || this.languageService.translate('Gagal menghapus toko.'));
        }
      },
      error: (err) => {
        alert(err?.error?.message || this.languageService.translate('Terjadi kesalahan saat menghapus toko.'));
      }
    });
  }

  formatDate(dateStr: string): string {
    try {
      const d = new Date(dateStr);
      const locale = this.languageService.currentLang() === 'en' ? 'en-US' : 'id-ID';
      return d.toLocaleDateString(locale, {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
      });
    } catch {
      return dateStr;
    }
  }
}
