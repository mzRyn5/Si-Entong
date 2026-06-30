import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { CategoriesService, CategoryItem, CategoryCreateRequest, CategoryUpdateRequest } from '../../categories.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { NgIf, NgFor, NgClass } from '@angular/common';

@Component({
  selector: 'app-categories-list',
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
  templateUrl: './categories-list.component.html',
  styleUrls: ['./categories-list.component.scss']
})
export class CategoriesListComponent implements OnInit, OnDestroy {
  readonly categories = signal<CategoryItem[]>([]);
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
  selectedCategoryId = '';
  formModel = {
    name: '',
    description: '',
    isActive: true
  };
  saving = signal(false);

  // Delete Dialog
  showDeleteDialog = signal(false);
  deleteTarget = signal<CategoryItem | null>(null);
  deleting = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly categoriesService: CategoriesService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadCategories();
      });

    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCategories(): void {
    this.loading.set(true);
    this.error.set(null);

    this.categoriesService.getAll({
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
          this.categories.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat kategori.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat kategori.');
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
    this.loadCategories();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadCategories();
  }

  openCreateForm(): void {
    this.isEditMode.set(false);
    this.selectedCategoryId = '';
    this.formModel = {
      name: '',
      description: '',
      isActive: true
    };
    this.showFormDrawer.set(true);
  }

  openEditForm(c: CategoryItem): void {
    this.isEditMode.set(true);
    this.selectedCategoryId = c.id;
    this.formModel = {
      name: c.name,
      description: c.description || '',
      isActive: c.isActive !== false
    };
    this.showFormDrawer.set(true);
  }

  closeFormDrawer(): void {
    this.showFormDrawer.set(false);
  }

  onSubmitForm(form: NgForm): void {
    if (form.invalid) return;

    this.saving.set(true);

    if (this.isEditMode()) {
      const request: CategoryUpdateRequest = {
        name: this.formModel.name.trim(),
        description: this.formModel.description.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.categoriesService.update(this.selectedCategoryId, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Kategori berhasil diperbarui.', 'success');
              this.loadCategories();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan kategori.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan kategori.', 'error');
          }
        });
    } else {
      const request: CategoryCreateRequest = {
        name: this.formModel.name.trim(),
        description: this.formModel.description.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.categoriesService.create(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Kategori berhasil ditambahkan.', 'success');
              this.loadCategories();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan kategori.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan kategori.', 'error');
          }
        });
    }
  }

  openDeleteConfirm(c: CategoryItem): void {
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
    this.categoriesService.delete(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleting.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show('Kategori berhasil dihapus.', 'success');
            this.loadCategories();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus kategori.', 'error');
          }
        },
        error: (err) => {
          this.deleting.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menghapus kategori.', 'error');
        }
      });
  }
}
