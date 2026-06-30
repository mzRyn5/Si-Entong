import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import { ProductListItem, ProductDetail } from '../../core/models/master-data.model';
export { ProductListItem, ProductDetail };

export interface ProductQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  isActive?: boolean;
}

export interface LowStockQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

export interface ProductCreateRequest {
  sku: string;
  barcode?: string;
  name: string;
  categoryId?: string;
  unitId: string;
  purchasePrice: number;
  sellingPrice: number;
  currentStock: number;
  lowStockThreshold: number;
  isActive: boolean;
}

export interface ProductUpdateRequest {
  sku?: string;
  barcode?: string;
  name?: string;
  categoryId?: string;
  unitId?: string;
  purchasePrice?: number;
  sellingPrice?: number;
  lowStockThreshold?: number;
  isActive?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProductsService {
  private readonly basePath = '/api/v1/products';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: ProductQueryParams): Observable<PaginatedResponse<ProductListItem>> {
    return this.api.get<PaginatedResponse<ProductListItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.categoryId !== undefined && { categoryId: params.categoryId }),
      ...(params?.isActive !== undefined && { isActive: params.isActive }),
    });
  }

  getById(id: string): Observable<ApiResponse<ProductDetail>> {
    return this.api.get<ApiResponse<ProductDetail>>(`${this.basePath}/${id}`);
  }

  getLowStock(params?: LowStockQueryParams): Observable<PaginatedResponse<ProductListItem>> {
    return this.api.get<PaginatedResponse<ProductListItem>>(`${this.basePath}/low-stock`, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
    });
  }

  create(data: ProductCreateRequest): Observable<ApiResponse<ProductDetail>> {
    return this.api.post<ApiResponse<ProductDetail>>(this.basePath, data);
  }

  update(id: string, data: ProductUpdateRequest): Observable<ApiResponse<ProductDetail>> {
    return this.api.put<ApiResponse<ProductDetail>>(`${this.basePath}/${id}`, data);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }
}
