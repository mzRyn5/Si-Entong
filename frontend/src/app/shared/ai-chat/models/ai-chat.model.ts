export interface AiChatRequest {
  sessionId: string;
  message: string;
  currentRoute: string;
  activeFormKey?: string;
  activeFormData?: any;
}

export interface AiChatResponse {
  reply: string;
  intent?: string;
  responseType: 'text' | 'navigation' | 'fill_form' | 'draft_preview' | 'clarification_needed' | 'error';
  draftId?: string;
  requiresConfirmation: boolean;
  usedFallback?: boolean;
  fallbackReason?: string;
  uiAction?: AiUiAction;
  quickActions: AiQuickAction[];
}

export interface AiUiAction {
  type: 'navigate' | 'fill_form' | 'refresh_table';
  route?: string;
  formKey?: string;
  query?: Record<string, any>;
  fields?: Record<string, any>;
}

export interface AiQuickAction {
  label: string;
  action: string;
  payload?: any;
}

export interface AiChatMessage {
  role: 'user' | 'assistant' | 'system' | 'tool';
  text: string;
  timestamp: Date;
  usedFallback?: boolean;
}
