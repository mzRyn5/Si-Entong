import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';

import { SalesService, CashSession, OpenCashSessionRequest, CloseCashSessionRequest } from '../../sales.service';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { FilterBarComponent } from '../../../../shared/components/filter-bar/filter-bar.component';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination.component';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-cash-session',
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
  templateUrl: './cash-session.component.html',
  styleUrls: ['./cash-session.component.scss']
})
export class CashSessionComponent implements OnInit, OnDestroy {
  readonly activeSession = signal<CashSession | null>(null);
  readonly loadingActive = signal(false);

  readonly sessions = signal<CashSession[]>([]);
  readonly loadingList = signal(false);
  readonly error = signal<string | null>(null);

  currentPage = 1;
  pageSize = 10;
  totalItems = signal(0);
  totalPages = signal(1);

  // Drawer states
  showOpenDrawer = signal(false);
  showCloseDrawer = signal(false);
  opening = signal(false);
  closing = signal(false);

  openFormModel = {
    openingCashAmount: 0,
    notes: ''
  };

  closeFormModel = {
    actualCashAmount: 0,
    notes: ''
  };

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly salesService: SalesService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadActiveSession();
    this.loadSessions();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadActiveSession(): void {
    this.loadingActive.set(true);
    this.salesService.getActiveCashSession()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.loadingActive.set(false);
          if (res.success && res.data) {
            this.activeSession.set(res.data);
          } else {
            this.activeSession.set(null);
          }
        },
        error: () => {
          this.loadingActive.set(false);
          this.activeSession.set(null);
        }
      });
  }

  loadSessions(): void {
    this.loadingList.set(true);
    this.error.set(null);

    this.salesService.getCashSessions({
      page: this.currentPage,
      pageSize: this.pageSize
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loadingList.set(false);
        if (res.success) {
          this.sessions.set(res.data);
          this.totalItems.set(res.meta.totalItems);
          this.totalPages.set(res.meta.totalPages);
        } else {
          this.error.set(res.message || 'Gagal memuat riwayat sesi kasir.');
        }
      },
      error: (err) => {
        this.loadingList.set(false);
        this.error.set(err?.error?.message || 'Kesalahan memuat riwayat sesi kasir.');
      }
    });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadSessions();
  }

  openOpenDrawer(): void {
    this.openFormModel = {
      openingCashAmount: 0,
      notes: ''
    };
    this.showOpenDrawer.set(true);
  }

  closeOpenDrawer(): void {
    this.showOpenDrawer.set(false);
  }

  onSubmitOpen(form: NgForm): void {
    if (form.invalid || this.opening()) return;

    this.opening.set(true);
    const request: OpenCashSessionRequest = {
      openingCashAmount: this.openFormModel.openingCashAmount,
      notes: this.openFormModel.notes.trim() || undefined
    };

    this.salesService.openCashSession(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.opening.set(false);
          if (res.success) {
            this.showOpenDrawer.set(false);
            this.toastService.show('Sesi kasir berhasil dibuka!', 'success');
            this.loadActiveSession();
            this.loadSessions();
          } else {
            this.toastService.show(res.message || 'Gagal membuka sesi kasir.', 'error');
          }
        },
        error: (err) => {
          this.opening.set(false);
          this.toastService.show(err?.error?.message || 'Terjadi kesalahan saat membuka sesi.', 'error');
        }
      });
  }

  openCloseDrawer(): void {
    const active = this.activeSession();
    if (!active) return;

    this.closeFormModel = {
      actualCashAmount: active.expectedCashAmount || 0,
      notes: ''
    };
    this.showCloseDrawer.set(true);
  }

  closeCloseDrawer(): void {
    this.showCloseDrawer.set(false);
  }

  onSubmitClose(form: NgForm): void {
    const active = this.activeSession();
    if (form.invalid || !active || this.closing()) return;

    this.closing.set(true);
    const request: CloseCashSessionRequest = {
      actualCashAmount: this.closeFormModel.actualCashAmount,
      notes: this.closeFormModel.notes.trim() || undefined
    };

    this.salesService.closeCashSession(active.id, request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.closing.set(false);
          if (res.success) {
            this.showCloseDrawer.set(false);
            this.toastService.show('Sesi kasir berhasil ditutup!', 'success');
            this.activeSession.set(null);
            this.loadActiveSession();
            this.loadSessions();
          } else {
            this.toastService.show(res.message || 'Gagal menutup sesi kasir.', 'error');
          }
        },
        error: (err) => {
          this.closing.set(false);
          this.toastService.show(err?.error?.message || 'Terjadi kesalahan saat menutup sesi.', 'error');
        }
      });
  }
}
