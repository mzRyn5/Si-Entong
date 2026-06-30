import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';

type QueryValue = string | number | boolean | null | undefined;

@Injectable({ providedIn: 'root' })
export class ApiClientService {
  private readonly baseUrl = environment.apiUrl.replace(/\/$/, '');

  constructor(private readonly http: HttpClient) {}

  get<T>(path: string, query?: Record<string, QueryValue>): Observable<T> {
    return this.http.get<T>(this.buildUrl(path), { params: this.buildParams(query) });
  }

  post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<T>(this.buildUrl(path), body);
  }

  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<T>(this.buildUrl(path), body);
  }

  delete<T>(path: string): Observable<T> {
    return this.http.delete<T>(this.buildUrl(path));
  }

  private buildUrl(path: string): string {
    const cleanPath = path.replace(/^\//, '');
    const base = this.baseUrl;
    
    if (cleanPath.startsWith('api/v1/')) {
      if (base.endsWith('/api/v1')) {
        return `${base}/${cleanPath.substring(7)}`;
      } else if (base.endsWith('/api')) {
        return `${base}/${cleanPath.substring(4)}`;
      }
    }
    
    return `${base}/${cleanPath}`;
  }

  private buildParams(query?: Record<string, QueryValue>): HttpParams {
    let params = new HttpParams();

    Object.entries(query ?? {}).forEach(([key, value]) => {
      if (value !== null && value !== undefined && value !== '') {
        params = params.set(key, String(value));
      }
    });

    return params;
  }
}
