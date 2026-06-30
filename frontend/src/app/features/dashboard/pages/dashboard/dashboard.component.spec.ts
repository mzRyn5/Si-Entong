import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { DashboardComponent } from './dashboard.component';
import { AuthService } from '../../../../core/auth/auth.service';
import { ReportsService } from '../../../reports/reports.service';
import { SysadminStoresService } from '../../../sysadmin/sysadmin-stores.service';

describe('DashboardComponent sysadmin layout', () => {
  let fixture: ComponentFixture<DashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            currentUser$: of({ name: 'Sysadmin', username: 'sysadmin', role: 'sysadmin' })
          }
        },
        {
          provide: ReportsService,
          useValue: {
            getDashboardSummary: () => of({ success: true, data: {} }),
            getDailySales: () => of([])
          }
        },
        {
          provide: SysadminStoresService,
          useValue: {
            getStores: () => of([
              {
                id: '1',
                name: 'Toko Baru Satu',
                address: '',
                phone: '',
                currency: 'IDR',
                timezone: 'Asia/Jakarta',
                isActive: true,
                createdAt: '2026-06-29T00:00:00.000Z',
                ownerName: 'Owner Satu'
              }
            ])
          }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
  });

  it('places the sysadmin chart beside latest store registrations and anchors y-axis labels on the left', fakeAsync(() => {
    fixture.detectChanges();
    tick();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const sideBySideGrid = root.querySelector('.sysadmin-insight-grid');
    const chartCard = root.querySelector('[data-sysadmin-panel="platform-chart"]');
    const latestStoresCard = root.querySelector('[data-sysadmin-panel="latest-stores"]');
    const yAxisLabel = root.querySelector('[data-chart-axis="sysadmin-y-label"]');

    expect(sideBySideGrid).withContext('sysadmin chart and latest stores should share a responsive grid').not.toBeNull();
    expect(chartCard).withContext('platform chart card should be identifiable inside the grid').not.toBeNull();
    expect(latestStoresCard).withContext('latest store registrations card should be identifiable inside the grid').not.toBeNull();
    expect(sideBySideGrid?.contains(chartCard)).toBeTrue();
    expect(sideBySideGrid?.contains(latestStoresCard)).toBeTrue();
    expect(yAxisLabel?.getAttribute('text-anchor')).toBe('end');
  }));
});
