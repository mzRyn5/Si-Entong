export interface ApiResponse<T> { success: boolean; message: string; data: T; errors?: unknown; }
export interface MetaData { page: number; pageSize: number; totalItems: number; totalPages: number; }
export interface PagedResponse<T> { success: boolean; message: string; data: T[]; meta: MetaData; }
