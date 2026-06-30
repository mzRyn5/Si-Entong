import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PagedResponse } from '../../core/models/api-response.model';

export interface UserItem {
  id: string;
  name: string;
  username: string;
  role: string; // 'sysadmin' | 'owner' | 'admin'
  isActive: boolean;
  createdAt: string;
  storeId?: string | null;
}

export interface UserCreateRequest {
  name: string;
  username: string;
  password?: string;
  role: string;
  isActive: boolean;
  storeId?: string | null;
}

export interface UserUpdateRequest {
  name: string;
  role: string;
  isActive: boolean;
  storeId?: string | null;
}

export interface ResetPasswordRequest {
  newPassword?: string;
}

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  constructor(private readonly apiClient: ApiClientService) {}

  getAll(params?: {
    page?: number;
    pageSize?: number;
    search?: string;
    role?: string;
    isActive?: boolean;
  }): Observable<PagedResponse<UserItem>> {
    return this.apiClient.get<PagedResponse<UserItem>>('/users', params as any);
  }

  getById(id: string): Observable<ApiResponse<UserItem>> {
    return this.apiClient.get<ApiResponse<UserItem>>(`/users/${id}`);
  }

  create(request: UserCreateRequest): Observable<ApiResponse<UserItem>> {
    return this.apiClient.post<ApiResponse<UserItem>>('/users', request);
  }

  update(id: string, request: UserUpdateRequest): Observable<ApiResponse<UserItem>> {
    return this.apiClient.put<ApiResponse<UserItem>>(`/users/${id}`, request);
  }

  delete(id: string): Observable<ApiResponse<void>> {
    return this.apiClient.delete<ApiResponse<void>>(`/users/${id}`);
  }

  resetPassword(id: string, request: ResetPasswordRequest): Observable<ApiResponse<void>> {
    return this.apiClient.post<ApiResponse<void>>(`/users/${id}/reset-password`, request);
  }

  // Helper for SysAdmin to select stores when adding owners/admins
  getStoresDropdown(): Observable<ApiResponse<any[]>> {
    return this.apiClient.get<ApiResponse<any[]>>('/sysadmin/stores');
  }
}
