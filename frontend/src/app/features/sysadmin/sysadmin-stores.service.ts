import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';

export interface StoreProfileDto {
  id: string;
  name: string;
  address: string;
  phone: string;
  currency: string;
  timezone: string;
  logoUrl?: string;
  receiptFooter?: string;
  isActive: boolean;
  createdAt: string;
  ownerName: string;
}

@Injectable({ providedIn: 'root' })
export class SysadminStoresService {
  constructor(private readonly apiClient: ApiClientService) {}

  getStores(): Observable<StoreProfileDto[]> {
    return this.apiClient.get<any>('/sysadmin/stores').pipe(
      map(res => {
        if (Array.isArray(res)) {
          return res;
        }
        if (res?.success && Array.isArray(res.data)) {
          return res.data;
        }
        throw new Error(res?.message || 'Gagal memuat daftar toko.');
      })
    );
  }
}
