import { Component, ElementRef, OnInit, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AiChatService } from '../../services/ai-chat.service';
import { AiChatContextService } from '../../services/ai-chat-context.service';
import { AiChatMessage, AiChatResponse, AiQuickAction, AiUiAction } from '../../models/ai-chat.model';
import { AiChatTypingIndicatorComponent } from '../ai-chat-typing-indicator/ai-chat-typing-indicator.component';

@Component({
  selector: 'app-ai-chat-widget',
  standalone: true,
  imports: [CommonModule, FormsModule, AiChatTypingIndicatorComponent],
  templateUrl: './ai-chat-widget.component.html',
  styleUrl: './ai-chat-widget.component.scss'
})
export class AiChatWidgetComponent implements OnInit, AfterViewChecked {
  @ViewChild('messagesScroll') private messagesScrollContainer!: ElementRef;

  isOpen = false;
  isLoading = false;
  userInput = '';
  messages: AiChatMessage[] = [];
  quickActions: AiQuickAction[] = [];
  sessionId = '';
  hasError = false;
  lastUserMessage: string | null = null;

  starterSuggestions = [
    { label: '📦 Cek Stok Kopi', query: 'Cek stok kopi' },
    { label: '💰 Penjualan Hari Ini', query: 'Tampilkan total penjualan hari ini' },
    { label: '💸 Catat Pengeluaran', query: 'Bagaimana cara mencatat pengeluaran?' }
  ];

  constructor(
    private readonly aiChatService: AiChatService,
    private readonly contextService: AiChatContextService,
    private readonly router: Router
  ) {}

  selectSuggestion(query: string): void {
    if (this.isLoading) return;
    this.userInput = query;
    this.send();
  }

  ngOnInit(): void {
    this.sessionId = this.contextService.getSessionId();
    this.loadHistory();
  }

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  toggleChat(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen && this.messages.length === 0) {
      this.sendWelcomeMessage();
    }
  }

  private sendWelcomeMessage(): void {
    this.messages.push({
      role: 'assistant',
      text: 'Halo! Saya adalah SobatEntong AI. Saya bisa membantu Anda membuat transaksi penjualan (POS), mencatat pembelian stok ke supplier, memantau piutang/hutang, dan mengedit data produk. Apa yang bisa saya bantu hari ini?',
      timestamp: new Date()
    });
  }

  loadHistory(): void {
    if (!this.sessionId) return;
    this.aiChatService.getSessionHistory(this.sessionId).subscribe({
      next: (res) => {
        if (res.success && res.data && res.data.length > 0) {
          this.messages = res.data.map((m: any) => ({
            role: m.role,
            text: m.message || (m.role === 'tool' ? 'Mengambil data dari database...' : 'Memproses...'),
            timestamp: new Date(m.createdAt || Date.now())
          }));
        }
      },
      error: () => {
        console.debug('No active session history found or failed to load.');
      }
    });
  }

  send(): void {
    if (!this.userInput.trim() || this.isLoading) return;

    const userText = this.userInput;
    this.userInput = '';
    this.isLoading = true;
    this.hasError = false;
    this.quickActions = [];
    this.lastUserMessage = userText;

    // Add user message
    this.messages.push({
      role: 'user',
      text: userText,
      timestamp: new Date()
    });

    const activeFormKey = this.contextService.getActiveFormKey();
    const activeFormData = this.contextService.getActiveFormData();

    this.aiChatService.sendMessage({
      sessionId: this.sessionId,
      message: userText,
      currentRoute: this.router.url,
      activeFormKey,
      activeFormData
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          const aiResponse: AiChatResponse = res.data;
          this.messages.push({
            role: 'assistant',
            text: aiResponse.reply,
            timestamp: new Date(),
            usedFallback: aiResponse.usedFallback
          });

          this.quickActions = aiResponse.quickActions || [];

          if (aiResponse.uiAction) {
            this.handleUiAction(aiResponse.uiAction);
          }
        } else {
          this.messages.push({
            role: 'assistant',
            text: 'Gagal memproses respon dari server.',
            timestamp: new Date()
          });
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.hasError = true;
        
        let errorMsg = 'Maaf, koneksi ke server AI terputus. Silakan coba kembali.';
        if (err.status === 403) {
          errorMsg = 'Anda tidak memiliki izin (role) untuk menggunakan asisten AI ini.';
        } else if (err.error && err.error.message) {
          errorMsg = err.error.message;
        }

        this.messages.push({
          role: 'assistant',
          text: errorMsg,
          timestamp: new Date()
        });
      }
    });
  }

  onQuickActionClick(qa: AiQuickAction): void {
    if (this.isLoading) return;

    this.isLoading = true;
    this.quickActions = [];

    this.messages.push({
      role: 'user',
      text: qa.label,
      timestamp: new Date()
    });

    this.aiChatService.executeAction(this.sessionId, qa.action, qa.payload).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success && res.data) {
          this.messages.push({
            role: 'assistant',
            text: res.data.message || 'Transaksi berhasil dieksekusi.',
            timestamp: new Date()
          });
          // Refresh table data if active page has refresh handler
          const refreshEvent = new CustomEvent('ai-refresh-data');
          window.dispatchEvent(refreshEvent);
        } else {
          this.messages.push({
            role: 'assistant',
            text: res.message || 'Gagal mengeksekusi transaksi.',
            timestamp: new Date()
          });
        }
      },
      error: (err) => {
        this.isLoading = false;
        let errorMsg = 'Gagal menyimpan transaksi. Silakan coba lagi.';
        if (err.error && err.error.message) {
          errorMsg = err.error.message;
        }
        this.messages.push({
          role: 'assistant',
          text: errorMsg,
          timestamp: new Date()
        });
      }
    });
  }

  handleUiAction(action: AiUiAction): void {
    switch (action.type) {
      case 'navigate':
        if (action.route) {
          void this.router.navigate([action.route], { queryParams: action.query });
        }
        break;
      case 'fill_form':
        if (action.formKey && action.fields) {
          const event = new CustomEvent('ai-fill-form', {
            detail: { formKey: action.formKey, fields: action.fields }
          });
          window.dispatchEvent(event);
        }
        break;
      case 'refresh_table':
        const refreshEvent = new CustomEvent('ai-refresh-data');
        window.dispatchEvent(refreshEvent);
        break;
    }
  }

  retrySend(): void {
    if (!this.lastUserMessage || this.isLoading) return;
    this.userInput = this.lastUserMessage;
    this.send();
  }

  clearHistory(): void {
    if (confirm('Apakah Anda yakin ingin menghapus riwayat percakapan ini?')) {
      this.isLoading = true;
      this.aiChatService.closeSession(this.sessionId).subscribe({
        next: () => {
          this.isLoading = false;
          this.messages = [];
          this.quickActions = [];
          this.sessionId = this.contextService.resetSession();
          this.sendWelcomeMessage();
        },
        error: () => {
          this.isLoading = false;
          alert('Gagal menghapus sesi.');
        }
      });
    }
  }

  private scrollToBottom(): void {
    try {
      this.messagesScrollContainer.nativeElement.scrollTop = this.messagesScrollContainer.nativeElement.scrollHeight;
    } catch (err) {
      // Ignore scroll failures
    }
  }
}
