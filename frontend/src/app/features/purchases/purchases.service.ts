import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import { PurchaseListItem, PurchaseDetail } from '../../core/models/transaction.model';
export { PurchaseListItem, PurchaseDetail };

export interface PurchaseQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  supplierId?: string;
  status?: string;
  paymentStatus?: string;
  fromDate?: string;
  toDate?: string;
}

export interface PurchaseItemRequest {
  productId: string;
  quantity: number;
  unitPrice: number;
}

export interface PurchaseCreateRequest {
  supplierId?: string;
  purchaseDate: string;
  items: PurchaseItemRequest[];
  discountAmount?: number;
  amountPaid?: number;
  paymentMethod: string;
  paymentStatus: string;
  notes?: string;
}

export interface PurchaseVoidRequest {
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class PurchasesService {
  private readonly basePath = '/api/v1/purchases';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: PurchaseQueryParams): Observable<PaginatedResponse<PurchaseListItem>> {
    return this.api.get<PaginatedResponse<PurchaseListItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.supplierId !== undefined && { supplierId: params.supplierId }),
      ...(params?.status !== undefined && { status: params.status }),
      ...(params?.paymentStatus !== undefined && { paymentStatus: params.paymentStatus }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getById(id: string): Observable<ApiResponse<PurchaseDetail>> {
    return this.api.get<ApiResponse<PurchaseDetail>>(`${this.basePath}/${id}`);
  }

  create(data: PurchaseCreateRequest): Observable<ApiResponse<PurchaseDetail>> {
    return this.api.post<ApiResponse<PurchaseDetail>>(this.basePath, data);
  }

  void(id: string, reason: string): Observable<ApiResponse<PurchaseDetail>> {
    return this.api.post<ApiResponse<PurchaseDetail>>(`${this.basePath}/${id}/void`, { reason } as PurchaseVoidRequest);
  }

  deletePurchase(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }

  // Purchase Returns APIs
  private readonly purchaseReturnsPath = '/api/v1/returns/purchases';

  getPurchaseReturns(params?: PurchaseReturnQueryParams): Observable<PaginatedResponse<PurchaseReturnListItem>> {
    return this.api.get<PaginatedResponse<PurchaseReturnListItem>>(this.purchaseReturnsPath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.status !== undefined && { status: params.status }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getPurchaseReturnById(id: string): Observable<ApiResponse<PurchaseReturnDetail>> {
    return this.api.get<ApiResponse<PurchaseReturnDetail>>(`${this.purchaseReturnsPath}/${id}`);
  }

  createPurchaseReturn(data: PurchaseReturnCreateRequest): Observable<ApiResponse<PurchaseReturnDetail>> {
    return this.api.post<ApiResponse<PurchaseReturnDetail>>(this.purchaseReturnsPath, data);
  }
}

export interface PurchaseReturnItemRequest {
  productId: string;
  quantity: number;
}

export interface PurchaseReturnCreateRequest {
  purchaseId: string;
  reason: string;
  items: PurchaseReturnItemRequest[];
}

export interface PurchaseReturnListItem {
  id: string;
  returnNumber: string;
  purchaseNumber: string;
  returnDate: string;
  totalAmount: number;
  status: string;
}

export interface PurchaseReturnItem {
  purchaseItemId: string;
  productId: string;
  productName: string;
  quantity: number;
  amount: number;
  unitPrice?: number;
}

export interface PurchaseReturnDetail {
  id: string;
  returnNumber: string;
  purchaseId: string;
  purchaseNumber: string;
  returnDate: string;
  reason: string;
  totalAmount: number;
  status: string;
  notes?: string;
  items: PurchaseReturnItem[];
}

export interface PurchaseReturnQueryParams {
  page?: number;
  pageSize?: number;
  fromDate?: string;
  toDate?: string;
  status?: string;
}

