export interface PaginationQuery { page?: number; pageSize?: number; search?: string; }
export interface PageResult<T> { items: T[]; page: number; pageSize: number; totalItems: number; totalPages: number; }
import { ApiResponse, PagedResponse } from './api-response.model';
export { ApiResponse };
export type PaginatedResponse<T> = PagedResponse<T>;
