import { Injectable } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AiChatContextService {
  private readonly SESSION_KEY = 'ai_chat_session_id';
  private sessionId: string;
  private currentUrl = '';
  private activeFormKey?: string;
  private activeFormData?: any;

  constructor(private readonly router: Router) {
    this.sessionId = this.getOrCreateSessionId();

    // Listen to route changes
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.currentUrl = event.urlAfterRedirects || event.url;
    });

    this.currentUrl = this.router.url;
  }

  getSessionId(): string {
    return this.sessionId;
  }

  getCurrentRoute(): string {
    return this.currentUrl || this.router.url;
  }

  setActiveForm(formKey: string, formData: any): void {
    this.activeFormKey = formKey;
    this.activeFormData = formData;
  }

  clearActiveForm(): void {
    this.activeFormKey = undefined;
    this.activeFormData = undefined;
  }

  getActiveFormKey(): string | undefined {
    return this.activeFormKey;
  }

  getActiveFormData(): any {
    return this.activeFormData;
  }

  resetSession(): string {
    sessionStorage.removeItem(this.SESSION_KEY);
    this.sessionId = this.getOrCreateSessionId();
    return this.sessionId;
  }

  private getOrCreateSessionId(): string {
    let id = sessionStorage.getItem(this.SESSION_KEY);
    if (!id) {
      id = this.generateGuid();
      sessionStorage.setItem(this.SESSION_KEY, id);
    }
    return id;
  }

  private generateGuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }
}
