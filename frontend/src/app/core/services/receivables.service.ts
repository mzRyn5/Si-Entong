import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from './api-client.service';
import { ApiResponse, PaginatedResponse } from '../models/pagination.model';

export interface ReceivableListItem {
  id: string;
  receivableNumber: string;
  saleNumber: string;
  customerName: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  dueDate: string;
  paymentStatus: string;
  createdAt: string;
}

export interface ReceivablePaymentItem {
  id: string;
  receivableId: string;
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  notes?: string;
}

export interface ReceivableDetail {
  id: string;
  receivableNumber: string;
  saleId: string;
  saleNumber: string;
  customerId: string;
  customerName: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  dueDate: string;
  paymentStatus: string;
  notes?: string;
  createdAt: string;
  payments: ReceivablePaymentItem[];
}

export interface ReceivableQueryParams {
  page?: number;
  pageSize?: number;
  customerId?: string;
  status?: string;
}

export interface RecordReceivablePaymentRequest {
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class ReceivablesService {
  private readonly basePath = '/api/v1/receivables';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: ReceivableQueryParams): Observable<PaginatedResponse<ReceivableListItem>> {
    return this.api.get<PaginatedResponse<ReceivableListItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.customerId !== undefined && { customerId: params.customerId }),
      ...(params?.status !== undefined && { status: params.status }),
    });
  }

  getById(id: string): Observable<ApiResponse<ReceivableDetail>> {
    return this.api.get<ApiResponse<ReceivableDetail>>(`${this.basePath}/${id}`);
  }

  recordPayment(id: string, data: RecordReceivablePaymentRequest): Observable<ApiResponse<ReceivableDetail>> {
    return this.api.post<ApiResponse<ReceivableDetail>>(`${this.basePath}/${id}/payments`, data);
  }

  deleteReceivable(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }
}
