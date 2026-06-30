export interface StoreProfile {
  id?: string;
  name: string;
  address?: string;
  phone?: string;
  currency?: string;
  timezone?: string;
  logoUrl?: string;
  receiptFooter?: string;
}

export interface StoreSettings {
  id?: string;
  allowNegativeStock: boolean;
  requireCashSessionForSales: boolean;
  defaultLowStockThreshold: number;
  enableBarcode: boolean;
  enablePurchasePriceTracking: boolean;
  defaultPaymentMethod: string;
}
