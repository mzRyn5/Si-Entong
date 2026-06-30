import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { jsPDF } from 'jspdf';

import { SalesService, SaleCreateRequest, SaleItemRequest, ReceiptData } from '../../sales.service';
import { ProductsService, ProductListItem } from '../../../products/products.service';
import { CustomersService, CustomerItem } from '../../../customers/customers.service';
import { CurrencyIdrPipe } from '../../../../shared/pipes/currency-idr.pipe';
import { TranslatePipe } from '../../../../shared/pipes/translate.pipe';
import { DateIdPipe } from '../../../../shared/pipes/date-id.pipe';
import { ToastService } from '../../../../shared/services/toast.service';

export interface CartItem {
  productId: string;
  name: string;
  sku: string;
  sellingPrice: number;
  quantity: number;
  maxStock: number;
}

@Component({
  selector: 'app-pos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    CurrencyIdrPipe,
    TranslatePipe,
    DateIdPipe
  ],
  templateUrl: './pos.component.html',
  styleUrls: ['./pos.component.scss']
})
export class PosComponent implements OnInit, OnDestroy {
  // Products & Customers
  readonly products = signal<ProductListItem[]>([]);
  readonly customers = signal<CustomerItem[]>([]);
  readonly loadingProducts = signal(false);

  searchQuery = '';
  filterCategory = '';
  selectedCustomerId = '';
  paymentMethod = 'Cash';
  paymentStatus = 'Paid';
  dueDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().substring(0, 10);
  amountPaid = 0;
  discountAmount = 0;
  taxAmount = 0;
  readonly showTaxInput = signal(false);
  notes = '';

  // POS Cart
  readonly cart = signal<CartItem[]>([]);
  readonly saving = signal(false);

  // Receipt Modal
  showReceiptModal = signal(false);
  readonly receiptData = signal<ReceiptData | null>(null);

  private readonly destroy$ = new Subject<void>();
  private readonly searchChange$ = new Subject<void>();

