import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';

import { ApiClientService } from '../../core/services/api-client.service';
import { ApiResponse } from '../../core/models/api-response.model';
import { StoreProfile, StoreSettings } from '../../core/models/store.model';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly storePath = '/api/v1/store';
  private readonly _storeProfile = new BehaviorSubject<StoreProfile | null>(null);
  readonly storeProfile$ = this._storeProfile.asObservable();

  constructor(private readonly api: ApiClientService) {}

  getProfile(): Observable<ApiResponse<StoreProfile>> {
    return this.api.get<ApiResponse<StoreProfile>>(`${this.storePath}/profile`).pipe(
      tap(res => {
        if (res.success && res.data) {
          this._storeProfile.next(res.data);
        }
      })
    );
  }

  updateProfile(data: StoreProfile): Observable<ApiResponse<StoreProfile>> {
    return this.api.put<ApiResponse<StoreProfile>>(`${this.storePath}/profile`, data).pipe(
      tap(res => {
        if (res.success && res.data) {
          this._storeProfile.next(res.data);
        }
      })
    );
  }

  getSettings(): Observable<ApiResponse<StoreSettings>> {
    return this.api.get<ApiResponse<StoreSettings>>(`${this.storePath}/settings`);
  }

  updateSettings(data: StoreSettings): Observable<ApiResponse<StoreSettings>> {
    return this.api.put<ApiResponse<StoreSettings>>(`${this.storePath}/settings`, data);
  }
}
