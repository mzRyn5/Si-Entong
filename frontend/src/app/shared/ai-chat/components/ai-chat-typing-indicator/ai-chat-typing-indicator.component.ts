import { Component } from '@angular/core';

@Component({
  selector: 'app-ai-chat-typing-indicator',
  standalone: true,
  template: `
    <div class="typing-indicator-container">
      <div class="typing-dots">
        <span class="dot"></span>
        <span class="dot"></span>
        <span class="dot"></span>
      </div>
      <span class="typing-text">AI sedang mengetik...</span>
    </div>
  `,
  styles: [`
    .typing-indicator-container {
      display: flex;
      align-items: center;
      padding: 8px 12px;
      margin: 4px 8px;
      background-color: var(--color-primary-100);
      border-radius: 12px;
      width: fit-content;
      animation: fadeIn 0.3s ease-in-out;
      border: 1px solid var(--color-border);
    }
    .typing-dots {
      display: flex;
      gap: 4px;
      margin-right: 8px;
      align-items: center;
    }
    .dot {
      width: 6px;
      height: 6px;
      background-color: var(--color-primary-500);
      border-radius: 50%;
      display: inline-block;
      animation: bounce 1.4s infinite ease-in-out both;
    }
    .dot:nth-child(1) {
      animation-delay: -0.32s;
    }
    .dot:nth-child(2) {
      animation-delay: -0.16s;
    }
    .typing-text {
      font-size: 0.75rem;
      color: var(--color-primary-800);
      font-weight: 600;
    }
    @keyframes bounce {
      0%, 80%, 100% {
        transform: scale(0);
      }
      40% {
        transform: scale(1.0);
      }
    }
    @keyframes fadeIn {
      from {
        opacity: 0;
        transform: translateY(5px);
      }
      to {
        opacity: 1;
        transform: translateY(0);
      }
    }
  `]
})
export class AiChatTypingIndicatorComponent {}