  constructor(
    private readonly salesService: SalesService,
    private readonly productsService: ProductsService,
    private readonly customersService: CustomersService,
    private readonly toastService: ToastService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.loadCustomers();
    this.loadProducts();

    this.searchChange$
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadProducts();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadCustomers(): void {
    this.customersService.getAll({ page: 1, pageSize: 100, isActive: true })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.customers.set(res.data);
          }
        }
      });
  }

  loadProducts(): void {
    this.loadingProducts.set(true);
    this.productsService.getAll({
      page: 1,
      pageSize: 30,
      search: this.searchQuery.trim() || undefined,
      isActive: true
    })
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (res) => {
        this.loadingProducts.set(false);
        if (res.success) {
          this.products.set(res.data);
        }
      },
      error: () => {
        this.loadingProducts.set(false);
      }
    });
  }

  onSearchChange(): void {
    this.searchChange$.next();
  }

  addToCart(p: ProductListItem): void {
    if (p.currentStock <= 0) {
      this.toastService.show('Stok produk habis!', 'error');
      return;
    }

    const currentCart = this.cart();
    const existing = currentCart.find(item => item.productId === p.id);

    if (existing) {
      if (existing.quantity >= p.currentStock) {
        this.toastService.show('Jumlah melebihi stok produk!', 'error');
        return;
      }
      existing.quantity += 1;
      this.cart.set([...currentCart]);
      this.toastService.show(`Jumlah ${p.name} ditambah menjadi ${existing.quantity}.`, 'success');
    } else {
      const newItem: CartItem = {
        productId: p.id,
        name: p.name,
        sku: p.sku,
        sellingPrice: p.sellingPrice,
        quantity: 1,
        maxStock: p.currentStock
      };
      this.cart.set([...currentCart, newItem]);
      this.toastService.show(`${p.name} ditambahkan ke keranjang.`, 'success');
    }

    // Default cash amount paid if payment method is Transfer or QRIS to auto-match total
    this.updateAmountPaid();
  }

  updateQuantity(item: CartItem, amount: number): void {
    const currentCart = this.cart();
    const target = currentCart.find(i => i.productId === item.productId);
    if (!target) return;

    const newQty = target.quantity + amount;
    if (newQty <= 0) {
      this.removeFromCart(item);
    } else {
      if (newQty > item.maxStock) {
        this.toastService.show('Jumlah melebihi stok produk!', 'error');
        return;
      }
      target.quantity = newQty;
      this.cart.set([...currentCart]);
      if (amount > 0) {
        this.toastService.show(`Jumlah ${item.name} ditambah menjadi ${newQty}.`, 'success');
      } else {
        this.toastService.show(`Jumlah ${item.name} dikurangi menjadi ${newQty}.`, 'success');
      }
    }
    this.updateAmountPaid();
  }

  removeFromCart(item: CartItem): void {
    const currentCart = this.cart();
    this.cart.set(currentCart.filter(i => i.productId !== item.productId));
    this.updateAmountPaid();
    this.toastService.show(`${item.name} dihapus dari keranjang.`, 'success');
  }

  clearCart(): void {
    this.cart.set([]);
    this.discountAmount = 0;
    this.taxAmount = 0;
    this.showTaxInput.set(false);
    this.amountPaid = 0;
    this.notes = '';
    this.selectedCustomerId = '';
    this.paymentStatus = 'Paid';
    this.dueDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().substring(0, 10);
    this.toastService.show('Keranjang belanja berhasil dikosongkan.', 'success');
  }

  // Calculations
  getSubtotal(): number {
    return this.cart().reduce((sum, item) => sum + (item.sellingPrice * item.quantity), 0);
  }

  getTaxAmount(): number {
    return this.taxAmount;
  }

  getTotal(): number {
    return Math.max(0, this.getSubtotal() - this.discountAmount + this.getTaxAmount());
  }

  toggleTaxInput(): void {
    const show = !this.showTaxInput();
    this.showTaxInput.set(show);
    if (show) {
      const taxableAmount = Math.max(0, this.getSubtotal() - this.discountAmount);
      this.taxAmount = Math.round(taxableAmount * 0.11);
    } else {
      this.taxAmount = 0;
    }
    this.updateAmountPaid();
  }

  onTaxChange(): void {
    if (this.taxAmount < 0) {
      this.taxAmount = 0;
    }
    this.updateAmountPaid();
  }

  onDiscountChange(): void {
    if (this.discountAmount < 0) {
      this.discountAmount = 0;
    }
    this.updateAmountPaid();
  }

  getChange(): number {
    if (this.paymentMethod !== 'Cash') return 0;
    return this.amountPaid - this.getTotal();
  }

  setExactAmount(): void {
    this.amountPaid = this.getTotal();
  }

  addCashAmount(amount: number): void {
    this.amountPaid = (this.amountPaid || 0) + amount;
  }

  onPaymentMethodChange(): void {
    this.updateAmountPaid();
  }

  onPaymentStatusChange(): void {
    if (this.paymentStatus === 'Unpaid') {
      this.amountPaid = 0;
    } else if (this.paymentStatus === 'Paid') {
      this.updateAmountPaid();
    } else if (this.paymentStatus === 'Partial') {
      this.amountPaid = Math.round(this.getTotal() * 0.5);
    }
  }

  private updateAmountPaid(): void {
    if (this.paymentMethod !== 'Cash') {
      this.amountPaid = this.getTotal();
    }
  }

  onSubmitSale(form: NgForm): void {
    if (this.cart().length === 0 || this.saving()) {
      return;
    }

    if (this.paymentStatus === 'Paid' && this.paymentMethod === 'Cash' && this.amountPaid < this.getTotal()) {
      this.toastService.show('Uang dibayar kurang!', 'error');
      return;
    }

    if (this.paymentStatus !== 'Paid' && !this.selectedCustomerId) {
      this.toastService.show('Pelanggan wajib dipilih untuk transaksi hutang/piutang!', 'error');
      return;
    }

    if (this.paymentStatus === 'Partial') {
      if (this.amountPaid <= 0) {
        this.toastService.show('Nominal pembayaran sebagian harus lebih dari 0!', 'error');
        return;
      }
      if (this.amountPaid >= this.getTotal()) {
        this.toastService.show('Nominal pembayaran sebagian tidak boleh melebihi atau sama dengan total belanja!', 'error');
        return;
      }
    }

    this.saving.set(true);

    const itemsRequest: SaleItemRequest[] = this.cart().map(item => ({
      productId: item.productId,
      quantity: item.quantity,
      unitPrice: item.sellingPrice
    }));

    const request: SaleCreateRequest = {
      saleDate: new Date().toISOString(),
      customerId: this.selectedCustomerId || undefined,
      cashSessionId: undefined, // Bypassed for simple offline POS flow
      items: itemsRequest,
      discountAmount: this.discountAmount || undefined,
      taxAmount: this.getTaxAmount() || undefined,
      paymentMethod: this.paymentMethod,
      paymentStatus: this.paymentStatus,
      amountPaid: this.paymentStatus === 'Unpaid' ? 0 : this.amountPaid,
      notes: this.notes.trim() || undefined,
      dueDate: this.paymentStatus !== 'Paid' ? new Date(this.dueDate).toISOString() : undefined
    };

    this.salesService.create(request)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.saving.set(false);
          if (res.success && res.data) {
            this.toastService.show('Transaksi berhasil disimpan!', 'success');
            this.loadReceipt(res.data.id);
          } else {
            this.toastService.show(res.message || 'Gagal menyimpan transaksi.', 'error');
          }
        },
        error: (err) => {
          this.saving.set(false);
          this.toastService.show(err?.error?.message || 'Terjadi kesalahan saat memproses POS.', 'error');
        }
      });
  }

  loadReceipt(saleId: string): void {
    this.salesService.getReceipt(saleId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.receiptData.set(res.data);
            this.showReceiptModal.set(true);
            this.clearCart();
            this.loadProducts(); // refresh stocks
          }
        }
      });
  }

  closeReceiptModal(): void {
    this.showReceiptModal.set(false);
    this.receiptData.set(null);
    this.router.navigate(['/sales']);
  }

  printReceipt(): void {
    const printContent = document.getElementById('receipt-print-area');
    if (!printContent) return;

    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = '0';
    document.body.appendChild(iframe);

    const doc = iframe.contentWindow?.document || iframe.contentDocument;
    if (!doc) return;

    doc.write(`
      <html>
        <head>
          <title>Print Receipt</title>
          <style>
            body {
              font-family: 'Courier New', Courier, monospace;
              font-size: 12px;
              line-height: 1.4;
              margin: 0;
              padding: 10px;
              width: 80mm;
            }
            .text-center { text-align: center; }
            .text-right { text-align: right; }
            .font-bold { font-weight: bold; }
            .flex { display: flex; }
            .justify-between { justify-content: space-between; }
            .my-2 { margin-top: 8px; margin-bottom: 8px; }
            .space-y-1 > * + * { margin-top: 4px; }
            .space-y-2 > * + * { margin-top: 8px; }
            .uppercase { text-transform: uppercase; }
            .text-text-muted { color: #666; }
            .border-t { border-top: 1px dashed #000; }
            .border-b { border-bottom: 1px dashed #000; }
            .no-print { display: none !important; }
          </style>
        </head>
        <body>
          ${printContent.innerHTML}
          <script>
            window.onload = function() {
              window.focus();
              window.print();
              setTimeout(function() {
                window.frameElement.remove();
              }, 100);
            }
          </script>
        </body>
      </html>
    `);
    doc.close();
  }

  downloadReceiptPdf(): void {
    const receipt = this.receiptData();
    if (!receipt) return;

    // Create jsPDF instance
    // Custom thermal roll size: 80mm wide, 150mm tall (standard receipt size)
    const doc = new jsPDF({
      orientation: 'portrait',
      unit: 'mm',
      format: [80, 150]
    });

    doc.setFont('courier', 'normal');
    doc.setFontSize(8);

    let y = 10;
    const xLeft = 5;
    const width = 70; // 80mm - 10mm margins
    const lineCharWidth = 38;

    const centerText = (text: string): string => {
      if (text.length >= lineCharWidth) return text.substring(0, lineCharWidth);
      const leftPadding = Math.floor((lineCharWidth - text.length) / 2);
      return ' '.repeat(leftPadding) + text;
    };

    const padLine = (left: string, right: string): string => {
      const spacesNeeded = lineCharWidth - left.length - right.length;
      if (spacesNeeded <= 0) return left + ' ' + right;
      return left + ' '.repeat(spacesNeeded) + right;
    };

    const line = '='.repeat(lineCharWidth);
    const dashedLine = '-'.repeat(lineCharWidth);

    // Store Name
    doc.setFont('courier', 'bold');
    doc.text(centerText(receipt.storeName || 'TOKO'), xLeft, y);
    y += 4;

    // Store Address
    doc.setFont('courier', 'normal');
    if (receipt.storeAddress) {
      const addressLines = doc.splitTextToSize(receipt.storeAddress, width);
      addressLines.forEach((l: string) => {
        doc.text(centerText(l), xLeft, y);
        y += 4;
      });
    }

    doc.text(line, xLeft, y);
    y += 4;

    // Receipt details
    doc.text(`No. Transaksi : ${receipt.saleNumber}`, xLeft, y);
    y += 4;

    let dateStr = '';
    try {
      dateStr = new Date(receipt.saleDate).toLocaleString('id-ID', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      dateStr = receipt.saleDate;
    }
    doc.text(`Tanggal       : ${dateStr}`, xLeft, y);
    y += 4;
    doc.text(`Kasir         : ${receipt.cashierName || '-'}`, xLeft, y);
    y += 4;

    doc.text(line, xLeft, y);
    y += 4;

    // Items
    if (receipt.items && receipt.items.length > 0) {
      receipt.items.forEach((item: any) => {
        const nameLines = doc.splitTextToSize(item.name, width);
        nameLines.forEach((l: string) => {
          doc.text(l, xLeft, y);
          y += 4;
        });

        const qtyPrice = `  ${item.quantity} x ${this.formatCurrencyRaw(item.unitPrice)}`;
        const subtotal = this.formatCurrencyRaw(item.subtotal);
        doc.text(padLine(qtyPrice, subtotal), xLeft, y);
        y += 4;
      });
    }

    doc.text(dashedLine, xLeft, y);
    y += 4;

    // Totals
    doc.setFont('courier', 'bold');
    doc.text(padLine('Total', this.formatCurrencyRaw(receipt.totalAmount)), xLeft, y);
    y += 4;
    doc.setFont('courier', 'normal');
    doc.text(padLine('Tunai Dibayar', this.formatCurrencyRaw(receipt.amountPaid)), xLeft, y);
    y += 4;
    doc.text(padLine('Kembalian', this.formatCurrencyRaw(receipt.changeAmount)), xLeft, y);
    y += 4;

    doc.text(line, xLeft, y);
    y += 4;

    doc.text(centerText('Terima Kasih'), xLeft, y);

    doc.save(`Struk-${receipt.saleNumber}.pdf`);
  }

  private centerText(text: string, width: number): string {
    if (text.length >= width) return text.substring(0, width);
    const leftPadding = Math.floor((width - text.length) / 2);
    return ' '.repeat(leftPadding) + text;
  }

  private padLine(left: string, right: string, width: number): string {
    const spacesNeeded = width - left.length - right.length;
    if (spacesNeeded <= 0) return left + ' ' + right;
    return left + ' '.repeat(spacesNeeded) + right;
  }

  private formatCurrencyRaw(val: number): string {
    return 'Rp ' + (val || 0).toLocaleString('id-ID');
  }
}
