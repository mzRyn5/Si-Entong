import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { AuthService } from '../../../../core/auth/auth.service';
import { SaleDetail, SaleListItem } from '../../../../core/models/transaction.model';
import { SalesReturnDetail, SalesReturnListItem } from '../../models/sales-api.model';
import { ReturnItemForm } from '../../models/sales-view.model';
import { SalesService } from '../../sales.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { ConfirmationDialogComponent } from '../../../../shared/components/confirmation-dialog/confirmation-dialog.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { ToastService } from '../../../../shared/services/toast.service';

@Component({
  selector: 'app-sales-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    BadgeComponent,
    ConfirmationDialogComponent,
    FilterBarComponent,
    LoadingStateComponent,
    PaginationComponent,
    CurrencyIdrPipe,
    DateIdPipe,
    TranslatePipe
  ],
  templateUrl: './sales-list.component.html',
  styleUrls: ['./sales-list.component.scss']
})
export class SalesListComponent implements OnInit, OnDestroy {
  readonly activeTab = signal<'sales' | 'returns'>('sales');

  // Sales State
  readonly sales = signal<SaleListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  searchQuery = '';
  filterPaymentMethod = '';
  fromDate = '';
  toDate = '';

  currentPage = 1;
  pageSize = 20;
  totalItems = signal(0);
  totalPages = signal(1);

  // Returns State
  readonly returns = signal<SalesReturnListItem[]>([]);
  readonly returnLoading = signal(false);
  readonly returnError = signal<string | null>(null);

  returnFromDate = '';
  returnToDate = '';

  returnPage = 1;
  returnTotalItems = signal(0);
  returnTotalPages = signal(1);

  // Detail & Drawer (Sales)
  showDetailDrawer = signal(false);
  detailLoading = signal(false);
  detailData = signal<SaleDetail | null>(null);
  drawerTitleId = `drawer-title-${Math.random().toString(36).slice(2)}`;

  // Detail Drawer (Returns)
  showReturnDetailDrawer = signal(false);
  selectedReturn = signal<SalesReturnDetail | null>(null);

  // Return Form Drawer
  showReturnFormDrawer = signal(false);
  returnSaleId = '';
  returnSaleNumber = '';
  returnItems: ReturnItemForm[] = [];
  returnFormModel = {
    returnDate: new Date().toISOString().substring(0, 10),
    reason: '',
    notes: ''
  };

  // Void Dialog
  showVoidDialog = signal(false);
  voidTarget = signal<SaleListItem | SaleDetail | null>(null);
  voidReason = '';
  voidSaving = signal(false);

  // Delete Dialog (Owner only)
  showDeleteDialog = signal(false);
  deleteTarget = signal<SaleListItem | SaleDetail | null>(null);
  deleteSaving = signal(false);
  isOwner = signal(false);

  readonly saving = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();
  private readonly returnFilterChange$ = new Subject<void>();

