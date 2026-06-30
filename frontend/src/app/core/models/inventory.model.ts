export interface StockSummary {
  productId: string;
  sku: string;
  name: string;
  productName: string;
  categoryName: string;
  unitName: string;
  currentStock: number;
  lowStockThreshold: number;
  stockValue: number;
  sellingPrice: number;
}

export interface StockMovement {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  movementDate: string;
  movementType: string;
  quantityBefore: number;
  quantityChange: number;
  quantityAfter: number;
  referenceType?: string;
  referenceId?: string;
  referenceNumber?: string;
  notes?: string;
}

export type StockSummaryItem = StockSummary;
export type StockMovementItem = StockMovement;
