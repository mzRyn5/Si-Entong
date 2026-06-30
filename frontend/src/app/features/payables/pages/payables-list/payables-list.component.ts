import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { PayablesService, PayableListItem, PayableDetail } from '../../../../core/services/payables.service';
import { SuppliersService, SupplierItem } from '../../../suppliers/suppliers.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-payables-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BadgeComponent,
    FilterBarComponent,
    LoadingStateComponent,
    PaginationComponent,
    TranslatePipe,
    CurrencyIdrPipe,
    DateIdPipe
  ],
  templateUrl: './payables-list.component.html'
})
export class PayablesListComponent implements OnInit, OnDestroy {
  readonly payables = signal<PayableListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // Filters
  filterSupplier = '';
  filterStatus = '';

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalItems = signal(0);
  totalPages = signal(1);

  // Suppliers for filter
  readonly suppliers = signal<SupplierItem[]>([]);

  // Detail Drawer
  showDetailDrawer = signal(false);
  detailLoading = signal(false);
  detailData = signal<PayableDetail | null>(null);
  showAddPaymentRow = signal(false);

  paymentSaving = signal(false);
  paymentForm = {
    paymentDate: new Date().toISOString().substring(0, 10),
    amount: 0,
    paymentMethod: 'Cash',
    notes: ''
  };

  // Delete Dialog (Owner only)
  showDeleteDialog = signal(false);
  deleteTarget = signal<PayableListItem | PayableDetail | null>(null);
  deleteSaving = signal(false);
  isOwner = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly payablesService: PayablesService,
    private readonly suppliersService: SuppliersService,
    private readonly toastService: ToastService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isOwner.set(this.authService.hasAnyRole(['owner']));

