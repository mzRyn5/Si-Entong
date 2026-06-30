import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from './api-client.service';
import { PaginatedResponse } from '../models/pagination.model';

export interface AuditLogQueryParams {
  page?: number;
  pageSize?: number;
  userId?: string;
  action?: string;
  module?: string;
  fromDate?: string;
  toDate?: string;
}

export interface AuditLogItem {
  id: string;
  userId: string;
  userName: string;
  action: string;
  module: string;
  entityName?: string;
  entityId?: string;
  oldValues?: string;
  newValues?: string;
  ipAddress?: string;
  userAgent?: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private readonly basePath = '/api/v1/audit-logs';

  constructor(private readonly api: ApiClientService) {}

  getAll(params?: AuditLogQueryParams): Observable<PaginatedResponse<AuditLogItem>> {
    return this.api.get<PaginatedResponse<AuditLogItem>>(this.basePath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.userId !== undefined && { userId: params.userId }),
      ...(params?.action !== undefined && { action: params.action }),
      ...(params?.module !== undefined && { module: params.module }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }
}