  constructor(
    private readonly salesService: SalesService,
    private readonly toastService: ToastService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isOwner.set(this.authService.hasAnyRole(['owner']));
    // Sales filter debounce
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadSales();
      });

    // Returns filter debounce
    this.returnFilterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.returnPage = 1;
        this.loadReturns();
      });

    this.loadSales();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  switchTab(tab: 'sales' | 'returns'): void {
    this.activeTab.set(tab);
    if (tab === 'sales') {
      this.currentPage = 1;
      this.loadSales();
    } else {
      this.returnPage = 1;
      this.loadReturns();
    }
  }

  // Sales Operations
  loadSales(): void {
    this.loading.set(true);
    this.error.set(null);

    this.salesService.getAll({
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchQuery.trim() || undefined,
      paymentMethod: this.filterPaymentMethod || undefined,
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.sales.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat riwayat penjualan.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat riwayat penjualan.');
      }
    });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.filterPaymentMethod = '';
    this.fromDate = '';
    this.toDate = '';
    this.currentPage = 1;
    this.loadSales();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadSales();
  }

  viewDetail(id: string): void {
    this.detailLoading.set(true);
    this.showDetailDrawer.set(true);
    this.detailData.set(null);

    this.salesService.getById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.detailLoading.set(false);
          if (res.success) {
            this.detailData.set(res.data);
          } else {
            this.showToast(res.message || 'Gagal memuat rincian penjualan.', 'error');
            this.closeDetailDrawer();
          }
        },
        error: (err) => {
          this.detailLoading.set(false);
          this.showToast(err?.error?.message || 'Gagal memuat rincian penjualan.', 'error');
          this.closeDetailDrawer();
        }
      });
  }

  closeDetailDrawer(): void {
    this.showDetailDrawer.set(false);
    this.detailData.set(null);
  }

  // Returns Operations
  loadReturns(): void {
    this.returnLoading.set(true);
    this.returnError.set(null);

    this.salesService.getSalesReturns({
      page: this.returnPage,
      pageSize: this.pageSize,
      fromDate: this.returnFromDate || undefined,
      toDate: this.returnToDate || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.returnLoading.set(false);
        if (res.success) {
          this.returns.set(res.data);
          this.returnTotalItems.set(res.meta.totalItems);
          this.returnTotalPages.set(res.meta.totalPages);
        } else {
          this.returnError.set(res.message || 'Gagal memuat retur penjualan.');
        }
      },
      error: (err) => {
        this.returnLoading.set(false);
        this.returnError.set(err?.error?.message || 'Kesalahan memuat retur penjualan.');
      }
    });
  }

  onReturnFilterChange(): void {
    this.returnFilterChange$.next();
  }

  clearReturnFilters(): void {
    this.returnFromDate = '';
    this.returnToDate = '';
    this.returnPage = 1;
    this.loadReturns();
  }

  onReturnPageChange(page: number): void {
    this.returnPage = page;
    this.loadReturns();
  }

  viewReturnDetail(id: string): void {
    this.salesService.getSalesReturnById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.selectedReturn.set(res.data);
            this.showReturnDetailDrawer.set(true);
          } else {
            this.showToast(res.message || 'Gagal memuat rincian retur.', 'error');
          }
        },
        error: (err) => {
          this.showToast(err?.error?.message || 'Gagal memuat rincian retur.', 'error');
        }
      });
  }

  closeReturnDetailDrawer(): void {
    this.showReturnDetailDrawer.set(false);
    this.selectedReturn.set(null);
  }

  // Return Creation Operations
  openReturnForm(detail: SaleDetail): void {
    this.returnSaleId = detail.id;
    this.returnSaleNumber = detail.saleNumber;
    this.returnFormModel = {
      returnDate: new Date().toISOString().substring(0, 10),
      reason: '',
      notes: ''
    };

    // Prepare return items
    this.returnItems = detail.items.map(item => ({
      saleItemId: item.id || '', // we added id to SaleItem
      productId: item.productId,
      productName: item.productName,
      soldQty: item.quantity,
      unitPrice: item.unitPrice,
      returnQty: 1,
      refundAmount: item.unitPrice,
      selected: false
    }));

    this.closeDetailDrawer(); // close detail drawer
    this.showReturnFormDrawer.set(true);
  }

  closeReturnForm(): void {
    this.showReturnFormDrawer.set(false);
    this.returnItems = [];
  }

  calculateTotalRefund(): number {
    return this.returnItems
      .filter(item => item.selected)
      .reduce((sum, item) => sum + (item.returnQty * item.refundAmount), 0);
  }

  isAnyItemReturnSelected(): boolean {
    return this.returnItems.some(item => item.selected);
  }

  onSubmitReturn(form: NgForm): void {
    if (form.invalid || !this.isAnyItemReturnSelected()) return;

    this.saving.set(true);
    const selectedItems = this.returnItems
      .filter(item => item.selected)
      .map(item => ({
        productId: item.productId,
        quantity: item.returnQty
      }));

    this.salesService.createSalesReturn({
      saleId: this.returnSaleId,
      reason: this.returnFormModel.reason,
      items: selectedItems
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.saving.set(false);
        if (res.success) {
          this.showReturnFormDrawer.set(false);
          this.showToast('Retur penjualan berhasil disimpan.', 'success');
          this.switchTab('returns'); // Switch to returns history tab
        } else {
          this.showToast(res.message || 'Gagal menyimpan retur.', 'error');
        }
      },
      error: (err) => {
        this.saving.set(false);
        this.showToast(err?.error?.message || 'Kesalahan saat menyimpan retur.', 'error');
      }
    });
  }

  // Void Actions
  openVoidConfirm(s: SaleListItem): void {
    this.voidTarget.set(s);
    this.voidReason = '';
    this.showVoidDialog.set(true);
  }

  openVoidConfirmFromDetail(detail: SaleDetail): void {
    this.voidTarget.set(detail);
    this.voidReason = '';
    this.showVoidDialog.set(true);
  }

  closeVoidDialog(): void {
    if (this.voidSaving()) return;
    this.showVoidDialog.set(false);
    this.voidTarget.set(null);
  }

  executeVoid(): void {
    const target = this.voidTarget();
    if (!target || !this.voidReason.trim() || this.voidSaving()) return;

    this.voidSaving.set(true);
    this.salesService.void(target.id, this.voidReason.trim())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.voidSaving.set(false);
          this.showVoidDialog.set(false);
          if (res.success) {
            this.showToast('Transaksi penjualan berhasil dibatalkan.', 'success');
            this.closeDetailDrawer();
            this.loadSales();
          } else {
            this.showToast(res.message || 'Gagal membatalkan transaksi.', 'error');
          }
        },
        error: (err) => {
          this.voidSaving.set(false);
          this.showToast(err?.error?.message || 'Kesalahan sistem saat memproses pembatalan.', 'error');
        }
      });
  }

  openDeleteConfirm(s: SaleListItem): void {
    this.deleteTarget.set(s);
    this.showDeleteDialog.set(true);
  }

  openDeleteConfirmFromDetail(detail: SaleDetail): void {
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
    this.salesService.deleteSale(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleteSaving.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.showToast('Transaksi penjualan berhasil dihapus permanen.', 'success');
            this.closeDetailDrawer();
            this.loadSales();
          } else {
            this.showToast(res.message || 'Gagal menghapus transaksi.', 'error');
          }
        },
        error: (err) => {
          this.deleteSaving.set(false);
          this.showToast(err?.error?.message || 'Kesalahan sistem saat menghapus transaksi.', 'error');
        }
      });
  }

  private showToast(message: string, type: 'success' | 'error'): void {
    this.toastService.show(message, type);
  }
}