    // debounce filter changes
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadPayables();
      });

    this.loadSuppliers();
    this.loadPayables();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadSuppliers(): void {
    this.suppliersService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.suppliers.set(res.data);
          }
        }
      });
  }

  loadPayables(): void {
    this.loading.set(true);
    this.error.set(null);

    this.payablesService.getAll({
      page: this.currentPage,
      pageSize: this.pageSize,
      supplierId: this.filterSupplier || undefined,
      status: this.filterStatus || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.payables.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat daftar hutang.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat daftar hutang.');
      }
    });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.filterSupplier = '';
    this.filterStatus = '';
    this.currentPage = 1;
    this.loadPayables();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadPayables();
  }

  viewDetail(id: string): void {
    this.detailLoading.set(true);
    this.showDetailDrawer.set(true);
    this.detailData.set(null);
    this.showAddPaymentRow.set(false);

    this.payablesService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.detailLoading.set(false);
          if (res.success) {
            this.detailData.set(res.data);
            this.paymentForm = {
              paymentDate: new Date().toISOString().substring(0, 10),
              amount: res.data.remainingAmount,
              paymentMethod: 'Cash',
              notes: ''
            };
          } else {
            this.toastService.show(res.message || 'Gagal memuat rincian hutang.', 'error');
            this.closeDetailDrawer();
          }
        },
        error: (err) => {
          this.detailLoading.set(false);
          this.toastService.show(err?.error?.message || 'Gagal memuat rincian hutang.', 'error');
          this.closeDetailDrawer();
        }
      });
  }

  closeDetailDrawer(): void {
    this.showDetailDrawer.set(false);
    this.detailData.set(null);
    this.showAddPaymentRow.set(false);
  }

  toggleAddPaymentRow(show: boolean): void {
    this.showAddPaymentRow.set(show);
    const detail = this.detailData();
    if (show && detail) {
      this.paymentForm = {
        paymentDate: new Date().toISOString().substring(0, 10),
        amount: detail.remainingAmount,
        paymentMethod: 'Cash',
        notes: ''
      };
    }
  }

  submitPaymentDirect(): void {
    const detail = this.detailData();
    if (!detail || this.paymentSaving()) return;

    if (!this.paymentForm.paymentDate) {
      this.toastService.show('Tanggal pembayaran wajib diisi.', 'error');
      return;
    }
    if (!this.paymentForm.amount || this.paymentForm.amount <= 0) {
      this.toastService.show('Nominal pembayaran harus lebih dari 0.', 'error');
      return;
    }
    if (this.paymentForm.amount > detail.remainingAmount) {
      this.toastService.show('Nominal pembayaran melebihi sisa hutang.', 'error');
      return;
    }

    this.paymentSaving.set(true);

    this.payablesService.recordPayment(detail.id, {
      paymentDate: new Date(this.paymentForm.paymentDate).toISOString(),
      amount: this.paymentForm.amount,
      paymentMethod: this.paymentForm.paymentMethod,
      notes: this.paymentForm.notes || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.paymentSaving.set(false);
        if (res.success) {
          this.toastService.show('Pembayaran hutang berhasil dicatat.', 'success');
          
          // Hide add payment row
          this.showAddPaymentRow.set(false);

          // Refresh detail data
          this.detailData.set(res.data);
          
          // Reset payment form for remaining amount
          this.paymentForm = {
            paymentDate: new Date().toISOString().substring(0, 10),
            amount: res.data.remainingAmount,
            paymentMethod: 'Cash',
            notes: ''
          };
          
          // Refresh list
          this.loadPayables();
        } else {
          this.toastService.show(res.message || 'Gagal mencatat pembayaran.', 'error');
        }
      },
      error: (err) => {
        this.paymentSaving.set(false);
        this.toastService.show(err?.error?.message || 'Kesalahan saat menyimpan pembayaran.', 'error');
      }
    });
  }

  deletePaymentDirect(paymentId: string): void {
    const detail = this.detailData();
    if (!detail) return;

    if (!confirm('Apakah Anda yakin ingin menghapus riwayat pembayaran ini?')) {
      return;
    }

    this.detailLoading.set(true);

    this.payablesService.cancelPayment(detail.id, paymentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.detailLoading.set(false);
          if (res.success) {
            this.toastService.show('Pembayaran berhasil dihapus.', 'success');
            // Refresh detail data
            this.detailData.set(res.data);
            
            // Reset payment form for remaining amount
            this.paymentForm = {
              paymentDate: new Date().toISOString().substring(0, 10),
              amount: res.data.remainingAmount,
              paymentMethod: 'Cash',
              notes: ''
            };
            
            // Refresh list
            this.loadPayables();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus pembayaran.', 'error');
          }
        },
        error: (err) => {
          this.detailLoading.set(false);
          this.toastService.show(err?.error?.message || 'Kesalahan saat menghapus pembayaran.', 'error');
        }
      });
  }

  resetPaymentFormDirect(remainingAmount: number): void {
    this.paymentForm = {
      paymentDate: new Date().toISOString().substring(0, 10),
      amount: remainingAmount,
      paymentMethod: 'Cash',
      notes: ''
    };
    this.toastService.show('Formulir direset.', 'success');
  }

  getPaymentStatusBadgeType(status: string): 'warning' | 'success' | 'danger' | 'info' {
    switch (status.toLowerCase()) {
      case 'unpaid':
        return 'danger';
      case 'partial':
        return 'warning';
      case 'paid':
        return 'success';
      default:
        return 'info';
    }
  }

  openDeleteConfirm(p: PayableListItem): void {
    this.deleteTarget.set(p);
    this.showDeleteDialog.set(true);
  }

  openDeleteConfirmFromDetail(detail: PayableDetail): void {
    this.deleteTarget.set(detail);
    this.showDeleteDialog.set(true);
  }

  closeDeleteDialog(): void {
    if (this.deleteSaving()) return;
    this.showDeleteDialog.set(false);
    this.deleteTarget.set(null);
  }

  executeDelete(): void {
    const target = this.deleteTarget();
    if (!target || this.deleteSaving()) return;

    this.deleteSaving.set(true);
    this.payablesService.deletePayable(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleteSaving.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show('Hutang berhasil dihapus.', 'success');
            this.closeDetailDrawer();
            this.loadPayables();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus hutang.', 'error');
          }
        },
        error: (err) => {
          this.deleteSaving.set(false);
          this.toastService.show(err?.error?.message || 'Kesalahan sistem saat menghapus hutang.', 'error');
        }
      });
  }
}
