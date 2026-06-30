import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import { CustomerItem } from '../../core/models/master-data.model';
export { CustomerItem };

export interface CustomerQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface CustomerCreateRequest {
  name: string;
  phone?: string;
  address?: string;
  notes?: string;
  isActive: boolean;
}

export interface CustomerUpdateRequest {
  name: string;
  phone?: string;
  address?: string;
  notes?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly basePath = '/api/v1/customers';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: CustomerQueryParams): Observable<PaginatedResponse<CustomerItem>> {
    return this.api.get<PaginatedResponse<CustomerItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.isActive !== undefined && { isActive: params.isActive }),
    });
  }

  getById(id: string): Observable<ApiResponse<CustomerItem>> {
    return this.api.get<ApiResponse<CustomerItem>>(`${this.basePath}/${id}`);
  }

  create(data: CustomerCreateRequest): Observable<ApiResponse<CustomerItem>> {
    return this.api.post<ApiResponse<CustomerItem>>(this.basePath, data);
  }

  update(id: string, data: CustomerUpdateRequest): Observable<ApiResponse<CustomerItem>> {
    return this.api.put<ApiResponse<CustomerItem>>(`${this.basePath}/${id}`, data);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }
}
