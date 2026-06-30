import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import { SupplierItem } from '../../core/models/master-data.model';
export { SupplierItem };

export interface SupplierQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface SupplierCreateRequest {
  name: string;
  phone?: string;
  address?: string;
  notes?: string;
  isActive: boolean;
}

export interface SupplierUpdateRequest {
  name: string;
  phone?: string;
  address?: string;
  notes?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class SuppliersService {
  private readonly basePath = '/api/v1/suppliers';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: SupplierQueryParams): Observable<PaginatedResponse<SupplierItem>> {
    return this.api.get<PaginatedResponse<SupplierItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.isActive !== undefined && { isActive: params.isActive }),
    });
  }

  getById(id: string): Observable<ApiResponse<SupplierItem>> {
    return this.api.get<ApiResponse<SupplierItem>>(`${this.basePath}/${id}`);
  }

  create(data: SupplierCreateRequest): Observable<ApiResponse<SupplierItem>> {
    return this.api.post<ApiResponse<SupplierItem>>(this.basePath, data);
  }

  update(id: string, data: SupplierUpdateRequest): Observable<ApiResponse<SupplierItem>> {
    return this.api.put<ApiResponse<SupplierItem>>(`${this.basePath}/${id}`, data);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }
}
