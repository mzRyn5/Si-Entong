import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { ProductsService, ProductListItem, ProductDetail, ProductCreateRequest, ProductUpdateRequest } from '../../products.service';
import { CategoriesService, CategoryItem } from '../../../categories/categories.service';
import { UnitsService, UnitItem } from '../../../units/units.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { NgIf, NgFor, NgClass } from '@angular/common';

@Component({
  selector: 'app-products-list',
  standalone: true,
  imports: [
    FormsModule,
    BadgeComponent,
    FilterBarComponent,
    LoadingStateComponent,
    PaginationComponent,
    CurrencyIdrPipe,
    TranslatePipe,
    NgIf,
    NgFor,
    NgClass
  ],
  templateUrl: './products-list.component.html',
  styleUrls: ['./products-list.component.scss']
})
export class ProductsListComponent implements OnInit, OnDestroy {
  readonly products = signal<ProductListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  searchQuery = '';
  filterCategory = '';
  filterActive = '';

  currentPage = 1;
  pageSize = 10;
  totalItems = signal(0);
  totalPages = signal(1);

  // Categories & Units for dropdown
  readonly categories = signal<CategoryItem[]>([]);
  readonly units = signal<UnitItem[]>([]);

  // Form Drawer
  showFormDrawer = signal(false);
  isEditMode = signal(false);
  selectedProductId = '';
  formModel = {
    sku: '',
    barcode: '',
    name: '',
    categoryId: '',
    unitId: '',
    purchasePrice: 0,
    sellingPrice: 0,
    currentStock: 0,
    lowStockThreshold: 5,
    isActive: true
  };
  saving = signal(false);

  // Delete Dialog
  showDeleteDialog = signal(false);
  deleteTarget = signal<ProductListItem | null>(null);
  deleting = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly productsService: ProductsService,
    private readonly categoriesService: CategoriesService,
    private readonly unitsService: UnitsService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadProducts();
      });

    this.loadDropdownData();
    this.loadProducts();
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

    this.unitsService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.units.set(res.data);
          }
        }
      });
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);

    this.productsService.getAll({
      page: this.currentPage,
      pageSize: this.pageSize,
      search: this.searchQuery.trim() || undefined,
      categoryId: this.filterCategory || undefined,
      isActive: this.filterActive === 'true' ? true : this.filterActive === 'false' ? false : undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.products.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat produk.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat produk.');
      }
    });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.filterCategory = '';
    this.filterActive = '';
    this.currentPage = 1;
    this.loadProducts();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadProducts();
  }

  openCreateForm(): void {
    this.isEditMode.set(false);
    this.selectedProductId = '';
    this.formModel = {
      sku: '',
      barcode: '',
      name: '',
      categoryId: '',
      unitId: this.units().length > 0 ? this.units()[0].id : '',
      purchasePrice: 0,
      sellingPrice: 0,
      currentStock: 0,
      lowStockThreshold: 5,
      isActive: true
    };
    this.showFormDrawer.set(true);
  }

  openEditForm(p: ProductListItem): void {
    this.isEditMode.set(true);
    this.selectedProductId = p.id;
    this.formModel = {
      sku: p.sku,
      barcode: '',
      name: p.name,
      categoryId: '',
      unitId: '',
      purchasePrice: 0,
      sellingPrice: p.sellingPrice,
      currentStock: p.currentStock,
      lowStockThreshold: 5,
      isActive: p.isActive !== false
    };

    // Load full details for edit
    this.productsService.getById(p.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            const d = res.data;
            this.formModel = {
              sku: d.sku,
              barcode: d.barcode || '',
              name: d.name,
              categoryId: d.categoryId || '',
              unitId: d.unitId,
              purchasePrice: d.purchasePrice,
              sellingPrice: d.sellingPrice,
              currentStock: d.currentStock,
              lowStockThreshold: d.lowStockThreshold,
              isActive: d.isActive !== false
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
      const request: ProductUpdateRequest = {
        sku: this.formModel.sku.trim() || undefined,
        barcode: this.formModel.barcode.trim() || undefined,
        name: this.formModel.name.trim(),
        categoryId: this.formModel.categoryId || undefined,
        unitId: this.formModel.unitId || undefined,
        purchasePrice: this.formModel.purchasePrice,
        sellingPrice: this.formModel.sellingPrice,
        lowStockThreshold: this.formModel.lowStockThreshold,
        isActive: this.formModel.isActive
      };
      this.productsService.update(this.selectedProductId, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Produk berhasil diperbarui.', 'success');
              this.loadProducts();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan produk.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan produk.', 'error');
          }
        });
    } else {
      const request: ProductCreateRequest = {
        sku: this.formModel.sku.trim(),
        barcode: this.formModel.barcode.trim() || undefined,
        name: this.formModel.name.trim(),
        categoryId: this.formModel.categoryId || undefined,
        unitId: this.formModel.unitId,
        purchasePrice: this.formModel.purchasePrice,
        sellingPrice: this.formModel.sellingPrice,
        currentStock: this.formModel.currentStock,
        lowStockThreshold: this.formModel.lowStockThreshold,
        isActive: this.formModel.isActive
      };
      this.productsService.create(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Produk berhasil ditambahkan.', 'success');
              this.loadProducts();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan produk.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan produk.', 'error');
          }
        });
    }
  }

  openDeleteConfirm(p: ProductListItem): void {
    this.deleteTarget.set(p);
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
    this.productsService.delete(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleting.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show('Produk berhasil dihapus.', 'success');
            this.loadProducts();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus produk.', 'error');
          }
        },
        error: (err) => {
          this.deleting.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menghapus produk.', 'error');
        }
      });
  }
}
