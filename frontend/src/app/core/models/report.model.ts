export interface DashboardSummary {
  totalSales: number;
  todayTransactionCount: number;
  totalPurchases: number;
  lowStockCount: number;
  outOfStockCount: number;
  expenses: number;
  grossProfit: number;
  paymentMethods?: {
    cash: number;
    transfer: number;
    qris: number;
  };
  recentActivities?: Array<{
    type: string;
    description: string;
    timestamp: string;
    userName?: string;
  }>;
}

export interface DailySalesItem {
  date: string;
  transactionCount: number;
  totalSalesAmount: number;
  totalDiscountAmount: number;
  netSalesAmount: number;
}

export interface ProductSalesItem {
  productId: string;
  productName: string;
  quantitySold: number;
  grossSalesAmount: number;
  estimatedCostAmount: number;
  estimatedGrossProfitAmount: number;
}

export interface StockValuationItem {
  productId: string;
  productName: string;
  currentStock: number;
  purchasePrice: number;
  stockValue: number;
}

export interface BasicProfitData {
  fromDate: string;
  toDate: string;
  netSalesAmount: number;
  estimatedCostOfGoodsSold: number;
  estimatedGrossProfit: number;
  expenseAmount: number;
  estimatedNetProfit: number;
}
