import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { ReceivablesService, ReceivableListItem, ReceivableDetail } from '../../../../core/services/receivables.service';
import { CustomersService, CustomerItem } from '../../../customers/customers.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';

@Component({
  selector: 'app-receivables-list',
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
  templateUrl: './receivables-list.component.html'
})
export class ReceivablesListComponent implements OnInit, OnDestroy {
  readonly receivables = signal<ReceivableListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  // Filters
  filterCustomer = '';
  filterStatus = '';

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalItems = signal(0);
  totalPages = signal(1);

  // Customers for filter
  readonly customers = signal<CustomerItem[]>([]);

  // Detail Drawer
  showDetailDrawer = signal(false);
  detailLoading = signal(false);
  detailData = signal<ReceivableDetail | null>(null);

  // Record Payment Drawer
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
  deleteTarget = signal<ReceivableListItem | ReceivableDetail | null>(null);
  deleteSaving = signal(false);
  isOwner = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly receivablesService: ReceivablesService,
    private readonly customersService: CustomersService,
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
        this.loadReceivables();
      });

    this.loadCustomers();
    this.loadReceivables();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCustomers(): void {
    this.customersService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.customers.set(res.data);
          }
        }
      });
  }

  loadReceivables(): void {
    this.loading.set(true);
    this.error.set(null);

    this.receivablesService.getAll({
      page: this.currentPage,
      pageSize: this.pageSize,
      customerId: this.filterCustomer || undefined,
      status: this.filterStatus || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.receivables.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat daftar piutang.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat daftar piutang.');
      }
    });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.filterCustomer = '';
    this.filterStatus = '';
    this.currentPage = 1;
    this.loadReceivables();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadReceivables();
  }

  viewDetail(id: string): void {
    this.showAddPaymentRow.set(false);
    this.detailLoading.set(true);
    this.showDetailDrawer.set(true);
    this.detailData.set(null);

    this.receivablesService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.detailLoading.set(false);
          if (res.success) {
            this.detailData.set(res.data);
          } else {
            this.toastService.show(res.message || 'Gagal memuat rincian piutang.', 'error');
            this.closeDetailDrawer();
          }
        },
        error: (err) => {
          this.detailLoading.set(false);
          this.toastService.show(err?.error?.message || 'Gagal memuat rincian piutang.', 'error');
          this.closeDetailDrawer();
        }
      });
  }

  closeDetailDrawer(): void {
    this.showAddPaymentRow.set(false);
    this.showDetailDrawer.set(false);
    this.detailData.set(null);
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
      this.toastService.show('Nominal pembayaran melebihi sisa piutang.', 'error');
      return;
    }

    this.paymentSaving.set(true);

    this.receivablesService.recordPayment(detail.id, {
      paymentDate: new Date(this.paymentForm.paymentDate).toISOString(),
      amount: this.paymentForm.amount,
      paymentMethod: this.paymentForm.paymentMethod,
      notes: this.paymentForm.notes.trim() || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.paymentSaving.set(false);
        if (res.success) {
          this.toastService.show('Pembayaran piutang berhasil dicatat.', 'success');
          this.showAddPaymentRow.set(false);
          this.detailData.set(res.data);
          this.paymentForm = {
            paymentDate: new Date().toISOString().substring(0, 10),
            amount: res.data.remainingAmount,
            paymentMethod: 'Cash',
            notes: ''
          };
          this.loadReceivables();
        } else {
          this.toastService.show(res.message || 'Gagal mencatat pembayaran.', 'error');
        }
      },
      error: (err) => {
        this.paymentSaving.set(false);
        this.toastService.show(err?.error?.message || 'Terjadi kesalahan saat mencatat pembayaran.', 'error');
      }
    });
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

  openDeleteConfirm(r: ReceivableListItem): void {
    this.deleteTarget.set(r);
    this.showDeleteDialog.set(true);
  }

  openDeleteConfirmFromDetail(detail: ReceivableDetail): void {
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
    this.receivablesService.deleteReceivable(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleteSaving.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show('Piutang berhasil dihapus.', 'success');
            this.closeDetailDrawer();
            this.loadReceivables();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus piutang.', 'error');
          }
        },
        error: (err) => {
          this.deleteSaving.set(false);
          this.toastService.show(err?.error?.message || 'Kesalahan sistem saat menghapus piutang.', 'error');
        }
      });
  }
}
