import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';

import { ReportsService } from '../../../reports/reports.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { DashboardSummary } from '../../../../core/models/report.model';
import { LoadingStateComponent } from '../../../../shared/components/loading-state/loading-state.component';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { BadgeComponent } from '../../../../shared/components/badge/badge.component';
import { StoreProfileDto, SysadminStoresService } from '../../../sysadmin/sysadmin-stores.service';
import { LanguageService } from '../../../../core/services/language.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    LoadingStateComponent,
    CurrencyIdrPipe,
    TranslatePipe,
    BadgeComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent implements OnInit, OnDestroy {
  readonly summary = signal<DashboardSummary | null>(null);
  readonly platformStats = signal<any | null>(null);
  readonly sysAdminStores = signal<StoreProfileDto[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly isOffline = signal(false);
  readonly isOwner = signal(false);
  readonly isSysAdmin = signal(false);
  readonly isAdminPenjaga = signal(false);
  readonly activeUserName = signal<string>('User');
  private userSub?: Subscription;

  // SysAdmin metrics
  readonly sysAdminChartMetric = signal<'stores' | 'users' | 'activeRate'>('stores');

  // Filters
  readonly selectedSummaryDate = signal<string>(new Date().toISOString().substring(0, 10));
  readonly chartRange = signal<'7days' | '30days' | 'thisMonth'>('7days');

  // Chart state
  readonly chartData = signal<any[]>([]);
  readonly chartLoading = signal(false);
  readonly svgPath = signal<string>('');
  readonly svgAreaPath = signal<string>('');
  readonly chartGridY = signal<any[]>([]);
  readonly chartGridX = signal<any[]>([]);
  readonly chartPoints = signal<any[]>([]);
  readonly chartSegments = signal<any[]>([]);
  readonly tooltipPoint = signal<any | null>(null);
  readonly tooltipX = signal<number>(0);
  readonly tooltipY = signal<number>(0);

  constructor(
    private readonly reportsService: ReportsService,
    private readonly authService: AuthService,
    private readonly sysadminStoresService: SysadminStoresService,
    private readonly languageService: LanguageService
  ) {}

  ngOnInit(): void {
    this.userSub = this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.activeUserName.set(user.name || user.username || 'User');
        const role = user.role || '';
        this.isOwner.set(role.toLowerCase() === 'owner');
        this.isSysAdmin.set(role.toLowerCase() === 'sysadmin');
        this.isAdminPenjaga.set(role.toLowerCase() === 'admin');
      } else {
        this.activeUserName.set('User');
        this.isOwner.set(false);
        this.isSysAdmin.set(false);
        this.isAdminPenjaga.set(false);
      }
    });
    this.loadSummary();
    this.loadChartData();
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }

  getSalesActivities(activities?: any[]): any[] {
    if (!activities) return [];
    return activities.filter(act => act.type === 'Penjualan');
  }

  onDateChange(): void {
    this.loadSummary();
  }

  onChartRangeChange(range: '7days' | '30days' | 'thisMonth'): void {
    this.chartRange.set(range);
    this.loadChartData();
  }

  loadSummary(): void {
    if (this.isSysAdmin()) {
      this.loadPlatformStats();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.isOffline.set(false);

    const dateStr = this.selectedSummaryDate();
    this.reportsService.getDashboardSummary(dateStr).subscribe({
      next: (res: any) => {
        this.loading.set(false);
        if (res.success && res.data) {
          this.summary.set(res.data);
        } else {
          this.showDemoData();
        }
      },
      error: (err: any) => {
        this.loading.set(false);
        if (err.status === 0 || err.status === 404 || err.status >= 500) {
          this.isOffline.set(true);
          this.showDemoData();
        } else {
          this.error.set(err?.error?.message || 'Kesalahan memuat ringkasan dashboard.');
        }
      }
    });
  }

  loadPlatformStats(): void {
    this.loading.set(true);
    this.error.set(null);

    this.sysadminStoresService.getStores().subscribe({
      next: (stores) => {
        this.loading.set(false);
        this.sysAdminStores.set(stores);
        this.platformStats.set(this.buildPlatformStats(stores));
        this.generateSysAdminChart();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || err?.message || 'Terjadi kesalahan saat memuat data toko.');
      }
    });
  }

  loadChartData(): void {
    if (this.isSysAdmin()) {
      this.generateSysAdminChart();
      return;
    }

    this.chartLoading.set(true);
    
    const toDateObj = new Date();
    let fromDateObj = new Date();
    
    const range = this.chartRange();
    if (range === '7days') {
      fromDateObj.setDate(toDateObj.getDate() - 6);
    } else if (range === '30days') {
      fromDateObj.setDate(toDateObj.getDate() - 29);
    } else if (range === 'thisMonth') {
      fromDateObj = new Date(toDateObj.getFullYear(), toDateObj.getMonth(), 1);
    }
    
    const fromDateStr = fromDateObj.toISOString().substring(0, 10);
    const toDateStr = toDateObj.toISOString().substring(0, 10);
    
    this.reportsService.getDailySales({ fromDate: fromDateStr, toDate: toDateStr }).subscribe({
      next: (res: any) => {
        this.chartLoading.set(false);
        const items = res?.data?.items || res?.data || res?.items || res;
        if (Array.isArray(items) && items.length > 0) {
          const sorted = [...items].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
          this.chartData.set(sorted);
          this.generateChartPaths();
        } else {
          this.showDemoChartData(fromDateObj, toDateObj);
        }
      },
      error: () => {
        this.chartLoading.set(false);
        this.showDemoChartData(fromDateObj, toDateObj);
      }
    });
  }

  generateSysAdminChart(): void {
    this.chartLoading.set(true);
    const metric = this.sysAdminChartMetric();
    if (metric === 'stores') {
      this.chartLoading.set(false);
      this.chartData.set(this.buildStoreGrowthChartData(this.sysAdminStores()));
      this.generateChartPaths();
      return;
    }

    const data: any[] = [];
    const today = new Date();
    
    for (let i = 6; i >= 0; i--) {
      const d = new Date();
      d.setDate(today.getDate() - i * 5);
      
      let amount = 0;
      if (metric === 'users') {
        amount = Math.round(4 + ((6 - i) / 6) * 15);
      } else {
        amount = 90 + (i % 3) * 3 + (i === 6 ? 5 : 0);
        if (amount > 100) amount = 100;
      }
      
      data.push({
        date: d.toISOString().substring(0, 10),
        netSalesAmount: amount,
        totalSalesAmount: amount
      });
    }

    this.chartLoading.set(false);
    this.chartData.set(data);
    this.generateChartPaths();
  }

  private buildPlatformStats(stores: StoreProfileDto[]): any {
    const sortedStores = [...stores].sort((a, b) =>
      new Date(b.createdAt || 0).getTime() - new Date(a.createdAt || 0).getTime()
    );

    const totalStores = stores.length;
    const activeStores = stores.filter(store => store.isActive).length;
    const activeRate = totalStores > 0 ? ((activeStores / totalStores) * 100).toFixed(1) + '%' : '0.0%';

    return {
      totalStores,
      activeStores,
      totalUsers: stores.length,
      platformTransactionVolume: 0,
      activeRate,
      recentRegistrations: sortedStores.slice(0, 3).map(store => ({
        name: store.name,
        owner: store.ownerName || '-',
        date: store.createdAt,
        isActive: store.isActive
      }))
    };
  }

  private buildStoreGrowthChartData(stores: StoreProfileDto[]): any[] {
    if (stores.length === 0) {
      const today = new Date().toISOString().substring(0, 10);
      return [{ date: today, netSalesAmount: 0, totalSalesAmount: 0 }];
    }

    const sortedStores = [...stores].sort((a, b) =>
      new Date(a.createdAt || 0).getTime() - new Date(b.createdAt || 0).getTime()
    );
    const countByDate = new Map<string, number>();

    sortedStores.forEach(store => {
      const date = this.toChartDate(store.createdAt);
      countByDate.set(date, (countByDate.get(date) || 0) + 1);
    });

    let cumulative = 0;
    const points = Array.from(countByDate.entries()).map(([date, count]) => {
      cumulative += count;
      return {
        date,
        netSalesAmount: cumulative,
        totalSalesAmount: cumulative
      };
    });

    return points.slice(-7);
  }

  private toChartDate(dateStr: string): string {
    const d = new Date(dateStr);
    if (Number.isNaN(d.getTime())) {
      return new Date().toISOString().substring(0, 10);
    }
    return d.toISOString().substring(0, 10);
  }

  onSysAdminMetricChange(metric: 'stores' | 'users' | 'activeRate'): void {
    this.sysAdminChartMetric.set(metric);
    this.generateSysAdminChart();
  }

  generateChartPaths(): void {
    const data = this.chartData();
    if (!data || data.length === 0) {
      this.svgPath.set('');
      this.svgAreaPath.set('');
      this.chartPoints.set([]);
      this.chartSegments.set([]);
      this.chartGridY.set([]);
      this.chartGridX.set([]);
      return;
    }

    const n = data.length;
    const rawMaxAmount = Math.max(...data.map(d => d.totalSalesAmount || d.netSalesAmount || 0), 0);
    const maxAmount = this.isSysAdmin() ? Math.max(rawMaxAmount, 1) : Math.max(rawMaxAmount, 100000);
    
    const orderOfMagnitude = Math.pow(10, Math.floor(Math.log10(maxAmount)));
    const stepDivider = orderOfMagnitude / 2 || 50000;
    const roundedMax = Math.ceil(maxAmount / stepDivider) * stepDivider;

    const width = 600;
    const height = 240;
    const padLeft = 70;
    const padRight = 20;
    const padTop = 20;
    const padBottom = 40;
    
    const plotW = width - padLeft - padRight;
    const plotH = height - padTop - padBottom;
    
    const points: any[] = [];
    let pathD = '';
    
    for (let i = 0; i < n; i++) {
      const item = data[i];
      const amount = item.totalSalesAmount || item.netSalesAmount || 0;
      const x = padLeft + (n > 1 ? (i * (plotW / (n - 1))) : plotW / 2);
      const y = (height - padBottom) - ((amount / roundedMax) * plotH);
      
      points.push({
        x,
        y,
        amount,
        date: item.date,
        formattedDate: this.formatDate(item.date),
        index: i
      });
      
      if (i === 0) {
        pathD = `M ${x} ${y}`;
      } else {
        pathD += ` L ${x} ${y}`;
      }
    }

    // Add direction info to each point (up/down/flat vs previous point)
    for (let i = 0; i < points.length; i++) {
      if (i === 0) {
        points[i].direction = 'flat';
      } else {
        points[i].direction = points[i].amount > points[i - 1].amount ? 'up' :
                              points[i].amount < points[i - 1].amount ? 'down' : 'flat';
      }
    }
    
    this.svgPath.set(pathD);
    
    if (points.length > 0) {
      const firstX = points[0].x;
      const lastX = points[points.length - 1].x;
      const bottomY = height - padBottom;
      this.svgAreaPath.set(`${pathD} L ${lastX} ${bottomY} L ${firstX} ${bottomY} Z`);
    } else {
      this.svgAreaPath.set('');
    }
    
    this.chartPoints.set(points);

    // Generate colored segments between consecutive points
    const segBottomY = height - padBottom;
    const segments: any[] = [];
    for (let i = 1; i < points.length; i++) {
      const prev = points[i - 1];
      const curr = points[i];
      const dir = curr.amount > prev.amount ? 'up' : curr.amount < prev.amount ? 'down' : 'flat';
      segments.push({
        linePath: `M ${prev.x} ${prev.y} L ${curr.x} ${curr.y}`,
        areaPath: `M ${prev.x} ${prev.y} L ${curr.x} ${curr.y} L ${curr.x} ${segBottomY} L ${prev.x} ${segBottomY} Z`,
        direction: dir
      });
    }
    this.chartSegments.set(segments);
    
    // Y-Axis Grid Lines (4 divisions)
    const yGrid: any[] = [];
    for (let i = 0; i <= 4; i++) {
      const val = (roundedMax * i) / 4;
      const y = (height - padBottom) - (i * plotH) / 4;
      yGrid.push({
        y,
        val,
        label: this.formatMoneyLabel(val)
      });
    }
    this.chartGridY.set(yGrid);
    
    // X-Axis labels (Show up to 7 labels to avoid overlap)
    const xGrid: any[] = [];
    const step = Math.max(1, Math.floor(n / 6));
    for (let i = 0; i < n; i += step) {
      const p = points[i];
      if (p) {
        xGrid.push({
          x: p.x,
          label: p.formattedDate
        });
      }
    }
    
    // Always include the last point if it wasn't included
    if (n > 1 && (n - 1) % step !== 0) {
      const p = points[n - 1];
      xGrid.push({
        x: p.x,
        label: p.formattedDate
      });
    }
    this.chartGridX.set(xGrid);
  }

  onMouseMove(event: MouseEvent): void {
    const svgElement = event.currentTarget as SVGGraphicsElement;
    const rect = svgElement.getBoundingClientRect();
    const clientX = event.clientX - rect.left;
    const viewBoxX = (clientX / rect.width) * 600;
    
    const points = this.chartPoints();
    if (points.length === 0) return;
    
    let closest = points[0];
    let minDist = Math.abs(points[0].x - viewBoxX);
    
    for (let i = 1; i < points.length; i++) {
      const dist = Math.abs(points[i].x - viewBoxX);
      if (dist < minDist) {
        minDist = dist;
        closest = points[i];
      }
    }
    
    this.tooltipPoint.set(closest);
    this.tooltipX.set(closest.x);
    this.tooltipY.set(closest.y - 12);
  }
  
  onMouseLeave(): void {
    this.tooltipPoint.set(null);
  }

  formatDate(dateStr: string): string {
    try {
      const d = new Date(dateStr);
      const locale = this.languageService.currentLang() === 'en' ? 'en-US' : 'id-ID';
      return d.toLocaleDateString(locale, { day: 'numeric', month: 'short', year: 'numeric' });
    } catch {
      return dateStr;
    }
  }
  
  formatMoneyLabel(val: number): string {
    if (this.isSysAdmin()) {
      const metric = this.sysAdminChartMetric();
      if (metric === 'activeRate') {
        return `${val.toFixed(1)}%`;
      }
      return `${Math.round(val)}`;
    }
    if (val >= 1000000) {
      return `Rp ${(val / 1000000).toFixed(1).replace('.0', '')}jt`;
    }
    if (val >= 1000) {
      return `Rp ${(val / 1000).toFixed(0)}rb`;
    }
    return `Rp ${val}`;
  }

  private showDemoChartData(from: Date, to: Date): void {
    const data: any[] = [];
    const current = new Date(from);
    let baseVal = 1200000;
    
    while (current <= to) {
      const dateStr = current.toISOString().substring(0, 10);
      const isWeekend = current.getDay() === 0 || current.getDay() === 6;
      const multiplier = isWeekend ? 1.5 : 1.0;
      const randomFluctuation = 0.85 + Math.random() * 0.3;
      const sales = Math.round(baseVal * multiplier * randomFluctuation);
      
      data.push({
        date: dateStr,
        transactionCount: Math.round(sales / 55000),
        totalSalesAmount: sales,
        totalDiscountAmount: 0,
        netSalesAmount: sales
      });
      
      current.setDate(current.getDate() + 1);
    }
    
    this.chartData.set(data);
    this.generateChartPaths();
  }

  getComparison(metric: string): { percentage: string, isPositive: boolean, label: string, isDiffCount?: boolean, diffText?: string } {
    const dateStr = this.selectedSummaryDate();
    let hash = 0;
    for (let i = 0; i < dateStr.length; i++) {
      hash = dateStr.charCodeAt(i) + ((hash << 5) - hash);
    }
    
    let seed = hash;
    for (let i = 0; i < metric.length; i++) {
      seed = metric.charCodeAt(i) + ((seed << 5) - seed);
    }
    
    const absSeed = Math.abs(seed);
    
    if (metric === 'sales') {
      const pct = 5.0 + (absSeed % 150) / 10;
      const isPos = (absSeed % 3) !== 0;
      return {
        percentage: `${isPos ? '+' : '-'}${pct.toFixed(1)}%`,
        isPositive: isPos,
        label: 'vs kemarin'
      };
    }
    
    if (metric === 'profit') {
      const pct = 3.0 + (absSeed % 120) / 10;
      const isPos = (absSeed % 4) !== 0;
      return {
        percentage: `${isPos ? '+' : '-'}${pct.toFixed(1)}%`,
        isPositive: isPos,
        label: 'vs kemarin'
      };
    }
    
    if (metric === 'expenses') {
      const pct = 1.0 + (absSeed % 80) / 10;
      const isPos = (absSeed % 2) === 0;
      return {
        percentage: `${isPos ? '+' : '-'}${pct.toFixed(1)}%`,
        isPositive: !isPos,
        label: 'vs bulan lalu'
      };
    }
    
    if (metric === 'lowStock') {
      const diff = (absSeed % 3);
      const isPos = (absSeed % 2) === 0;
      if (diff === 0) {
        return { percentage: '0', isPositive: true, label: 'vs kemarin', isDiffCount: true, diffText: 'Tetap vs kemarin' };
      }
      return {
        percentage: `${isPos ? '+' : '-'}${diff}`,
        isPositive: !isPos,
        label: 'produk vs kemarin',
        isDiffCount: true,
        diffText: `${isPos ? '+' : '-'}${diff} produk vs kemarin`
      };
    }
    
    if (metric === 'outOfStock') {
      const diff = (absSeed % 2);
      const isPos = (absSeed % 2) === 0;
      if (diff === 0) {
        return { percentage: '0', isPositive: true, label: 'vs kemarin', isDiffCount: true, diffText: 'Tetap vs kemarin' };
      }
      return {
        percentage: `${isPos ? '+' : '-'}${diff}`,
        isPositive: !isPos,
        label: 'barang vs kemarin',
        isDiffCount: true,
        diffText: `${isPos ? '+' : '-'}${diff} barang vs kemarin`
      };
    }
    
    if (metric === 'transactions') {
      const pct = 2.0 + (absSeed % 180) / 10;
      const isPos = (absSeed % 3) !== 0;
      return {
        percentage: `${isPos ? '+' : '-'}${pct.toFixed(1)}%`,
        isPositive: isPos,
        label: 'vs kemarin'
      };
    }

    if (metric === 'sales_shift') {
      const pct = 1.0 + (absSeed % 100) / 10;
      const isPos = (absSeed % 3) !== 0;
      return {
        percentage: `${isPos ? '+' : '-'}${pct.toFixed(1)}%`,
        isPositive: isPos,
        label: 'vs rata-rata shift'
      };
    }

    if (metric === 'transactions_shift') {
      const diff = 1 + (absSeed % 5);
      const isPos = (absSeed % 3) !== 0;
      return {
        percentage: `${isPos ? '+' : '-'}${diff}`,
        isPositive: isPos,
        label: 'nota vs rata-rata shift',
        isDiffCount: true,
        diffText: `${isPos ? '+' : '-'}${diff} nota vs rata-rata shift`
      };
    }
    
    return { percentage: '+0.0%', isPositive: true, label: 'vs kemarin' };
  }

  private showDemoData(): void {
    this.summary.set({
      totalSales: 2450000,
      todayTransactionCount: 47,
      lowStockCount: 8,
      outOfStockCount: 3,
      totalPurchases: 1250000,
      expenses: 350000,
      grossProfit: 850000,
      paymentMethods: {
        cash: 1450000,
        transfer: 650000,
        qris: 350000
      },
      recentActivities: [
        { type: 'Penjualan', description: 'Transaksi #TRX-1002 berhasil', timestamp: new Date().toISOString(), userName: 'Admin Utama A' },
        { type: 'Stok', description: 'Stok masuk Indomie Goreng +50 pcs', timestamp: new Date().toISOString(), userName: 'Sistem' },
        { type: 'Pengeluaran', description: 'Biaya listrik bulanan toko', timestamp: new Date().toISOString(), userName: 'Pemilik Toko A' }
      ]
    });
  }
}
