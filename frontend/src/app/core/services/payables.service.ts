import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from './api-client.service';
import { ApiResponse, PaginatedResponse } from '../models/pagination.model';

export interface PayableListItem {
  id: string;
  payableNumber: string;
  purchaseNumber: string;
  supplierName: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  dueDate: string;
  paymentStatus: string;
  createdAt: string;
}

export interface PayablePaymentItem {
  id: string;
  payableId: string;
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  notes?: string;
}

export interface PayableDetail {
  id: string;
  payableNumber: string;
  purchaseId: string;
  purchaseNumber: string;
  supplierId: string;
  supplierName: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  dueDate: string;
  paymentStatus: string;
  notes?: string;
  createdAt: string;
  payments: PayablePaymentItem[];
}

export interface PayableQueryParams {
  page?: number;
  pageSize?: number;
  supplierId?: string;
  status?: string;
}

export interface RecordPayablePaymentRequest {
  paymentDate: string;
  amount: number;
  paymentMethod: string;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class PayablesService {
  private readonly basePath = '/api/v1/payables';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: PayableQueryParams): Observable<PaginatedResponse<PayableListItem>> {
    return this.api.get<PaginatedResponse<PayableListItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.supplierId !== undefined && { supplierId: params.supplierId }),
      ...(params?.status !== undefined && { status: params.status }),
    });
  }

  getById(id: string): Observable<ApiResponse<PayableDetail>> {
    return this.api.get<ApiResponse<PayableDetail>>(`${this.basePath}/${id}`);
  }

  recordPayment(id: string, data: RecordPayablePaymentRequest): Observable<ApiResponse<PayableDetail>> {
    return this.api.post<ApiResponse<PayableDetail>>(`${this.basePath}/${id}/payments`, data);
  }

  cancelPayment(id: string, paymentId: string): Observable<ApiResponse<PayableDetail>> {
    return this.api.delete<ApiResponse<PayableDetail>>(`${this.basePath}/${id}/payments/${paymentId}`);
  }

  deletePayable(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }
}
