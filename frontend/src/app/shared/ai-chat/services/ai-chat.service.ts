import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../../../core/services/api-client.service';
import { ApiResponse } from '../../../core/models/api-response.model';
import { AiChatRequest, AiChatResponse } from '../models/ai-chat.model';

@Injectable({ providedIn: 'root' })
export class AiChatService {
  constructor(private readonly apiClient: ApiClientService) {}

  sendMessage(payload: AiChatRequest): Observable<ApiResponse<AiChatResponse>> {
    return this.apiClient.post<ApiResponse<AiChatResponse>>('ai-chat/message', payload);
  }

  executeAction(sessionId: string, action: string, payload: any): Observable<ApiResponse<any>> {
    return this.apiClient.post<ApiResponse<any>>('ai-chat/action', { sessionId, action, payload });
  }

  getSessionHistory(sessionId: string): Observable<ApiResponse<any>> {
    return this.apiClient.get<ApiResponse<any>>(`ai-chat/session/${sessionId}`);
  }

  closeSession(sessionId: string): Observable<ApiResponse<any>> {
    return this.apiClient.delete<ApiResponse<any>>(`ai-chat/session/${sessionId}`);
  }
}
