import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { CustomersService, CustomerItem, CustomerCreateRequest, CustomerUpdateRequest } from '../../customers.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { NgIf, NgFor, NgClass } from '@angular/common';

@Component({
  selector: 'app-customers-list',
  standalone: true,
  imports: [
    FormsModule,
    BadgeComponent,
    FilterBarComponent,
    LoadingStateComponent,
    PaginationComponent,
    TranslatePipe,
    NgIf,
    NgFor,
    NgClass
  ],
  templateUrl: './customers-list.component.html',
  styleUrls: ['./customers-list.component.scss']
})
export class CustomersListComponent implements OnInit, OnDestroy {
  readonly customers = signal<CustomerItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  searchQuery = '';
  filterActive = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = signal(0);
  totalPages = signal(1);

  // Form Drawer
  showFormDrawer = signal(false);
  isEditMode = signal(false);
  selectedCustomerId = '';
  formModel = {
    name: '',
    phone: '',
    address: '',
    notes: '',
    isActive: true
  };
  saving = signal(false);

  // Delete Dialog
  showDeleteDialog = signal(false);
  deleteTarget = signal<CustomerItem | null>(null);
  deleting = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly customersService: CustomersService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadCustomers();
      });

    this.loadCustomers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCustomers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.customersService.getAll({
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchQuery.trim() || undefined,
      isActive: this.filterActive === 'true' ? true : this.filterActive === 'false' ? false : undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.customers.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat pelanggan.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat pelanggan.');
      }
    });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.filterActive = '';
    this.currentPage = 1;
    this.loadCustomers();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadCustomers();
  }

  openCreateForm(): void {
    this.isEditMode.set(false);
    this.selectedCustomerId = '';
    this.formModel = {
      name: '',
      phone: '',
      address: '',
      notes: '',
      isActive: true
    };
    this.showFormDrawer.set(true);
  }

  openEditForm(c: CustomerItem): void {
    this.isEditMode.set(true);
    this.selectedCustomerId = c.id;
    this.formModel = {
      name: c.name,
      phone: '',
      address: '',
      notes: '',
      isActive: c.isActive !== false
    };

    // Prefetch detail if necessary, otherwise just use item properties
    this.customersService.getById(c.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            const data: any = res.data;
            this.formModel = {
              name: data.name,
              phone: data.phone || '',
              address: data.address || '',
              notes: data.notes || '',
              isActive: data.isActive !== false
            };
          }
        }
      });

    this.showFormDrawer.set(true);
  }

  closeFormDrawer(): void {
    this.showFormDrawer.set(false);
  }

  onSubmitForm(form: NgForm): void {
    if (form.invalid) return;

    this.saving.set(true);

    if (this.isEditMode()) {
      const request: CustomerUpdateRequest = {
        name: this.formModel.name.trim(),
        phone: this.formModel.phone.trim() || undefined,
        address: this.formModel.address.trim() || undefined,
        notes: this.formModel.notes.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.customersService.update(this.selectedCustomerId, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Pelanggan berhasil diperbarui.', 'success');
              this.loadCustomers();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan pelanggan.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan pelanggan.', 'error');
          }
        });
    } else {
      const request: CustomerCreateRequest = {
        name: this.formModel.name.trim(),
        phone: this.formModel.phone.trim() || undefined,
        address: this.formModel.address.trim() || undefined,
        notes: this.formModel.notes.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.customersService.create(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Pelanggan berhasil ditambahkan.', 'success');
              this.loadCustomers();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan pelanggan.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan pelanggan.', 'error');
          }
        });
    }
  }

  openDeleteConfirm(c: CustomerItem): void {
    this.deleteTarget.set(c);
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
    this.customersService.delete(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleting.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show('Pelanggan berhasil dihapus.', 'success');
            this.loadCustomers();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus pelanggan.', 'error');
          }
        },
        error: (err) => {
          this.deleting.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menghapus pelanggan.', 'error');
        }
      });
  }
}
