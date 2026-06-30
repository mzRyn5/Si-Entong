import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { PurchasesService, PurchaseCreateRequest, PurchaseItemRequest } from '../../purchases.service';
import { SuppliersService, SupplierItem } from '../../../suppliers/suppliers.service';
import { ProductsService, ProductListItem } from '../../../products/products.service';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { ToastService } from '../../../../shared/services/toast.service';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';

export interface PurchaseCartItem {
  productId: string;
  name: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  purchaseUnit?: string;
  purchaseQuantity?: number;
  multiplier?: number;
}

@Component({
  selector: 'app-purchase-create',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    CurrencyIdrPipe,
    TranslatePipe
  ],
  templateUrl: './purchase-create.component.html',
  styleUrls: ['./purchase-create.component.scss']
})
export class PurchaseCreateComponent implements OnInit, OnDestroy {
  readonly suppliers = signal<SupplierItem[]>([]);
  readonly products = signal<ProductListItem[]>([]);

  selectedSupplierId = '';
  purchaseDate = new Date().toISOString().substring(0, 10);
  paymentMethod = 'Cash';
  paymentStatus = 'Paid';
  discountAmount = 0;
  notes = '';

  // Purchase Cart Items
  readonly cart = signal<PurchaseCartItem[]>([]);

  // Item Form Input
  selectedProductId = '';
  itemQuantity = 1;
  itemUnitPrice = 0;
  itemSubtotal = 0;
  purchaseUnit = 'Pcs';
  itemMultiplier = 1;

  // Pencarian barang autocomplete
  productSearchQuery = '';
  isProductDropdownOpen = false;

  // Toggle visibilitas input grosir
  showWholesale = false;

  // Harga per satuan beli aktif (Pcs atau unit grosir)
  pricePerUnit = 0;

  amountPaid = 0;

