import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';
import { UnitItem } from '../../core/models/master-data.model';
export { UnitItem };

export interface UnitQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
}

export interface UnitCreateRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface UnitUpdateRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class UnitsService {
  private readonly basePath = '/api/v1/units';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: UnitQueryParams): Observable<PaginatedResponse<UnitItem>> {
    return this.api.get<PaginatedResponse<UnitItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
      ...(params?.isActive !== undefined && { isActive: params.isActive }),
    });
  }

  getById(id: string): Observable<ApiResponse<UnitItem>> {
    return this.api.get<ApiResponse<UnitItem>>(`${this.basePath}/${id}`);
  }

  create(data: UnitCreateRequest): Observable<ApiResponse<UnitItem>> {
    return this.api.post<ApiResponse<UnitItem>>(this.basePath, data);
  }

  update(id: string, data: UnitUpdateRequest): Observable<ApiResponse<UnitItem>> {
    return this.api.put<ApiResponse<UnitItem>>(`${this.basePath}/${id}`, data);
  }

  delete(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.basePath}/${id}`);
  }
}
