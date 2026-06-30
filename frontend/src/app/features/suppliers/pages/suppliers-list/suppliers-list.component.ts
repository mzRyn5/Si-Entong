import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { SuppliersService, SupplierItem, SupplierCreateRequest, SupplierUpdateRequest } from '../../suppliers.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { NgIf, NgFor, NgClass } from '@angular/common';

@Component({
  selector: 'app-suppliers-list',
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
  templateUrl: './suppliers-list.component.html',
  styleUrls: ['./suppliers-list.component.scss']
})
export class SuppliersListComponent implements OnInit, OnDestroy {
  readonly suppliers = signal<SupplierItem[]>([]);
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
  selectedSupplierId = '';
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
  deleteTarget = signal<SupplierItem | null>(null);
  deleting = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly suppliersService: SuppliersService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadSuppliers();
      });

    this.loadSuppliers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadSuppliers(): void {
    this.loading.set(true);
    this.error.set(null);

    this.suppliersService.getAll({
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
          this.suppliers.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat supplier.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat supplier.');
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
    this.loadSuppliers();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadSuppliers();
  }

  openCreateForm(): void {
    this.isEditMode.set(false);
    this.selectedSupplierId = '';
    this.formModel = {
      name: '',
      phone: '',
      address: '',
      notes: '',
      isActive: true
    };
    this.showFormDrawer.set(true);
  }

  openEditForm(s: SupplierItem): void {
    this.isEditMode.set(true);
    this.selectedSupplierId = s.id;
    this.formModel = {
      name: s.name,
      phone: '', // phone/address can be loaded or default empty
      address: '',
      notes: '',
      isActive: s.isActive !== false
    };

    // Prefetch detail if necessary, otherwise just use item properties
    this.suppliersService.getById(s.id)
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
      const request: SupplierUpdateRequest = {
        name: this.formModel.name.trim(),
        phone: this.formModel.phone.trim() || undefined,
        address: this.formModel.address.trim() || undefined,
        notes: this.formModel.notes.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.suppliersService.update(this.selectedSupplierId, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Supplier berhasil diperbarui.', 'success');
              this.loadSuppliers();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan supplier.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan supplier.', 'error');
          }
        });
    } else {
      const request: SupplierCreateRequest = {
        name: this.formModel.name.trim(),
        phone: this.formModel.phone.trim() || undefined,
        address: this.formModel.address.trim() || undefined,
        notes: this.formModel.notes.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.suppliersService.create(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Supplier berhasil ditambahkan.', 'success');
              this.loadSuppliers();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan supplier.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan supplier.', 'error');
          }
        });
    }
  }

  openDeleteConfirm(s: SupplierItem): void {
    this.deleteTarget.set(s);
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
    this.suppliersService.delete(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleting.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show('Supplier berhasil dihapus.', 'success');
            this.loadSuppliers();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus supplier.', 'error');
          }
        },
        error: (err) => {
          this.deleting.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menghapus supplier.', 'error');
        }
      });
  }
}
