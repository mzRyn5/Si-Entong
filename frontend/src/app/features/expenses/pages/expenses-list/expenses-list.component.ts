
import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { ExpensesService, ExpenseListItem, ExpenseDetail, ExpenseCategoryItem, ExpenseCreateRequest, ExpenseCategoryCreateRequest, ExpenseCategoryUpdateRequest } from '../../expenses.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-expenses-list',
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
  templateUrl: './expenses-list.component.html',
  styleUrls: ['./expenses-list.component.scss']
})
export class ExpensesListComponent implements OnInit, OnDestroy {
  readonly activeTab = signal<'expenses' | 'categories'>('expenses');

  // Expenses State
  readonly expenses = signal<ExpenseListItem[]>([]);
  readonly loadingExpenses = signal(false);
  readonly expensesError = signal<string | null>(null);

  filterCategory = '';
  fromDate = '';
  toDate = '';

  expensesPage = 1;
  pageSize = 10;
  expensesTotalItems = signal(0);
  expensesTotalPages = signal(1);

  // Categories list for dropdown
  readonly categories = signal<ExpenseCategoryItem[]>([]);
  readonly loadingCategories = signal(false);

  // Expense Detail Drawer
  showDetailDrawer = signal(false);
  detailLoading = signal(false);
  detailData = signal<ExpenseDetail | null>(null);

  // Expense Create Drawer
  showExpenseDrawer = signal(false);
  expenseSaving = signal(false);
  expenseFormModel = {
    expenseDate: new Date().toISOString().substring(0, 10),
    categoryId: '',
    amount: 0,
    paymentMethod: 'Cash',
    notes: ''
  };

  // Void Expense Dialog
  showVoidDialog = signal(false);
  voidTarget = signal<ExpenseListItem | ExpenseDetail | null>(null);
  voidReason = '';
  voidSaving = signal(false);

  // Categories Tab State
  readonly categoriesList = signal<ExpenseCategoryItem[]>([]);
  readonly categoriesError = signal<string | null>(null);
  categoriesPage = 1;
  categoriesTotalItems = signal(0);
  categoriesTotalPages = signal(1);

  // Category Form Drawer (Create/Edit)
  showCategoryDrawer = signal(false);
  isCategoryEditMode = signal(false);
  selectedCategoryId = '';
  categoryFormModel = {
    name: '',
    description: '',
    isActive: true
  };
  categorySaving = signal(false);

