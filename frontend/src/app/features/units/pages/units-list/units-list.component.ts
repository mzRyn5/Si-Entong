import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { UnitsService, UnitItem, UnitCreateRequest, UnitUpdateRequest } from '../../units.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { NgIf, NgFor, NgClass } from '@angular/common';

@Component({
  selector: 'app-units-list',
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
  templateUrl: './units-list.component.html',
  styleUrls: ['./units-list.component.scss']
})
export class UnitsListComponent implements OnInit, OnDestroy {
  readonly units = signal<UnitItem[]>([]);
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
  selectedUnitId = '';
  formModel = {
    name: '',
    description: '',
    isActive: true
  };
  saving = signal(false);

  // Delete Dialog
  showDeleteDialog = signal(false);
  deleteTarget = signal<UnitItem | null>(null);
  deleting = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly unitsService: UnitsService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadUnits();
      });

    this.loadUnits();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadUnits(): void {
    this.loading.set(true);
    this.error.set(null);

    this.unitsService.getAll({
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
          this.units.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat satuan.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat satuan.');
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
    this.loadUnits();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadUnits();
  }

  openCreateForm(): void {
    this.isEditMode.set(false);
    this.selectedUnitId = '';
    this.formModel = {
      name: '',
      description: '',
      isActive: true
    };
    this.showFormDrawer.set(true);
  }

  openEditForm(u: UnitItem): void {
    this.isEditMode.set(true);
    this.selectedUnitId = u.id;
    this.formModel = {
      name: u.name,
      description: u.description || '',
      isActive: u.isActive !== false
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
      const request: UnitUpdateRequest = {
        name: this.formModel.name.trim(),
        description: this.formModel.description.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.unitsService.update(this.selectedUnitId, request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Satuan berhasil diperbarui.', 'success');
              this.loadUnits();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan satuan.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan satuan.', 'error');
          }
        });
    } else {
      const request: UnitCreateRequest = {
        name: this.formModel.name.trim(),
        description: this.formModel.description.trim() || undefined,
        isActive: this.formModel.isActive
      };
      this.unitsService.create(request)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (res) => {
            this.saving.set(false);
            if (res.success) {
              this.showFormDrawer.set(false);
              this.toastService.show('Satuan berhasil ditambahkan.', 'success');
              this.loadUnits();
            } else {
              this.toastService.show(res.message || 'Gagal menyimpan satuan.', 'error');
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.toastService.show(err?.error?.message || 'Gagal menyimpan satuan.', 'error');
          }
        });
    }
  }

  openDeleteConfirm(u: UnitItem): void {
    this.deleteTarget.set(u);
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
    this.unitsService.delete(target.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.deleting.set(false);
          this.showDeleteDialog.set(false);
          if (res.success) {
            this.toastService.show('Satuan berhasil dihapus.', 'success');
            this.loadUnits();
          } else {
            this.toastService.show(res.message || 'Gagal menghapus satuan.', 'error');
          }
        },
        error: (err) => {
          this.deleting.set(false);
          this.toastService.show(err?.error?.message || 'Gagal menghapus satuan.', 'error');
        }
      });
  }
}