  saving = signal(false);

  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly purchasesService: PurchasesService,
    private readonly suppliersService: SuppliersService,
    private readonly productsService: ProductsService,
    private readonly toastService: ToastService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadSuppliers();
    this.loadProducts();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadSuppliers(): void {
    this.suppliersService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.suppliers.set(res.data);
          }
        }
      });
  }

  loadProducts(): void {
    this.productsService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.products.set(res.data);
          }
        }
      });
  }

  get filteredProducts(): ProductListItem[] {
    const query = this.productSearchQuery.trim().toLowerCase();
    if (!query) {
      return this.products();
    }
    return this.products().filter(p =>
      p.name.toLowerCase().includes(query) ||
      p.sku.toLowerCase().includes(query)
    );
  }

  selectProduct(p: ProductListItem): void {
    this.selectedProductId = p.id;
    this.productSearchQuery = `${p.sku} - ${p.name}`;
    this.isProductDropdownOpen = false;
    this.onProductSelectChange();
  }

  clearProductSelection(): void {
    this.selectedProductId = '';
    this.productSearchQuery = '';
    this.isProductDropdownOpen = false;
    this.onProductSelectChange();
  }

  onProductSearchInput(): void {
    this.isProductDropdownOpen = true;
    if (!this.productSearchQuery) {
      this.clearProductSelection();
    }
  }

  closeProductDropdownWithDelay(): void {
    setTimeout(() => {
      this.isProductDropdownOpen = false;
      if (this.selectedProductId) {
        const p = this.products().find(item => item.id === this.selectedProductId);
        if (p) {
          this.productSearchQuery = `${p.sku} - ${p.name}`;
        }
      } else {
        this.productSearchQuery = '';
      }
    }, 200);
  }

  onProductSelectChange(): void {
    const p = this.products().find(item => item.id === this.selectedProductId);
    if (p) {
      const estimatedPcsPrice = Math.round(p.sellingPrice * 0.7);
      this.itemUnitPrice = estimatedPcsPrice;
      this.itemQuantity = 1;
      this.purchaseUnit = 'Pcs';
      this.itemMultiplier = 1;
      this.showWholesale = false;
      this.pricePerUnit = estimatedPcsPrice;
      this.calculateSubtotal();
    } else {
      this.itemUnitPrice = 0;
      this.itemQuantity = 1;
      this.purchaseUnit = 'Pcs';
      this.itemMultiplier = 1;
      this.showWholesale = false;
      this.pricePerUnit = 0;
      this.itemSubtotal = 0;
    }
  }

  toggleWholesale(show: boolean): void {
    this.showWholesale = show;
    if (!show) {
      this.purchaseUnit = 'Pcs';
      this.itemMultiplier = 1;
    } else {
      if (this.purchaseUnit === 'Pcs') {
        this.purchaseUnit = 'Lusin';
        this.itemMultiplier = 12;
      }
    }
    this.pricePerUnit = this.itemUnitPrice * this.itemMultiplier;
    this.calculateSubtotal();
  }

  onUnitChange(): void {
    switch (this.purchaseUnit) {
      case 'Lusin':
        this.itemMultiplier = 12;
        break;
      case 'Dus':
        this.itemMultiplier = 24;
        break;
      case 'Pack':
        this.itemMultiplier = 10;
        break;
      case 'Renceng':
        this.itemMultiplier = 10;
        break;
      case 'Pcs':
      default:
        this.itemMultiplier = 1;
        break;
    }
    this.pricePerUnit = this.itemUnitPrice * this.itemMultiplier;
    this.calculateSubtotal();
  }

  calculateSubtotal(): void {
    this.itemSubtotal = this.itemQuantity * this.pricePerUnit;
    if (this.itemMultiplier > 0) {
      this.itemUnitPrice = Math.round(this.pricePerUnit / this.itemMultiplier);
    } else {
      this.itemUnitPrice = this.pricePerUnit;
    }
  }

  onQuantityChange(): void {
    this.calculateSubtotal();
  }

  onMultiplierChange(): void {
    this.calculateSubtotal();
  }

  onUnitPriceChange(): void {
    this.pricePerUnit = this.itemUnitPrice * this.itemMultiplier;
    this.calculateSubtotal();
  }

  onPricePerUnitChange(): void {
    this.calculateSubtotal();
  }

  onSubtotalChange(): void {
    if (this.itemQuantity > 0) {
      this.pricePerUnit = Math.round(this.itemSubtotal / this.itemQuantity);
      if (this.itemMultiplier > 0) {
        this.itemUnitPrice = Math.round(this.pricePerUnit / this.itemMultiplier);
      } else {
        this.itemUnitPrice = this.pricePerUnit;
      }
    }
  }

  addItemToCart(): void {
    if (!this.selectedProductId) return;

    const p = this.products().find(item => item.id === this.selectedProductId);
    if (!p) return;

    if (this.itemQuantity <= 0) {
      this.toastService.show('Jumlah harus lebih besar dari 0!', 'error');
      return;
    }

    if (this.itemUnitPrice < 0) {
      this.toastService.show('Harga beli tidak boleh negatif!', 'error');
      return;
    }

    const currentCart = this.cart();
    const existing = currentCart.find(item => item.productId === p.id);

    const totalQty = this.itemQuantity * this.itemMultiplier;

    if (existing) {
      existing.quantity += totalQty;
      existing.unitPrice = this.itemUnitPrice; // use latest price input
      this.cart.set([...currentCart]);
      this.toastService.show(`Jumlah ${p.name} ditambah menjadi ${existing.quantity} pcs.`, 'success');
    } else {
      const newItem: PurchaseCartItem = {
        productId: p.id,
        name: p.name,
        sku: p.sku,
        quantity: totalQty,
        unitPrice: this.itemUnitPrice,
        purchaseUnit: this.purchaseUnit,
        purchaseQuantity: this.itemQuantity,
        multiplier: this.itemMultiplier
      };
      this.cart.set([...currentCart, newItem]);
      this.toastService.show(`${p.name} ditambahkan ke keranjang pembelian.`, 'success');
    }

    // Reset item input form
    this.selectedProductId = '';
    this.productSearchQuery = '';
    this.isProductDropdownOpen = false;
    this.showWholesale = false;
    this.pricePerUnit = 0;
    this.itemQuantity = 1;
    this.itemUnitPrice = 0;
    this.itemSubtotal = 0;
    this.purchaseUnit = 'Pcs';
    this.itemMultiplier = 1;
  }

  removeItem(item: PurchaseCartItem): void {
    this.cart.set(this.cart().filter(i => i.productId !== item.productId));
    this.toastService.show(`${item.name} dihapus dari keranjang.`, 'success');
  }

  getSubtotal(): number {
    return this.cart().reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
  }

  getTotal(): number {
    return Math.max(0, this.getSubtotal() - this.discountAmount);
  }

  onSubmitPurchase(form: NgForm): void {
    if (form.invalid || this.cart().length === 0 || this.saving()) {
      return;
    }

    this.saving.set(true);

    const itemsRequest: PurchaseItemRequest[] = this.cart().map(item => ({
      productId: item.productId,
      quantity: item.quantity,
      unitPrice: item.unitPrice
    }));

    const request: PurchaseCreateRequest = {
      supplierId: this.selectedSupplierId || undefined,
      purchaseDate: new Date(this.purchaseDate).toISOString(),
      items: itemsRequest,
      discountAmount: this.discountAmount || undefined,
      amountPaid: this.paymentStatus !== 'Paid' ? this.amountPaid : undefined,
      paymentMethod: this.paymentMethod,
      paymentStatus: this.paymentStatus,
      notes: this.notes.trim() || undefined
    };

    this.purchasesService.create(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          if (res.success) {
            this.toastService.show('Transaksi pembelian berhasil disimpan!', 'success');
            void this.router.navigate(['/purchases']);
          } else {
            this.toastService.show(res.message || 'Gagal mencatat pembelian.', 'error');
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.show(err?.error?.message || 'Terjadi kesalahan saat memproses pembelian.', 'error');
        }
      });
  }
}
