import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { InventoryService, StockSummaryItem, StockMovementItem, StockOpnameListItem, StockOpnameDetail, StockOpnameItemRequest } from '../../inventory.service';
import { CategoriesService, CategoryItem } from '../../../categories/categories.service';
import { ProductsService, ProductListItem } from '../../../products/products.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

export interface OpnameProductForm {
  productId: string;
  name: string;
  sku: string;
  systemStock: number;
  physicalStock: number;
  notes: string;
  selected: boolean;
}

@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BadgeComponent,
    FilterBarComponent,
    LoadingStateComponent,
    PaginationComponent,
    CurrencyIdrPipe,
    DateIdPipe,
    TranslatePipe
  ],
  templateUrl: './inventory-list.component.html',
  styleUrls: ['./inventory-list.component.scss']
})
export class InventoryListComponent implements OnInit, OnDestroy {
  readonly activeTab = signal<'summary' | 'movements' | 'opname'>('summary');

  // Products and Categories
  readonly categories = signal<CategoryItem[]>([]);
  readonly allProducts = signal<ProductListItem[]>([]);

  // 1. Stock Summary Tab
  readonly summaryItems = signal<StockSummaryItem[]>([]);
  readonly loadingSummary = signal(false);
  summarySearch = '';
  summaryCategory = '';
  summaryLowStockOnly = false;
  summaryPage = 1;
  summaryTotalItems = signal(0);
  summaryTotalPages = signal(1);

  // 2. Stock Movements Tab
  readonly movementItems = signal<StockMovementItem[]>([]);
  readonly loadingMovements = signal(false);
  movementType = '';
  movementFromDate = '';
  movementToDate = '';
  movementPage = 1;
  movementTotalItems = signal(0);
  movementTotalPages = signal(1);

  // 3. Stock Opname Tab
  readonly opnameList = signal<StockOpnameListItem[]>([]);
  readonly loadingOpnames = signal(false);
  opnamePage = 1;
  opnameTotalItems = signal(0);
  opnameTotalPages = signal(1);

  // Opname Detail Drawer
  showDetailDrawer = signal(false);
  detailLoading = signal(false);
  detailData = signal<StockOpnameDetail | null>(null);

  // Opname Form Drawer (Create/Edit Draft)
  showFormDrawer = signal(false);
  isEditMode = signal(false);
  selectedOpnameId = '';
  opnameDate = new Date().toISOString().substring(0, 10);
  opnameNotes = '';
  opnameProducts: OpnameProductForm[] = [];
  saving = signal(false);
  opnameSearchQuery = '';
  showSelectedOnly = false;

  // Void Opname Dialog
  showVoidDialog = signal(false);
  voidTarget = signal<StockOpnameListItem | StockOpnameDetail | null>(null);
  voidReason = '';
  voidSaving = signal(false);