  // Delete Category Dialog
  showDeleteCatDialog = signal(false);
  deleteCatTarget = signal<ExpenseCategoryItem | null>(null);
  deleteCatSaving = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly expensesService: ExpensesService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.expensesPage = 1;
        this.loadExpenses();
      });

    this.loadDropdownCategories();
    this.loadExpenses();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  switchTab(tab: 'expenses' | 'categories'): void {
    this.activeTab.set(tab);
    if (tab === 'expenses') {
      this.expensesPage = 1;
      this.loadExpenses();
    } else {
      this.categoriesPage = 1;
      this.loadCategoriesTab();
    }
  }

  loadDropdownCategories(): void {
    this.expensesService.getCategoriesDropdown()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.categories.set(res.data || []);
          }
        }
      });
  }

  // Expenses Operations
  loadExpenses(): void {
    this.loadingExpenses.set(true);
    this.expensesError.set(null);

    this.expensesService.getExpenses({
      page: this.expensesPage,
      pageSize: this.pageSize,
      categoryId: this.filterCategory || undefined,
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loadingExpenses.set(false);
        if (res.success) {
          this.expenses.set(res.data);
          this.expensesTotalItems.set(res.meta.totalItems);
          this.expensesTotalPages.set(res.meta.totalPages);
        } else {
          this.expensesError.set(res.message || 'Gagal memuat pengeluaran.');
        }
      },
      error: (err) => {
        this.loadingExpenses.set(false);
        this.expensesError.set(err?.error?.message || 'Kesalahan memuat pengeluaran.');
      }
    });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.filterCategory = '';
    this.fromDate = '';
    this.toDate = '';
    this.expensesPage = 1;
    this.loadExpenses();
  }

  onExpensesPageChange(page: number): void {
    this.expensesPage = page;
    this.loadExpenses();
  }

  viewExpenseDetail(id: string): void {
    this.detailLoading.set(true);
    this.showDetailDrawer.set(true);
    this.detailData.set(null);

    this.expensesService.getExpenseById(id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.detailLoading.set(false);
          if (res.success) {
            this.detailData.set(res.data);
          } else {
            this.toastService.show(res.message || 'Gagal memuat rincian pengeluaran.', 'error');
            this.closeDetailDrawer();
          }
        },
        error: (err) => {
          this.detailLoading.set(false);
          this.toastService.show(err?.error?.message || 'Gagal memuat rincian pengeluaran.', 'error');
          this.closeDetailDrawer();
        }
      });
  }

  closeDetailDrawer(): void {
    this.showDetailDrawer.set(false);
    this.detailData.set(null);
  }

  // Create Expense Drawer
  openExpenseDrawer(): void {
    this.expenseFormModel = {
      expenseDate: new Date().toISOString().substring(0, 10),
      categoryId: this.categories().length > 0 ? this.categories()[0].id : '',
      amount: 0,
      paymentMethod: 'Cash',
      notes: ''
    };
    this.showExpenseDrawer.set(true);
  }

  closeExpenseDrawer(): void {
    this.showExpenseDrawer.set(false);
  }

  onSubmitExpense(form: NgForm): void {
    if (form.invalid || this.expenseSaving()) return;

    this.expenseSaving.set(true);
    const request: ExpenseCreateRequest = {
      expenseDate: new Date(this.expenseFormModel.expenseDate).toISOString(),
      expenseCategoryId: this.expenseFormModel.categoryId,
      amount: this.expenseFormModel.amount,
      description: this.expenseFormModel.notes.trim(),
      paymentMethod: this.expenseFormModel.paymentMethod
    };

    this.expensesService.createExpense(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.expenseSaving.set(false);
          if (res.success) {
            this.showExpenseDrawer.set(false);
            this.toastService.show('Pengeluaran berhasil dicatat.', 'success');
            this.loadExpenses();
          } else {
            this.toastService.show(res.message || 'Gagal mencatat pengeluaran.', 'error');
          }
        },
        error: (err) => {
          this.expenseSaving.set(false);
          this.toastService.show(err?.error?.message || 'Terjadi kesalahan saat mencatat pengeluaran.', 'error');
        }
      });
  }

  // Void Operations
  openVoidConfirm(e: ExpenseListItem): void {
    this.voidTarget.set(e);
    this.voidReason = '';
    this.showVoidDialog.set(true);
  }

  openVoidConfirmFromDetail(detail: ExpenseDetail): void {
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
    if (!target || this.voidSaving()) return;

    this.voidSaving.set(true);
    this.expensesService.deleteExpense(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.voidSaving.set(false);
          this.showVoidDialog.set(false);
          if (res.success) {
            this.toastService.show('Pengeluaran berhasil dihapus.', 'success');
            this.closeDetailDrawer();
            this.loadExpenses();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus pengeluaran.', 'error');
          }
        },
        error: (err) => {
          this.voidSaving.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menghapus pengeluaran.', 'error');
        }
      });
  }

  // Categories Operations
  loadCategoriesTab(): void {
    this.loadingCategories.set(true);
    this.categoriesError.set(null);

    this.expensesService.getCategories({
      page: this.categoriesPage,
      pageSize: this.pageSize
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loadingCategories.set(false);
        if (res.success) {
          this.categoriesList.set(res.data);
          this.categoriesTotalItems.set(res.meta.totalItems);
          this.categoriesTotalPages.set(res.meta.totalPages);
        } else {
          this.categoriesError.set(res.message || 'Gagal memuat kategori.');
        }
      },
      error: (err) => {
        this.loadingCategories.set(false);
        this.categoriesError.set(err?.error?.message || 'Kesalahan memuat kategori.');
      }
    });
  }

  onCategoriesPageChange(page: number): void {
    this.categoriesPage = page;
    this.loadCategoriesTab();
  }

  openCategoryDrawer(): void {
    this.isCategoryEditMode.set(false);
    this.selectedCategoryId = '';
    this.categoryFormModel = {
      name: '',
      description: '',
      isActive: true
    };
    this.showCategoryDrawer.set(true);
  }

  openEditCategoryDrawer(c: ExpenseCategoryItem): void {
    this.isCategoryEditMode.set(true);
    this.selectedCategoryId = c.id;
    this.categoryFormModel = {
      name: c.name,
      description: c.description || '',
      isActive: c.isActive !== false
    };
    this.showCategoryDrawer.set(true);
  }

  closeCategoryDrawer(): void {
    this.showCategoryDrawer.set(false);
  }

  onSubmitCategory(form: NgForm): void {
    if (form.invalid || this.categorySaving()) return;

    this.categorySaving.set(true);

    if (this.isCategoryEditMode()) {
      const request: ExpenseCategoryUpdateRequest = {
        name: this.categoryFormModel.name.trim(),
        description: this.categoryFormModel.description.trim() || undefined,
        isActive: this.categoryFormModel.isActive
      };
      this.expensesService.updateCategory(this.selectedCategoryId, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.categorySaving.set(false);
            if (res.success) {
              this.showCategoryDrawer.set(false);
              this.toastService.show('Kategori berhasil diperbarui.', 'success');
              this.loadCategoriesTab();
              this.loadDropdownCategories(); // sync dropdown
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan kategori.', 'error');
            }
          },
          error: (err) => {
            this.categorySaving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan kategori.', 'error');
          }
        });
    } else {
      const request: ExpenseCategoryCreateRequest = {
        name: this.categoryFormModel.name.trim(),
        description: this.categoryFormModel.description.trim() || undefined,
        isActive: this.categoryFormModel.isActive
      };
      this.expensesService.createCategory(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.categorySaving.set(false);
            if (res.success) {
              this.showCategoryDrawer.set(false);
              this.toastService.show('Kategori berhasil ditambahkan.', 'success');
              this.loadCategoriesTab();
              this.loadDropdownCategories();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan kategori.', 'error');
            }
          },
          error: (err) => {
            this.categorySaving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan kategori.', 'error');
          }
        });
    }
  }

  openDeleteCatConfirm(c: ExpenseCategoryItem): void {
    this.deleteCatTarget.set(c);
    this.showDeleteCatDialog.set(true);
  }

  closeDeleteCatDialog(): void {
    this.showDeleteCatDialog.set(false);
    this.deleteCatTarget.set(null);
  }

  executeDeleteCat(): void {
    const target = this.deleteCatTarget();
    if (!target || this.deleteCatSaving()) return;

    this.deleteCatSaving.set(true);
    this.expensesService.deleteCategory(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleteCatSaving.set(false);
          this.showDeleteCatDialog.set(false);
          if (res.success) {
            this.toastService.show('Kategori berhasil dihapus.', 'success');
            this.loadCategoriesTab();
            this.loadDropdownCategories();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus kategori.', 'error');
          }
        },
        error: (err) => {
          this.deleteCatSaving.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menghapus kategori.', 'error');
        }
      });
  }
}
