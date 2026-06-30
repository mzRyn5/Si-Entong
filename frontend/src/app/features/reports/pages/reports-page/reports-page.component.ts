import { ChangeDetectionStrategy, Component, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';

import { ReportsService } from '../../reports.service';
import { CategoriesService, CategoryItem } from '../../../categories/categories.service';
import { ToastService } from '../../../../shared/services/toast.service';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { DailySalesItem, ProductSalesItem, StockValuationItem, BasicProfitData } from '../../../../core/models/report.model';
import { LanguageService } from '../../../../core/services/language.service';
import { SettingsService } from '../../../settings/settings.service';
import { AuthService } from '../../../../core/auth/auth.service';

@Component({
  selector: 'app-reports-page',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LoadingStateComponent,
    PaginationComponent,
    TranslatePipe,
    CurrencyIdrPipe
  ],
  templateUrl: './reports-page.component.html',
  styleUrl: './reports-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsPageComponent implements OnInit, OnDestroy {
  readonly activeTab = signal<'daily' | 'product' | 'valuation' | 'profit'>('daily');
  readonly loading = signal(false);
  readonly exporting = signal(false);
  readonly error = signal<string | null>(null);

  readonly categories = signal<CategoryItem[]>([]);
  todayDate = new Date();

  // Tab: Daily Sales
  readonly dailySales = signal<DailySalesItem[]>([]);
  dailyCurrentPage = 1;
  dailyPageSize = 10;
  readonly dailyTotalItems = signal(0);
  readonly dailyTotalPages = signal(1);
  dailyFromDate = '';
  dailyToDate = '';

  // Tab: Product Sales
  readonly productSales = signal<ProductSalesItem[]>([]);
  productCurrentPage = 1;
  productPageSize = 10;
  readonly productTotalItems = signal(0);
  readonly productTotalPages = signal(1);
  productFromDate = '';
  productToDate = '';
  productSearch = '';
  productCategoryId = '';

  // Tab: Stock Valuation
  readonly stockValuation = signal<StockValuationItem[]>([]);
  stockCurrentPage = 1;
  stockPageSize = 10;
  readonly stockTotalItems = signal(0);
  readonly stockTotalPages = signal(1);
  stockSearch = '';
  stockCategoryId = '';

  // Tab: Basic Profit
  readonly profitData = signal<BasicProfitData | null>(null);
  profitFromDate = '';
  profitToDate = '';

  // Store details for print header & footer
  storeName = signal('');
  storeAddress = signal('');
  storePhone = signal('');
  ownerName = signal('');

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly reportsService: ReportsService,
    private readonly categoriesService: CategoriesService,
    private readonly toastService: ToastService,
    private readonly languageService: LanguageService,
    private readonly settingsService: SettingsService,
    private readonly authService: AuthService
  ) {
    const today = new Date();
    const startOfMonth = new Date(today.getFullYear(), today.getMonth(), 2); // Local timezone safe start of month
    
    const startStr = startOfMonth.toISOString().substring(0, 10);
    const endStr = today.toISOString().substring(0, 10);

    this.dailyFromDate = startStr;
    this.dailyToDate = endStr;
    this.productFromDate = startStr;
    this.productToDate = endStr;
    this.profitFromDate = startStr;
    this.profitToDate = endStr;
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadActiveTabData();
    this.settingsService.getProfile()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.storeName.set(res.data.name || '');
            this.storeAddress.set(res.data.address || '');
            this.storePhone.set(res.data.phone || '');
          }
        }
      });
    const user = this.authService.getCurrentUser();
    this.ownerName.set(user?.name || user?.username || 'Owner');
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  setActiveTab(tab: 'daily' | 'product' | 'valuation' | 'profit'): void {
    this.activeTab.set(tab);
    this.loadActiveTabData();
  }

  loadCategories(): void {
    this.categoriesService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.categories.set(res.data);
          }
        }
      });
  }

  loadActiveTabData(): void {
    this.loading.set(true);
    this.error.set(null);

    const tab = this.activeTab();
    if (tab === 'daily') {
      this.reportsService.getDailySales({
        page: this.dailyCurrentPage,
        pageSize: this.dailyPageSize,
        fromDate: this.dailyFromDate || undefined,
        toDate: this.dailyToDate || undefined
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res.success) {
            this.dailySales.set(res.data);
            this.dailyTotalItems.set(res.meta.totalItems);
            this.dailyTotalPages.set(res.meta.totalPages);
          } else {
            this.error.set(res.message || this.languageService.translate('Gagal memuat laporan penjualan harian.'));
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.message || this.languageService.translate('Kesalahan memuat data.'));
        }
      });
    } else if (tab === 'product') {
      this.reportsService.getProductSales({
        page: this.productCurrentPage,
        pageSize: this.productPageSize,
        fromDate: this.productFromDate || undefined,
        toDate: this.productToDate || undefined,
        search: this.productSearch.trim() || undefined,
        categoryId: this.productCategoryId || undefined
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res.success) {
            this.productSales.set(res.data);
            this.productTotalItems.set(res.meta.totalItems);
            this.productTotalPages.set(res.meta.totalPages);
          } else {
            this.error.set(res.message || this.languageService.translate('Gagal memuat laporan penjualan produk.'));
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.message || this.languageService.translate('Kesalahan memuat data.'));
        }
      });
    } else if (tab === 'valuation') {
      this.reportsService.getStockValuation({
        page: this.stockCurrentPage,
        pageSize: this.stockPageSize,
        search: this.stockSearch.trim() || undefined,
        categoryId: this.stockCategoryId || undefined
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res.success) {
            this.stockValuation.set(res.data);
            this.stockTotalItems.set(res.meta.totalItems);
            this.stockTotalPages.set(res.meta.totalPages);
          } else {
            this.error.set(res.message || this.languageService.translate('Gagal memuat laporan valuasi stok.'));
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.message || this.languageService.translate('Kesalahan memuat data.'));
        }
      });
    } else if (tab === 'profit') {
      this.reportsService.getBasicProfit({
        fromDate: this.profitFromDate || undefined,
        toDate: this.profitToDate || undefined
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.loading.set(false);
          if (res.success) {
            this.profitData.set(res.data);
          } else {
            this.error.set(res.message || this.languageService.translate('Gagal memuat laporan laba rugi.'));
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.message || this.languageService.translate('Kesalahan memuat data.'));
        }
      });
    }
  }

  onDailyPageChange(page: number): void {
    this.dailyCurrentPage = page;
    this.loadActiveTabData();
  }

  onProductPageChange(page: number): void {
    this.productCurrentPage = page;
    this.loadActiveTabData();
  }

  onStockPageChange(page: number): void {
    this.stockCurrentPage = page;
    this.loadActiveTabData();
  }

  clearProductFilters(): void {
    this.productSearch = '';
    this.productCategoryId = '';
    this.productCurrentPage = 1;
    this.loadActiveTabData();
  }

  exportExcel(): void {
    if (this.exporting()) return;
    this.exporting.set(true);
    const tab = this.activeTab();
    let params: Record<string, any> = {};

    if (tab === 'daily') {
      params = {
        fromDate: this.dailyFromDate || undefined,
        toDate: this.dailyToDate || undefined
      };
    } else if (tab === 'product') {
      params = {
        fromDate: this.productFromDate || undefined,
        toDate: this.productToDate || undefined,
        search: this.productSearch.trim() || undefined,
        categoryId: this.productCategoryId || undefined
      };
    } else if (tab === 'valuation') {
      params = {
        search: this.stockSearch.trim() || undefined,
        categoryId: this.stockCategoryId || undefined
      };
    } else if (tab === 'profit') {
      params = {
        fromDate: this.profitFromDate || undefined,
        toDate: this.profitToDate || undefined
      };
    }

    this.reportsService.downloadReport(this.getReportType(tab), params)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (blob) => {
          this.exporting.set(false);
          const url = window.URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `${tab}_report_${new Date().toISOString().substring(0, 10)}.xlsx`;
          document.body.appendChild(a);
          a.click();
          document.body.removeChild(a);
          window.URL.revokeObjectURL(url);
          this.toastService.show(this.languageService.translate('Laporan berhasil diekspor.'), 'success');
        },
        error: (err) => {
          this.exporting.set(false);
          this.toastService.show(err?.error?.message || this.languageService.translate('Gagal mengekspor laporan.'), 'error');
        }
      });
  }

  private getReportType(tab: string): string {
    switch (tab) {
      case 'daily': return 'daily-sales';
      case 'product': return 'product-sales';
      case 'valuation': return 'stock-valuation';
      case 'profit': return 'basic-profit';
      default: return 'daily-sales';
    }
  }

  exportPdf(): void {
    this.todayDate = new Date();
    const reportRoot = document.querySelector('.reports-print-root');
    if (!reportRoot) {
      this.toastService.show(this.languageService.translate('Gagal mengekspor laporan.'), 'error');
      return;
    }

    const printFrame = document.createElement('iframe');
    printFrame.className = 'reports-print-frame';
    printFrame.setAttribute('aria-hidden', 'true');
    printFrame.style.position = 'fixed';
    printFrame.style.right = '0';
    printFrame.style.bottom = '0';
    printFrame.style.width = '0';
    printFrame.style.height = '0';
    printFrame.style.border = '0';

    const cleanup = () => {
      printFrame.remove();
      window.removeEventListener('focus', cleanup);
    };

    document.body.appendChild(printFrame);
    const printWindow = printFrame.contentWindow;
    const printDocument = printFrame.contentDocument || printWindow?.document;
    if (!printDocument || !printWindow) {
      cleanup();
      this.toastService.show(this.languageService.translate('Gagal mengekspor laporan.'), 'error');
      return;
    }

    printFrame.onload = () => {
      printWindow.focus();
      printWindow.print();
    };

    printDocument.open();
    printDocument.write(`
      <!doctype html>
      <html lang="id">
        <head>
          <base href="${document.baseURI}">
          ${document.head.innerHTML}
        </head>
        <body class="reports-printing">
          ${(reportRoot.cloneNode(true) as HTMLElement).outerHTML}
        </body>
      </html>
    `);
    printDocument.close();

    window.addEventListener('focus', cleanup, { once: true });
  }

  getReportTitle(): string {
    const tab = this.activeTab();
    switch (tab) {
      case 'daily': return 'Laporan Penjualan Harian';
      case 'product': return 'Laporan Penjualan per Produk';
      case 'valuation': return 'Laporan Valuasi Nilai Stok';
      case 'profit': return 'Laporan Ringkasan Laba Rugi';
      default: return 'Laporan Toko';
    }
  }

  getReportPeriod(): string {
    const tab = this.activeTab();
    if (tab === 'daily') {
      return `${this.dailyFromDate || '-'} s/d ${this.dailyToDate || '-'}`;
    } else if (tab === 'product') {
      return `${this.productFromDate || '-'} s/d ${this.productToDate || '-'}`;
    } else if (tab === 'valuation') {
      const cat = this.categories().find(c => c.id === this.stockCategoryId);
      return cat ? `Kategori: ${cat.name}` : 'Semua Kategori';
    } else if (tab === 'profit') {
      return `${this.profitFromDate || '-'} s/d ${this.profitToDate || '-'}`;
    }
    return '';
  }

  getStoreCity(): string {
    const address = this.storeAddress();
    if (!address) return 'Kota Toko';
    const parts = address.split(',');
    if (parts.length > 1) {
      // Return last part or second to last if last is postal code / country
      const possibleCity = parts[parts.length - 1].trim();
      if (/^\d+$/.test(possibleCity)) { // just postal code
        return parts[parts.length - 2].trim();
      }
      return possibleCity;
    }
    return address.trim();
  }
}
