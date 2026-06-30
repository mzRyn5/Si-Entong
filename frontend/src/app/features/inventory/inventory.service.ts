import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import { StockSummaryItem, StockMovementItem } from '../../core/models/inventory.model';
export { StockSummaryItem, StockMovementItem };

export interface StockSummaryQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  lowStockOnly?: boolean;
}

export interface StockMovementQueryParams {
  page?: number;
  pageSize?: number;
  productId?: string;
  movementType?: string;
  fromDate?: string;
  toDate?: string;
}

export interface StockAdjustmentQueryParams {
  page?: number;
  pageSize?: number;
  productId?: string;
  fromDate?: string;
  toDate?: string;
}

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly basePath = '/api/v1/inventory';

  constructor(private readonly api: ApiClientService) {}

  getStockSummary(params?: StockSummaryQueryParams): Observable<PaginatedResponse<StockSummaryItem>> {
    return this.api.get<PaginatedResponse<StockSummaryItem>>(`${this.basePath}/stock-summary`, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.categoryId !== undefined && { categoryId: params.categoryId }),
      ...(params?.lowStockOnly !== undefined && { lowStockOnly: params.lowStockOnly }),
    });
  }

  getStockMovements(params?: StockMovementQueryParams): Observable<PaginatedResponse<StockMovementItem>> {
    return this.api.get<PaginatedResponse<StockMovementItem>>(`${this.basePath}/stock-movements`, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.productId !== undefined && { productId: params.productId }),
      ...(params?.movementType !== undefined && { movementType: params.movementType }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getStockAdjustments(params?: StockAdjustmentQueryParams): Observable<PaginatedResponse<StockMovementItem>> {
    return this.api.get<PaginatedResponse<StockMovementItem>>(`${this.basePath}/stock-adjustments`, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.productId !== undefined && { productId: params.productId }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  // Stock Opname APIs
  private readonly opnamesPath = '/api/v1/stock-opnames';

  getStockOpnames(params?: StockOpnameQueryParams): Observable<PaginatedResponse<StockOpnameListItem>> {
    return this.api.get<PaginatedResponse<StockOpnameListItem>>(this.opnamesPath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.status !== undefined && { status: params.status }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getStockOpnameById(id: string): Observable<ApiResponse<StockOpnameDetail>> {
    return this.api.get<ApiResponse<StockOpnameDetail>>(`${this.opnamesPath}/${id}`);
  }

  createStockOpname(data: StockOpnameCreateRequest): Observable<ApiResponse<StockOpnameDetail>> {
    return this.api.post<ApiResponse<StockOpnameDetail>>(this.opnamesPath, data);
  }

  updateStockOpname(id: string, data: StockOpnameUpdateRequest): Observable<ApiResponse<StockOpnameDetail>> {
    return this.api.put<ApiResponse<StockOpnameDetail>>(`${this.opnamesPath}/${id}`, data);
  }

  postStockOpname(id: string): Observable<ApiResponse<StockOpnameDetail>> {
    return this.api.post<ApiResponse<StockOpnameDetail>>(`${this.opnamesPath}/${id}/post`, {});
  }

  cancelStockOpname(id: string, reason: string): Observable<ApiResponse<StockOpnameDetail>> {
    return this.api.post<ApiResponse<StockOpnameDetail>>(`${this.opnamesPath}/${id}/cancel`, { reason });
  }
}

export interface StockOpnameItemRequest {
  productId: string;
  systemStock: number;
  physicalStock: number;
  notes?: string;
}

export interface StockOpnameCreateRequest {
  opnameDate: string;
  notes?: string;
  items: StockOpnameItemRequest[];
}

export interface StockOpnameUpdateRequest {
  opnameDate: string;
  notes?: string;
  items: StockOpnameItemRequest[];
}

export interface StockOpnameItem {
  productId: string;
  productName: string;
  systemStock: number;
  physicalStock: number;
  difference: number;
  notes?: string;
}

export interface StockOpnameDetail {
  id: string;
  opnameNumber: string;
  opnameDate: string;
  notes?: string;
  status: string;
  items: StockOpnameItem[];
  createdAt: string;
}

export interface StockOpnameListItem {
  id: string;
  opnameNumber: string;
  opnameDate: string;
  status: string;
  totalItems: number;
}

export interface StockOpnameQueryParams {
  page?: number;
  pageSize?: number;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

