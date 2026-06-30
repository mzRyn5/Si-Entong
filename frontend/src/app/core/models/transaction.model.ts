export interface SaleItem {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface PurchaseItem {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface CashSession {
  id: string;
  cashierId: string;
  cashierName: string;
  openedAt: string;
  openingCashAmount: number;
  cashSalesAmount: number;
  cashInAmount: number;
  cashOutAmount: number;
  expectedCashAmount: number;
  actualCashAmount?: number;
  differenceAmount?: number;
  closedAt?: string;
  status: string;
}

export interface SaleListItem {
  id: string;
  saleNumber: string;
  saleDate: string;
  cashierName: string;
  customerName?: string;
  totalAmount: number;
  paymentMethod: string;
  paymentStatus: string;
  status: string;
}

export interface SaleItemResponse {
  id?: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discountAmount: number;
  subtotal: number;
}

export interface SaleDetail {
  id: string;
  saleNumber: string;
  saleDate: string;
  cashier?: { id: string; name: string };
  customer?: { id: string; name: string };
  items: SaleItemResponse[];
  subtotal: number;
  discountAmount: number;
  taxAmount: number;
  totalAmount: number;
  paymentMethod: string;
  amountPaid: number;
  changeAmount: number;
  paymentStatus: string;
  status: string;
  notes?: string;
}

export interface SaleReceiptItemResponse {
  name: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface ReceiptData {
  storeName: string;
  storeAddress: string;
  saleNumber: string;
  saleDate: string;
  cashierName: string;
  items: SaleReceiptItemResponse[];
  totalAmount: number;
  amountPaid: number;
  changeAmount: number;
}

export interface PurchaseListItem {
  id: string;
  purchaseNumber: string;
  purchaseDate: string;
  supplierName?: string;
  totalAmount: number;
  paymentStatus: string;
  status: string;
}

export interface PurchaseItemResponse {
  id?: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface PurchaseDetail {
  id: string;
  purchaseNumber: string;
  purchaseDate: string;
  supplier: { id: string; name: string };
  paymentMethod: string;
  paymentStatus: string;
  status: string;
  items: PurchaseItemResponse[];
  subtotal: number;
  discountAmount: number;
  totalAmount: number;
  notes?: string;
}
