import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import {
  DashboardSummary,
  DailySalesItem,
  ProductSalesItem,
  StockValuationItem,
  BasicProfitData,
} from '../../core/models/report.model';
import { environment } from '../../../environments/environment';

export interface DailySalesQueryParams {
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface ProductSalesQueryParams {
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
}

export interface StockValuationQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
}

export interface BasicProfitQueryParams {
  fromDate?: string;
  toDate?: string;
}

@Injectable({ providedIn: 'root' })
export class ReportsService {
  private readonly basePath = '/api/v1/reports';
  private readonly baseUrl: string;

  constructor(
    private readonly api: ApiClientService,
    private readonly http: HttpClient
  ) {
    this.baseUrl = environment.apiUrl.replace(/\/$/, '');
  }

  downloadReport(type: string, params?: Record<string, any>): Observable<Blob> {
    let httpParams = new HttpParams();
    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== null && value !== undefined && value !== '') {
          httpParams = httpParams.set(key, String(value));
        }
      });
    }
    const path = `/api/v1/reports/${type}/export`;
    const cleanPath = path.replace(/^\//, '');
    const url = this.baseUrl.endsWith('/api/v1')
      ? `${this.baseUrl}/${cleanPath.substring(7)}`
      : `${this.baseUrl}/${cleanPath}`;
    return this.http.get(url, { params: httpParams, responseType: 'blob' });
  }

  getDashboardSummary(date?: string): Observable<ApiResponse<DashboardSummary>> {
    return this.api.get<ApiResponse<any>>(
      `${this.basePath}/dashboard-summary`,
      date ? { date } : undefined,
    ).pipe(
      map(res => {
        if (res.success && res.data) {
          const d = res.data;
          
          // Map payment method summaries list to object structure
          const paymentMethodsObj = { cash: 0, transfer: 0, qris: 0 };
          if (d.paymentMethodSummaries && Array.isArray(d.paymentMethodSummaries)) {
            d.paymentMethodSummaries.forEach((p: any) => {
              const pm = (p.paymentMethod || '').toLowerCase();
              if (pm.includes('cash') || pm.includes('tunai')) {
                paymentMethodsObj.cash = p.totalAmount || 0;
              } else if (pm.includes('transfer') || pm.includes('bank')) {
                paymentMethodsObj.transfer = p.totalAmount || 0;
              } else if (pm.includes('qris') || pm.includes('qr')) {
                paymentMethodsObj.qris = p.totalAmount || 0;
              }
            });
          }

          // Map latest activities to recentActivities structure
          const recentActivitiesArr = (d.latestActivities || []).map((a: any) => ({
            type: a.module || 'Sistem',
            description: `${a.action} ${a.entityName || ''}`.trim(),
            timestamp: a.createdAt,
            userName: a.userName
          }));

          const mappedData: DashboardSummary = {
            totalSales: d.totalSalesAmount || 0,
            todayTransactionCount: d.totalSalesTransactions || 0,
            totalPurchases: d.totalPurchaseAmount || 0,
            lowStockCount: d.lowStockProductCount || 0,
            outOfStockCount: d.outOfStockProductCount || 0,
            expenses: d.totalExpenseAmount || 0,
            grossProfit: d.grossProfitAmount || 0,
            paymentMethods: paymentMethodsObj,
            recentActivities: recentActivitiesArr
          };
          
          return { ...res, data: mappedData };
        }
        return res;
      })
    );
  }

  getDailySales(params?: DailySalesQueryParams): Observable<PaginatedResponse<DailySalesItem>> {
    return this.api.get<PaginatedResponse<DailySalesItem>>(`${this.basePath}/daily-sales`, {
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
    });
  }

  getProductSales(params?: ProductSalesQueryParams): Observable<PaginatedResponse<ProductSalesItem>> {
    return this.api.get<PaginatedResponse<ProductSalesItem>>(`${this.basePath}/product-sales`, {
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.categoryId !== undefined && { categoryId: params.categoryId }),
    });
  }

  getStockValuation(params?: StockValuationQueryParams): Observable<PaginatedResponse<StockValuationItem>> {
    return this.api.get<PaginatedResponse<StockValuationItem>>(`${this.basePath}/stock-valuation`, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.categoryId !== undefined && { categoryId: params.categoryId }),
    });
  }

  getBasicProfit(params?: BasicProfitQueryParams): Observable<ApiResponse<BasicProfitData>> {
    return this.api.get<ApiResponse<BasicProfitData>>(`${this.basePath}/basic-profit`, {
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getExportUrl(type: string, params?: Record<string, unknown>): string {
    const path = `/api/v1/reports/${type}/export`;
    const cleanPath = path.replace(/^\//, '');
    const exportPath = this.baseUrl.endsWith('/api/v1')
      ? `${this.baseUrl}/${cleanPath.substring(7)}`
      : `${this.baseUrl}/${cleanPath}`;
    if (!params || Object.keys(params).length === 0) {
      return exportPath;
    }
    const queryString = Object.entries(params)
      .filter(([, value]) => value !== null && value !== undefined && value !== '')
      .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
      .join('&');
    return queryString ? `${exportPath}?${queryString}` : exportPath;
  }
}
