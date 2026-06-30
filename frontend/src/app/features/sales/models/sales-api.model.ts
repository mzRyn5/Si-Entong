export interface SalesReturnItem {
  saleItemId: string;
  productId: string;
  productName: string;
  quantity: number;
  refundAmount: number;
  unitPrice?: number;
}

export interface SalesReturnDetail {
  id: string;
  returnNumber: string;
  saleId: string;
  saleNumber: string;
  returnDate: string;
  reason: string;
  totalRefundAmount: number;
  status: string;
  notes?: string;
  items: SalesReturnItem[];
}

export interface SalesReturnListItem {
  id: string;
  returnNumber: string;
  saleNumber: string;
  returnDate: string;
  totalRefundAmount: number;
  status: string;
}
