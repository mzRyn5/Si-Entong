import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';

import { AuditLogService, AuditLogItem } from '../../../../core/services/audit-log.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-audit-logs-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    BadgeComponent,
    FilterBarComponent,
    LoadingStateComponent,
    PaginationComponent,
    DateIdPipe,
    TranslatePipe
  ],
  templateUrl: './audit-logs-list.component.html',
  styleUrls: ['./audit-logs-list.component.scss']
})
export class AuditLogsListComponent implements OnInit, OnDestroy {
  readonly logs = signal<AuditLogItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  filterModule = '';
  filterAction = '';
  fromDate = '';
  toDate = '';

  currentPage = 1;
  pageSize = 20;
  totalItems = signal(0);
  totalPages = signal(1);

  // Detail Drawer
  showDetailDrawer = signal(false);
  selectedLog = signal<AuditLogItem | null>(null);

  isOwner = signal(false);

  private readonly destroy$ = new Subject<void>();
  private readonly filterChange$ = new Subject<void>();

  constructor(
    private readonly auditLogService: AuditLogService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.isOwner.set(this.authService.hasAnyRole(['owner']));
    
    this.filterChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        this.loadLogs();
      });

    if (this.isOwner()) {
      this.loadLogs();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadLogs(): void {
    this.loading.set(true);
    this.error.set(null);

    this.auditLogService.getAll({
      page: this.currentPage,
      pageSize: this.pageSize,
      module: this.filterModule || undefined,
      action: this.filterAction || undefined,
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.success) {
          this.logs.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat log aktivitas.');
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat log aktivitas.');
      }
    });
  }

  onFilterChange(): void {
    this.filterChange$.next();
  }

  clearFilters(): void {
    this.filterModule = '';
    this.filterAction = '';
    this.fromDate = '';
    this.toDate = '';
    this.currentPage = 1;
    this.loadLogs();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadLogs();
  }

  viewDetail(log: AuditLogItem): void {
    this.selectedLog.set(log);
    this.showDetailDrawer.set(true);
  }

  closeDetailDrawer(): void {
    this.showDetailDrawer.set(false);
    this.selectedLog.set(null);
  }

  parseJSON(value?: string): any {
    if (!value) return null;
    try {
      return JSON.parse(value);
    } catch {
      return null;
    }
  }

  objectKeys(obj: any): string[] {
    return obj ? Object.keys(obj) : [];
  }
}
