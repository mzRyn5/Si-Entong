import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse, PaginatedResponse } from '../../core/models/pagination.model';

export interface ExpenseCategoryItem {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface ExpenseCategoryCreateRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface ExpenseCategoryUpdateRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface ExpenseListItem {
  id: string;
  expenseNumber: string;
  expenseDate: string;
  categoryName: string;
  amount: number;
  paymentMethod: string;
  status: string;
}

export interface ExpenseDetail {
  id: string;
  expenseNumber: string;
  expenseDate: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  paymentMethod: string;
  status: string;
  description: string;
  voidedAt?: string;
  voidReason?: string;
}

export interface ExpenseCreateRequest {
  expenseDate: string;
  expenseCategoryId: string;
  amount: number;
  description: string;
  paymentMethod: string;
}

export interface ExpenseQueryParams {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  fromDate?: string;
  toDate?: string;
}

export interface ExpenseCategoryQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
}

@Injectable({ providedIn: 'root' })
export class ExpensesService {
  private readonly expensesPath = '/api/v1/expenses';
  private readonly categoriesPath = '/api/v1/expenses/categories';

  constructor(private readonly api: ApiClientService) {}

  // Expense Categories
  getCategories(params?: ExpenseCategoryQueryParams): Observable<PaginatedResponse<ExpenseCategoryItem>> {
    return this.api.get<PaginatedResponse<ExpenseCategoryItem>>(this.categoriesPath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.search !== undefined && { search: params.search }),
    });
  }

  getCategoriesDropdown(): Observable<ApiResponse<ExpenseCategoryItem[]>> {
    return this.api.get<ApiResponse<ExpenseCategoryItem[]>>(`${this.categoriesPath}/dropdown`);
  }

  getCategoryById(id: string): Observable<ApiResponse<ExpenseCategoryItem>> {
    return this.api.get<ApiResponse<ExpenseCategoryItem>>(`${this.categoriesPath}/${id}`);
  }

  createCategory(data: ExpenseCategoryCreateRequest): Observable<ApiResponse<ExpenseCategoryItem>> {
    return this.api.post<ApiResponse<ExpenseCategoryItem>>(this.categoriesPath, data);
  }

  updateCategory(id: string, data: ExpenseCategoryUpdateRequest): Observable<ApiResponse<ExpenseCategoryItem>> {
    return this.api.put<ApiResponse<ExpenseCategoryItem>>(`${this.categoriesPath}/${id}`, data);
  }

  deleteCategory(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.categoriesPath}/${id}`);
  }

  // Expenses
  getExpenses(params?: ExpenseQueryParams): Observable<PaginatedResponse<ExpenseListItem>> {
    return this.api.get<PaginatedResponse<ExpenseListItem>>(this.expensesPath, {
      ...(params?.page !== undefined && { page: params.page }),
      ...(params?.pageSize !== undefined && { pageSize: params.pageSize }),
      ...(params?.categoryId !== undefined && { categoryId: params.categoryId }),
      ...(params?.fromDate !== undefined && { fromDate: params.fromDate }),
      ...(params?.toDate !== undefined && { toDate: params.toDate }),
    });
  }

  getExpenseById(id: string): Observable<ApiResponse<ExpenseDetail>> {
    return this.api.get<ApiResponse<ExpenseDetail>>(`${this.expensesPath}/${id}`);
  }

  createExpense(data: ExpenseCreateRequest): Observable<ApiResponse<ExpenseDetail>> {
    return this.api.post<ApiResponse<ExpenseDetail>>(this.expensesPath, data);
  }

  voidExpense(id: string, reason: string): Observable<ApiResponse<ExpenseDetail>> {
    return this.api.post<ApiResponse<ExpenseDetail>>(`${this.expensesPath}/${id}/void`, { reason });
  }

  deleteExpense(id: string): Observable<ApiResponse<null>> {
    return this.api.delete<ApiResponse<null>>(`${this.expensesPath}/${id}`);
  }
}
