import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PagedResponse } from '../../core/models/api-response.model';
import {
  SaleListItem,
  SaleDetail,
  CashSession,
  ReceiptData,
} from '../../core/models/transaction.model';
export { SaleListItem, SaleDetail, CashSession, ReceiptData };

export interface SaleQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  cashierId?: string;
  customerId?: string;
  paymentMethod?: string;
  paymentStatus?: string;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export interface SaleItemRequest {
  productId: string;
  quantity: number;
  unitPrice: number;
  discountAmount?: number;
}

export interface SaleCreateRequest {
  saleDate: string;
  customerId?: string;
  cashSessionId?: string;
  items: SaleItemRequest[];
  discountAmount?: number;
  taxAmount?: number;
  paymentMethod: string;
  paymentStatus: string;
  amountPaid: number;
  notes?: string;
  dueDate?: string;
}

export interface SaleVoidRequest {
  reason: string;
}

export interface CashSessionQueryParams {
  page?: number;
  pageSize?: number;
  status?: string;
  fromDate?: string;
  toDate?: string;
}

export interface OpenCashSessionRequest {
  openingCashAmount: number;
  notes?: string;
}

export interface CloseCashSessionRequest {
  actualCashAmount: number;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly basePath = '/api/v1/sales';
  private readonly cashSessionsPath = '/api/v1/cash-sessions';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: SaleQueryParams): Observable<PagedResponse<SaleListItem>> {
    return this.api.get<PagedResponse<SaleListItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.cashierId !== undefined && { cashierId: params.cashierId }),
      ...(params?.customerId !== undefined && { customerId: params.customerId }),
      ...(params?.paymentMethod !== undefined && { paymentMethod: params.paymentMethod }),
      ...(params?.paymentStatus !== undefined && { paymentStatus: params.paymentStatus }),
      ...(params?.status !== undefined && { status: params.status }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getById(id: string): Observable<ApiResponse<SaleDetail>> {
    return this.api.get<ApiResponse<SaleDetail>>(`${this.basePath}/${id}`);
  }

  getReceipt(id: string): Observable<ApiResponse<ReceiptData>> {
    return this.api.get<ApiResponse<ReceiptData>>(`${this.basePath}/${id}/receipt`);
  }

  create(data: SaleCreateRequest): Observable<ApiResponse<SaleDetail>> {
    return this.api.post<ApiResponse<SaleDetail>>(this.basePath, data);
  }

  void(id: string, reason: string): Observable<ApiResponse<SaleDetail>> {
    return this.api.post<ApiResponse<SaleDetail>>(`${this.basePath}/${id}/void`, { reason } as SaleVoidRequest);
  }

  deleteSale(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }

  getActiveCashSession(): Observable<ApiResponse<CashSession>> {
    return this.api.get<ApiResponse<CashSession>>(`${this.cashSessionsPath}/active`);
  }

  getCashSessions(params?: CashSessionQueryParams): Observable<PagedResponse<CashSession>> {
    return this.api.get<PagedResponse<CashSession>>(this.cashSessionsPath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.status !== undefined && { status: params.status }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  openCashSession(data: OpenCashSessionRequest): Observable<ApiResponse<CashSession>> {
    return this.api.post<ApiResponse<CashSession>>(`${this.cashSessionsPath}/open`, data);
  }

  closeCashSession(id: string, data: CloseCashSessionRequest): Observable<ApiResponse<CashSession>> {
    return this.api.post<ApiResponse<CashSession>>(`${this.cashSessionsPath}/${id}/close`, data);
  }

  // Cash Movements APIs
  private readonly movementsPath = '/api/v1/cash-movements';

  getCashMovements(params?: CashMovementQueryParams): Observable<PagedResponse<CashMovementListItem>> {
    return this.api.get<PagedResponse<CashMovementListItem>>(this.movementsPath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.cashSessionId !== undefined && { cashSessionId: params.cashSessionId }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
      ...(params?.type !== undefined && { type: params.type }),
    });
  }

  createCashMovement(data: CashMovementCreateRequest): Observable<ApiResponse<CashMovement>> {
    return this.api.post<ApiResponse<CashMovement>>(this.movementsPath, data);
  }

  // Sales Returns APIs
  private readonly salesReturnsPath = '/api/v1/returns/sales';

  getSalesReturns(params?: SalesReturnQueryParams): Observable<PagedResponse<SalesReturnListItem>> {
    return this.api.get<PagedResponse<SalesReturnListItem>>(this.salesReturnsPath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.status !== undefined && { status: params.status }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getSalesReturnById(id: string): Observable<ApiResponse<SalesReturnDetail>> {
    return this.api.get<ApiResponse<SalesReturnDetail>>(`${this.salesReturnsPath}/${id}`);
  }

  createSalesReturn(data: SalesReturnCreateRequest): Observable<ApiResponse<SalesReturnDetail>> {
    return this.api.post<ApiResponse<SalesReturnDetail>>(this.salesReturnsPath, data);
  }
}

export interface CashMovementQueryParams {
  page?: number;
  pageSize?: number;
  cashSessionId?: string;
  fromDate?: string;
  toDate?: string;
  type?: string;
}

export interface CashMovementCreateRequest {
  cashSessionId: string;
  movementDate: string;
  type: string;
  amount: number;
  category: string;
  notes?: string;
}

export interface CashMovementListItem {
  id: string;
  cashSessionId: string;
  movementDate: string;
  type: string;
  amount: number;
  category: string;
  status: string;
}

export interface CashMovement {
  id: string;
  cashSessionId: string;
  movementDate: string;
  type: string;
  amount: number;
  category: string;
  notes?: string;
  status: string;
}

export interface SalesReturnItemRequest {
  productId: string;
  quantity: number;
}

export interface SalesReturnCreateRequest {
  saleId: string;
  reason: string;
  notes?: string;
  items: SalesReturnItemRequest[];
}

export interface SalesReturnListItem {
  id: string;
  returnNumber: string;
  saleNumber: string;
  returnDate: string;
  totalRefundAmount: number;
  status: string;
}

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

export interface SalesReturnQueryParams {
  page?: number;
  pageSize?: number;
  fromDate?: string;
  toDate?: string;
  status?: string;
}