  readonly pageSize = 10;
  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly inventoryService: InventoryService,
    private readonly categoriesService: CategoriesService,
    private readonly productsService: ProductsService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        if (this.activeTab() === 'summary') {
          this.summaryPage = 1;
          this.loadSummary();
        } else if (this.activeTab() === 'movements') {
          this.movementPage = 1;
          this.loadMovements();
        }
      });

    this.loadDropdownData();
    this.loadSummary();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadDropdownData(): void {
    this.categoriesService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.categories.set(res.data);
          }
        }
      });

    this.productsService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.allProducts.set(res.data);
          }
        }
      });
  }

  switchTab(tab: 'summary' | 'movements' | 'opname'): void {
    this.activeTab.set(tab);
    if (tab === 'summary') {
      this.summaryPage = 1;
      this.loadSummary();
    } else if (tab === 'movements') {
      this.movementPage = 1;
      this.loadMovements();
    } else {
      this.opnamePage = 1;
      this.loadOpnames();
    }
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  // Stock Summary Tab Operations
  loadSummary(): void {
    this.loadingSummary.set(true);
    this.inventoryService.getStockSummary({
      page: this.summaryPage,
      pageSize: this.pageSize,
      search: this.summarySearch.trim() || undefined,
      categoryId: this.summaryCategory || undefined,
      lowStockOnly: this.summaryLowStockOnly || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loadingSummary.set(false);
        if (res.success) {
          const mapped = (res.data || []).map(item => ({
            ...item,
            name: item.name || item.productName || ''
          }));
          this.summaryItems.set(mapped);
          this.summaryTotalItems.set(res.meta.totalItems);
          this.summaryTotalPages.set(res.meta.totalPages);
        }
      },
      error: () => {
        this.loadingSummary.set(false);
      }
    });
  }

  clearSummaryFilters(): void {
    this.summarySearch = '';
    this.summaryCategory = '';
    this.summaryLowStockOnly = false;
    this.summaryPage = 1;
    this.loadSummary();
  }

  onSummaryPageChange(page: number): void {
    this.summaryPage = page;
    this.loadSummary();
  }

  // Stock Movements Tab Operations
  loadMovements(): void {
    this.loadingMovements.set(true);
    this.inventoryService.getStockMovements({
      page: this.movementPage,
      pageSize: this.pageSize,
      movementType: this.movementType || undefined,
      fromDate: this.movementFromDate || undefined,
      toDate: this.movementToDate || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loadingMovements.set(false);
        if (res.success) {
          this.movementItems.set(res.data);
          this.movementTotalItems.set(res.meta.totalItems);
          this.movementTotalPages.set(res.meta.totalPages);
        }
      },
      error: () => {
        this.loadingMovements.set(false);
      }
    });
  }

  clearMovementFilters(): void {
    this.movementType = '';
    this.movementFromDate = '';
    this.movementToDate = '';
    this.movementPage = 1;
    this.loadMovements();
  }

  onMovementPageChange(page: number): void {
    this.movementPage = page;
    this.loadMovements();
  }

  // Stock Opname Tab Operations
  loadOpnames(): void {
    this.loadingOpnames.set(true);
    this.inventoryService.getStockOpnames({
      page: this.opnamePage,
      pageSize: this.pageSize
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loadingOpnames.set(false);
        if (res.success) {
          this.opnameList.set(res.data);
          this.opnameTotalItems.set(res.meta.totalItems);
          this.opnameTotalPages.set(res.meta.totalPages);
        }
      },
      error: () => {
        this.loadingOpnames.set(false);
      }
    });
  }

  onOpnamePageChange(page: number): void {
    this.opnamePage = page;
    this.loadOpnames();
  }

  viewOpnameDetail(id: string): void {
    this.detailLoading.set(true);
    this.showDetailDrawer.set(true);
    this.detailData.set(null);

    this.inventoryService.getStockOpnameById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.detailLoading.set(false);
          if (res.success) {
            this.detailData.set(res.data);
          } else {
            this.toastService.show(res.message || 'Gagal memuat rincian opname.', 'error');
            this.closeDetailDrawer();
          }
        },
        error: (err) => {
          this.detailLoading.set(false);
          this.toastService.show(err?.error?.message || 'Gagal memuat rincian opname.', 'error');
          this.closeDetailDrawer();
        }
      });
  }

  closeDetailDrawer(): void {
    this.showDetailDrawer.set(false);
    this.detailData.set(null);
  }

  // Form Drawer (Create/Edit Draft)
  openCreateOpname(): void {
    this.isEditMode.set(false);
    this.selectedOpnameId = '';
    this.opnameDate = new Date().toISOString().substring(0, 10);
    this.opnameNotes = '';
    this.opnameSearchQuery = '';
    this.showSelectedOnly = false;

    // Load products list for choice
    this.opnameProducts = this.allProducts().map(p => ({
      productId: p.id,
      name: p.name,
      sku: p.sku,
      systemStock: p.currentStock,
      physicalStock: p.currentStock,
      notes: '',
      selected: false
    }));

    this.showFormDrawer.set(true);
  }

  openEditOpname(d: StockOpnameDetail): void {
    this.isEditMode.set(true);
    this.selectedOpnameId = d.id;
    this.opnameDate = new Date(d.opnameDate).toISOString().substring(0, 10);
    this.opnameNotes = d.notes || '';
    this.opnameSearchQuery = '';
    this.showSelectedOnly = false;

    // Merge existing items with products selection
    const itemsMap = new Map(d.items.map(item => [item.productId, item]));

    this.opnameProducts = this.allProducts().map(p => {
      const existing = itemsMap.get(p.id);
      return {
        productId: p.id,
        name: p.name,
        sku: p.sku,
        systemStock: existing ? existing.systemStock : p.currentStock,
        physicalStock: existing ? existing.physicalStock : p.currentStock,
        notes: existing ? existing.notes || '' : '',
        selected: !!existing
      };
    });

    this.closeDetailDrawer();
    this.showFormDrawer.set(true);
  }

  closeFormDrawer(): void {
    this.showFormDrawer.set(false);
  }

  filteredOpnameProducts(): OpnameProductForm[] {
    const query = (this.opnameSearchQuery || '').trim().toLowerCase();
    return this.opnameProducts.filter(p => {
      const matchesSearch = !query || 
        (p.name || '').toLowerCase().includes(query) ||
        (p.sku || '').toLowerCase().includes(query);
      
      const matchesSelected = !this.showSelectedOnly || p.selected;
      
      return matchesSearch && matchesSelected;
    });
  }

  getSelectedOpnameCount(): number {
    return this.opnameProducts.filter(p => p.selected).length;
  }

  toggleOpnameShowSelectedOnly(): void {
    this.showSelectedOnly = !this.showSelectedOnly;
  }

  isAnyProductSelected(): boolean {
    return this.opnameProducts.some(p => p.selected);
  }

  onSubmitOpname(form: NgForm): void {
    if (form.invalid || !this.isAnyProductSelected() || this.saving()) return;

    this.saving.set(true);
    const selectedItems: StockOpnameItemRequest[] = this.opnameProducts
      .filter(p => p.selected)
      .map(p => ({
        productId: p.productId,
        systemStock: p.systemStock,
        physicalStock: p.physicalStock,
        notes: p.notes.trim() || undefined
      }));

    if (this.isEditMode()) {
      this.inventoryService.updateStockOpname(this.selectedOpnameId, {
        opnameDate: new Date(this.opnameDate).toISOString(),
        notes: this.opnameNotes.trim() || undefined,
        items: selectedItems
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          if (res.success) {
            this.showFormDrawer.set(false);
            this.toastService.show('Draft Stock Opname berhasil diperbarui.', 'success');
            this.loadOpnames();
          } else {
            this.toastService.show(res.message || 'Gagal memperbarui draft opname.', 'error');
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.show(err?.error?.message || 'Gagal memperbarui draft opname.', 'error');
        }
      });
    } else {
      this.inventoryService.createStockOpname({
        opnameDate: new Date(this.opnameDate).toISOString(),
        notes: this.opnameNotes.trim() || undefined,
        items: selectedItems
      })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          if (res.success) {
            this.showFormDrawer.set(false);
            this.toastService.show('Draft Stock Opname berhasil disimpan.', 'success');
            this.loadOpnames();
          } else {
            this.toastService.show(res.message || 'Gagal menyimpan draft opname.', 'error');
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menyimpan draft opname.', 'error');
        }
      });
    }
  }

  // Actions: Post & Cancel
  postOpname(id: string): void {
    if (this.saving()) return;
    this.saving.set(true);

    this.inventoryService.postStockOpname(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          if (res.success) {
            this.toastService.show('Stock Opname berhasil diposting! Stok produk telah disesuaikan.', 'success');
            this.closeDetailDrawer();
            this.loadOpnames();
            this.loadDropdownData(); // reload current stocks
          } else {
            this.toastService.show(res.message || 'Gagal memposting stock opname.', 'error');
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.show(err?.error?.message || 'Gagal memposting stock opname.', 'error');
        }
      });
  }

  openVoidConfirm(o: StockOpnameListItem | StockOpnameDetail): void {
    this.voidTarget.set(o);
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
    this.inventoryService.cancelStockOpname(target.id, this.voidReason.trim())
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.voidSaving.set(false);
          this.showVoidDialog.set(false);
          if (res.success) {
            this.toastService.show('Stock Opname berhasil dibatalkan (void). Penyesuaian stok dikembalikan.', 'success');
            this.closeDetailDrawer();
            this.loadOpnames();
            this.loadDropdownData();
          } else {
            this.toastService.show(res.message || 'Gagal membatalkan stock opname.', 'error');
          }
        },
        error: (err) => {
          this.voidSaving.set(false);
          this.toastService.show(err?.error?.message || 'Gagal membatalkan stock opname.', 'error');
        }
      });
  }
}
