import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import { CategoryItem } from '../../core/models/master-data.model';
export { CategoryItem };

export interface CategoryQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface CategoryCreateRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface CategoryUpdateRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class CategoriesService {
  private readonly basePath = '/api/v1/categories';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: CategoryQueryParams): Observable<PaginatedResponse<CategoryItem>> {
    return this.api.get<PaginatedResponse<CategoryItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.isActive !== undefined && { isActive: params.isActive }),
    });
  }

  getById(id: string): Observable<ApiResponse<CategoryItem>> {
    return this.api.get<ApiResponse<CategoryItem>>(`${this.basePath}/${id}`);
  }

  create(data: CategoryCreateRequest): Observable<ApiResponse<CategoryItem>> {
    return this.api.post<ApiResponse<CategoryItem>>(this.basePath, data);
  }

  update(id: string, data: CategoryUpdateRequest): Observable<ApiResponse<CategoryItem>> {
    return this.api.put<ApiResponse<CategoryItem>>(`${this.basePath}/${id}`, data);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }
}
