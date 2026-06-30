import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);
  private counter = 0;

  show(message: string, type: ToastType = 'success', duration = 4000): void {
    const id = ++this.counter;
    this.toasts.update(current => [...current, { id, message, type }]);
    setTimeout(() => {
      this.toasts.update(current => current.filter(t => t.id !== id));
    }, duration);
  }

  success(message: string, duration = 4000): void {
    this.show(message, 'success', duration);
  }

  error(message: string, duration = 4000): void {
    this.show(message, 'error', duration);
  }
}
