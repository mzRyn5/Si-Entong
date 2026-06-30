import { Component, OnInit, OnDestroy } from '@angular/core';
import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';
import { SettingsService } from '../../features/settings/settings.service';
import { LanguageService } from '../../core/services/language.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';

interface NavigationItem {
  label: string;
  route: string;
  icon: string;
  ownerOnly?: boolean;
  sysAdminOnly?: boolean;
  group?: string;
  isGroupHeader?: boolean;
  children?: NavigationItem[];
}

interface NavigationGroup {
  label: string;
  icon: string;
  ownerOnly?: boolean;
  sysAdminOnly?: boolean;
  expanded?: boolean;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [AsyncPipe, NgClass, NgIf, RouterLink, RouterLinkActive, RouterOutlet, TranslatePipe],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.scss'
})
export class AppShellComponent implements OnInit {
  readonly navigationGroups: NavigationGroup[] = [
    { label: 'Platform', icon: 'admin_panel_settings', sysAdminOnly: true, expanded: true },
    { label: 'Dashboard', icon: 'dashboard', expanded: true },
    { label: 'Transaksi', icon: 'swap_horiz', expanded: true },
    { label: 'Data Master', icon: 'inventory', expanded: false },
    { label: 'Inventori', icon: 'warehouse', expanded: true },
    { label: 'Laporan', icon: 'analytics', ownerOnly: true, expanded: true },
    { label: 'Sistem', icon: 'settings', ownerOnly: true, expanded: false }
  ];

  readonly navigationItems: NavigationItem[] = [
    // Platform (Sys Admin only)
    { label: 'Kelola Toko', route: '/sysadmin/stores', icon: 'storefront', group: 'Platform', sysAdminOnly: true },

    // Dashboard
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard', group: 'Dashboard' },

    // Transaksi
    { label: 'Penjualan / POS', route: '/sales', icon: 'point_of_sale', group: 'Transaksi' },
    { label: 'Pembelian', route: '/purchases', icon: 'shopping_cart', group: 'Transaksi' },
    { label: 'Piutang', route: '/receivables', icon: 'account_balance', group: 'Transaksi' },
    { label: 'Hutang', route: '/payables', icon: 'credit_card', group: 'Transaksi' },

    // Data Master
    { label: 'Produk', route: '/products', icon: 'inventory_2', group: 'Data Master' },
    { label: 'Kategori', route: '/categories', icon: 'category', group: 'Data Master' },
    { label: 'Satuan', route: '/units', icon: 'straighten', group: 'Data Master' },
    { label: 'Supplier', route: '/suppliers', icon: 'local_shipping', group: 'Data Master' },
    { label: 'Pelanggan', route: '/customers', icon: 'groups', group: 'Data Master' },

    // Inventori
    { label: 'Stok', route: '/inventory', icon: 'inventory', group: 'Inventori' },
    { label: 'Pengeluaran', route: '/expenses', icon: 'payments', group: 'Inventori' },

    // Laporan
    { label: 'Laporan', route: '/reports', icon: 'assessment', group: 'Laporan', ownerOnly: true },

    // Sistem (Owner only)
    { label: 'Pengaturan', route: '/settings', icon: 'store', group: 'Sistem', ownerOnly: true },
    { label: 'User', route: '/users', icon: 'manage_accounts', group: 'Sistem', ownerOnly: true },
    { label: 'Log Aktivitas', route: '/audit-logs', icon: 'history', group: 'Sistem', ownerOnly: true }
  ];

  readonly sysAdminNavigationItems: NavigationItem[] = [
    { label: 'Dashboard', route: '/dashboard', icon: 'dashboard' },
    { label: 'Kelola Toko', route: '/sysadmin/stores', icon: 'storefront', sysAdminOnly: true },
    { label: 'User', route: '/users', icon: 'manage_accounts' }
  ];

  isSidebarOpen = true;
  isMobile = false;

  private readonly MOBILE_BREAKPOINT = 1024;

  constructor(
    readonly authService: AuthService,
    readonly settingsService: SettingsService,
    readonly languageService: LanguageService,
    private readonly router: Router
  ) {
    this.checkMobile();
    this.isSidebarOpen = !this.isMobile;
    window.addEventListener('resize', () => this.checkMobile());
  }

  private checkMobile(): void {
    const wasMobile = this.isMobile;
    this.isMobile = window.innerWidth < this.MOBILE_BREAKPOINT;
    if (this.isMobile !== wasMobile) {
      this.isSidebarOpen = !this.isMobile;
    }
  }

  ngOnInit(): void {
    // SysAdmin doesn't belong to a specific store — skip store profile loading
    if (this.authService.isAuthenticated() && !this.isSysAdmin()) {
      this.settingsService.getProfile().subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.settingsService.storeProfile$.subscribe();
          }
        },
        error: (err) => {
          // Silently fail - profile is optional for basic functionality
          console.debug('Store profile not available:', err?.status);
        }
      });
    }
  }

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  closeSidebar(): void {
    this.isSidebarOpen = false;
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part[0]?.toUpperCase())
      .join('') || 'U';
  }

  getItemsByGroup(groupLabel: string): NavigationItem[] {
    return this.navigationItems.filter(item => item.group === groupLabel);
  }

  isSysAdmin(): boolean {
    return this.authService.hasAnyRole(['sysadmin']);
  }

  isGroupVisible(group: NavigationGroup): boolean {
    const isSys = this.authService.hasAnyRole(['sysadmin']);
    if (group.sysAdminOnly) return isSys;
    if (group.label === 'Dashboard') return true;
    if (group.label === 'Sistem') return true;
    if (isSys) return false;
    if (!group.ownerOnly) return true;
    return this.authService.hasAnyRole(['owner']);
  }

  isItemVisible(item: NavigationItem): boolean {
    const isSys = this.authService.hasAnyRole(['sysadmin']);
    if (item.sysAdminOnly) return isSys;
    if (item.route === '/dashboard') return true;
    if (isSys) return false;
    if (!item.ownerOnly) return true;
    return this.authService.hasAnyRole(['owner']);
  }

  toggleGroup(group: NavigationGroup): void {
    group.expanded = !group.expanded;
  }

  hasVisibleItems(group: NavigationGroup): boolean {
    if (!this.isGroupVisible(group)) return false;
    const items = this.getItemsByGroup(group.label).filter(item => this.isItemVisible(item));
    return items.length > 0;
  }
}
