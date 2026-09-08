/**
 * Component Widget Trợ lý AI (Depot AI Assistant / Copilot).
 * Hiển thị cửa sổ chat popup nổi ở góc phải màn hình, hỗ trợ:
 * - Tương tác ngôn ngữ tự nhiên với backend Agent API (/api/agents/execute)
 * - Tích hợp Google Gemini AI và RAG Depot Context
 * - Trình render Markdown chuyên nghiệp, định dạng danh sách, in đậm, ngắt dòng gãy gọn
 * - Gợi ý các câu hỏi nghiệp vụ nhanh (Quick Prompt Chips)
 * - Tự động cuộn và lưu lịch sử hội thoại
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Component, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AgentService } from '../../../core/services';
import { AgentMessageDto, AgentExecuteResponse } from '../../../core/models/api.models';

interface ChatMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
  status?: string;
  timestamp: Date;
}

@Component({
  selector: 'app-ai-assistant-widget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <!-- Floating Trigger Button -->
    <div class="ai-widget-container">
      <button
        type="button"
        class="ai-trigger-btn"
        [class.active]="isOpen"
        (click)="toggleOpen()"
        title="Trợ lý AI Depot Copilot">
        <span class="btn-icon" *ngIf="!isOpen">🤖</span>
        <span class="btn-icon" *ngIf="isOpen">✕</span>
        <span class="btn-label" *ngIf="!isOpen">AI Copilot</span>
        <span class="pulse-dot" *ngIf="!isOpen"></span>
      </button>

      <!-- Chat Window Popup -->
      <div class="ai-chat-window" *ngIf="isOpen">
        <!-- Header -->
        <div class="chat-header">
          <div class="header-left">
            <div class="header-avatar">🤖</div>
            <div>
              <div class="header-title">Depot AI Copilot</div>
              <div class="header-status">
                <span class="status-dot"></span> Powered by Google Gemini
              </div>
            </div>
          </div>
          <div class="header-actions">
            <button class="icon-btn" (click)="clearHistory()" title="Xóa lịch sử chat">🗑️</button>
            <button class="icon-btn" (click)="toggleOpen()" title="Đóng">✕</button>
          </div>
        </div>

        <!-- Messages Area -->
        <div class="chat-messages" #scrollContainer>
          <!-- Welcome Message -->
          <div class="message-wrapper assistant">
            <div class="msg-avatar">🤖</div>
            <div class="msg-content-wrap">
              <div class="msg-bubble">
                <div class="welcome-header">👋 <strong>Xin chào! Tôi là Depot AI Assistant.</strong></div>
                <div class="welcome-sub">Tôi có thể giúp bạn tra cứu và phân tích số liệu bãi:</div>
                <ul class="md-list">
                  <li>Tra cứu tình trạng & vị trí container cụ thể</li>
                  <li>Số lượng container đang tồn bãi theo hãng tàu</li>
                  <li>Báo cáo thời gian lưu bãi (Aging Report)</li>
                  <li>Khẩu lượng & lượt xuất nhập Gate In/Out</li>
                </ul>
              </div>
            </div>
          </div>

          <!-- Quick Suggestion Chips (when few messages) -->
          <div class="quick-suggestions" *ngIf="messages.length <= 1">
            <div class="suggestion-title">Gợi ý câu hỏi nhanh:</div>
            <div class="chips-grid">
              <button
                type="button"
                class="chip-btn"
                *ngFor="let s of quickPrompts"
                (click)="sendQuickPrompt(s.prompt)">
                <span class="chip-icon">{{ s.icon }}</span>
                <span>{{ s.label }}</span>
              </button>
            </div>
          </div>

          <!-- Chat History -->
          <div
            *ngFor="let msg of messages"
            class="message-wrapper"
            [class.user]="msg.role === 'user'"
            [class.assistant]="msg.role === 'assistant'">
            <div class="msg-avatar" *ngIf="msg.role === 'assistant'">🤖</div>
            <div class="msg-content-wrap">
              <div class="msg-bubble" [class.error]="msg.status === 'Failure'">
                <div class="msg-markdown" [innerHTML]="renderMarkdown(msg.content)"></div>
              </div>
              <div class="msg-time">{{ msg.timestamp | date:'HH:mm' }}</div>
            </div>
            <div class="msg-avatar user-avatar" *ngIf="msg.role === 'user'">👤</div>
          </div>

          <!-- Loading Indicator -->
          <div class="message-wrapper assistant" *ngIf="isLoading">
            <div class="msg-avatar">🤖</div>
            <div class="msg-bubble loading-bubble">
              <span class="dot"></span>
              <span class="dot"></span>
              <span class="dot"></span>
              <span class="loading-text">Gemini đang phân tích dữ liệu...</span>
            </div>
          </div>
        </div>

        <!-- Input Box -->
        <div class="chat-input-area">
          <input
            type="text"
            class="chat-input"
            placeholder="Hỏi AI về bãi container..."
            [(ngModel)]="currentInput"
            (keydown.enter)="sendMessage()"
            [disabled]="isLoading"
            #inputEl />
          <button
            type="button"
            class="send-btn"
            (click)="sendMessage()"
            [disabled]="!currentInput.trim() || isLoading">
            ➔
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .ai-widget-container {
      position: fixed;
      bottom: 24px;
      right: 24px;
      z-index: 9999;
      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    }

    .ai-trigger-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      background: linear-gradient(135deg, #2563eb, #1d4ed8);
      color: #ffffff;
      border: none;
      border-radius: 30px;
      padding: 10px 18px;
      font-size: 14px;
      font-weight: 700;
      cursor: pointer;
      box-shadow: 0 4px 14px rgba(37, 99, 235, 0.4);
      transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
      position: relative;
    }

    .ai-trigger-btn:hover {
      transform: translateY(-2px);
      box-shadow: 0 6px 20px rgba(37, 99, 235, 0.5);
    }

    .ai-trigger-btn.active {
      border-radius: 50%;
      width: 44px;
      height: 44px;
      padding: 0;
      display: grid;
      place-items: center;
      background: #334155;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.2);
    }

    .btn-icon {
      font-size: 18px;
    }

    .pulse-dot {
      width: 8px;
      height: 8px;
      background: #10b981;
      border-radius: 50%;
      box-shadow: 0 0 0 2px rgba(255, 255, 255, 0.8);
      animation: pulse 1.8s infinite;
    }

    @keyframes pulse {
      0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
      70% { transform: scale(1); box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
      100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
    }

    .ai-chat-window {
      position: absolute;
      bottom: 60px;
      right: 0;
      width: 440px;
      max-width: calc(100vw - 32px);
      height: 580px;
      background: #ffffff;
      border-radius: 16px;
      box-shadow: 0 12px 36px rgba(0, 0, 0, 0.18), 0 2px 6px rgba(0, 0, 0, 0.06);
      border: 1px solid #cbd5e1;
      display: flex;
      flex-direction: column;
      overflow: hidden;
      animation: slideUp 0.25s cubic-bezier(0.16, 1, 0.3, 1);
    }

    @keyframes slideUp {
      from { opacity: 0; transform: translateY(16px) scale(0.96); }
      to { opacity: 1; transform: translateY(0) scale(1); }
    }

    .chat-header {
      background: linear-gradient(135deg, #0f172a, #1e293b);
      color: #ffffff;
      padding: 12px 16px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      border-bottom: 1px solid #334155;
    }

    .header-left {
      display: flex;
      align-items: center;
      gap: 10px;
    }

    .header-avatar {
      width: 34px;
      height: 34px;
      background: rgba(37, 99, 235, 0.3);
      border: 1px solid rgba(59, 130, 246, 0.5);
      border-radius: 10px;
      display: grid;
      place-items: center;
      font-size: 18px;
    }

    .header-title {
      font-weight: 700;
      font-size: 14px;
      letter-spacing: -0.01em;
    }

    .header-status {
      font-size: 11px;
      color: #94a3b8;
      display: flex;
      align-items: center;
      gap: 5px;
    }

    .status-dot {
      width: 7px;
      height: 7px;
      background: #10b981;
      border-radius: 50%;
    }

    .header-actions {
      display: flex;
      gap: 4px;
    }

    .icon-btn {
      background: transparent;
      border: none;
      color: #94a3b8;
      width: 28px;
      height: 28px;
      border-radius: 6px;
      display: grid;
      place-items: center;
      cursor: pointer;
      font-size: 13px;
      transition: all 0.15s;
    }

    .icon-btn:hover {
      background: rgba(255, 255, 255, 0.1);
      color: #ffffff;
    }

    .chat-messages {
      flex: 1;
      overflow-y: auto;
      padding: 16px;
      display: flex;
      flex-direction: column;
      gap: 14px;
      background: #f8fafc;
    }

    .message-wrapper {
      display: flex;
      gap: 10px;
      max-width: 92%;
      animation: msgFadeIn 0.2s ease;
    }

    @keyframes msgFadeIn {
      from { opacity: 0; transform: translateY(6px); }
      to { opacity: 1; transform: translateY(0); }
    }

    .message-wrapper.user {
      align-self: flex-end;
      flex-direction: row-reverse;
    }

    .message-wrapper.assistant {
      align-self: flex-start;
    }

    .msg-avatar {
      width: 30px;
      height: 30px;
      border-radius: 50%;
      background: #e2e8f0;
      display: grid;
      place-items: center;
      font-size: 15px;
      flex-shrink: 0;
      margin-top: 2px;
    }

    .msg-avatar.user-avatar {
      background: #dbeafe;
    }

    .msg-content-wrap {
      display: flex;
      flex-direction: column;
      max-width: 100%;
    }

    .msg-bubble {
      padding: 12px 16px;
      border-radius: 14px;
      font-size: 13.5px;
      line-height: 1.6;
      word-break: break-word;
    }

    .user .msg-bubble {
      background: #2563eb;
      color: #ffffff;
      border-bottom-right-radius: 3px;
    }

    .assistant .msg-bubble {
      background: #ffffff;
      color: #1e293b;
      border: 1px solid #e2e8f0;
      border-bottom-left-radius: 3px;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
    }

    .msg-bubble.error {
      background: #fef2f2;
      border-color: #fecaca;
      color: #dc2626;
    }

    .welcome-header {
      margin-bottom: 6px;
      color: #0f172a;
    }

    .welcome-sub {
      color: #475569;
      font-size: 13px;
      margin-bottom: 8px;
    }

    .msg-time {
      font-size: 10.5px;
      color: #94a3b8;
      margin-top: 4px;
      padding: 0 4px;
    }

    .user .msg-time {
      text-align: right;
    }

    /* Markdown Styling Inside Chat Bubbles */
    :host ::ng-deep .msg-markdown {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    :host ::ng-deep .md-p {
      margin: 0;
      line-height: 1.55;
    }

    :host ::ng-deep .md-h3 {
      font-weight: 700;
      font-size: 14px;
      color: #0f172a;
      margin: 6px 0 2px 0;
    }

    :host ::ng-deep .md-list {
      margin: 4px 0 4px 18px;
      padding: 0;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    :host ::ng-deep .md-list li {
      line-height: 1.5;
    }

    :host ::ng-deep .md-code {
      background: #f1f5f9;
      color: #0f172a;
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 12px;
      font-family: monospace;
      border: 1px solid #e2e8f0;
    }

    :host ::ng-deep strong {
      font-weight: 700;
      color: #0f172a;
    }

    .user :host ::ng-deep strong {
      color: #ffffff;
    }

    .quick-suggestions {
      background: #ffffff;
      border: 1px solid #e2e8f0;
      border-radius: 12px;
      padding: 10px 12px;
      margin: 2px 0;
    }

    .suggestion-title {
      font-size: 11px;
      font-weight: 700;
      color: #64748b;
      margin-bottom: 8px;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .chips-grid {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .chip-btn {
      display: flex;
      align-items: center;
      gap: 8px;
      background: #f1f5f9;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 7px 10px;
      font-size: 12px;
      color: #334155;
      cursor: pointer;
      text-align: left;
      transition: all 0.15s;
    }

    .chip-btn:hover {
      background: #e0e7ff;
      border-color: #c7d2fe;
      color: #1d4ed8;
    }

    .chip-icon {
      font-size: 14px;
    }

    .loading-bubble {
      display: flex;
      gap: 8px;
      align-items: center;
      padding: 10px 14px;
    }

    .loading-bubble .dot {
      width: 6px;
      height: 6px;
      background: #3b82f6;
      border-radius: 50%;
      animation: bounce 1.2s infinite ease-in-out;
    }

    .loading-bubble .dot:nth-child(2) { animation-delay: 0.2s; }
    .loading-bubble .dot:nth-child(3) { animation-delay: 0.4s; }

    .loading-text {
      font-size: 12px;
      color: #64748b;
      margin-left: 4px;
    }

    @keyframes bounce {
      0%, 80%, 100% { transform: scale(0); }
      40% { transform: scale(1); }
    }

    .chat-input-area {
      padding: 12px 14px;
      background: #ffffff;
      border-top: 1px solid #e2e8f0;
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .chat-input {
      flex: 1;
      padding: 9px 14px;
      border: 1px solid #cbd5e1;
      border-radius: 20px;
      font-size: 13px;
      outline: none;
      transition: border-color 0.15s;
    }

    .chat-input:focus {
      border-color: #2563eb;
      box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.15);
    }

    .send-btn {
      width: 36px;
      height: 36px;
      background: #2563eb;
      color: #ffffff;
      border: none;
      border-radius: 50%;
      display: grid;
      place-items: center;
      cursor: pointer;
      font-size: 15px;
      flex-shrink: 0;
      transition: all 0.15s;
    }

    .send-btn:hover:not(:disabled) {
      background: #1d4ed8;
      transform: scale(1.05);
    }

    .send-btn:disabled {
      background: #cbd5e1;
      cursor: not-allowed;
    }
  `]
})
export class AiAssistantWidgetComponent implements AfterViewChecked {
  @ViewChild('scrollContainer') private readonly scrollContainer?: ElementRef<HTMLDivElement>;
  @ViewChild('inputEl') private readonly inputEl?: ElementRef<HTMLInputElement>;

  isOpen = false;
  isLoading = false;
  currentInput = '';
  readonly messages: ChatMessage[] = [];

  readonly quickPrompts = [
    { icon: '📊', label: 'Có bao nhiêu container trong bãi?', prompt: 'Có bao nhiêu container trong bãi?' },
    { icon: '⏳', label: 'Báo cáo thời gian lưu bãi (Aging)?', prompt: 'Báo cáo thời gian lưu bãi container' },
    { icon: '📈', label: 'Khẩu lượng / Sản lượng hàng ngày?', prompt: 'Khẩu lượng hàng ngày của bãi' },
    { icon: '🔍', label: 'Tra cứu container CMAU1234564', prompt: 'Tìm container CMAU1234564' }
  ];

  constructor(
    private readonly agentService: AgentService
  ) {}

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  toggleOpen(): void {
    this.isOpen = !this.isOpen;
    if (this.isOpen) {
      setTimeout(() => this.inputEl?.nativeElement.focus(), 100);
    }
  }

  sendQuickPrompt(promptText: string): void {
    this.currentInput = promptText;
    this.sendMessage();
  }

  sendMessage(): void {
    const text = this.currentInput.trim();
    if (!text || this.isLoading) return;

    // Thêm câu hỏi người dùng
    this.messages.push({
      role: 'user',
      content: text,
      timestamp: new Date()
    });

    this.currentInput = '';
    this.isLoading = true;

    // Chuyển đổi lịch sử
    const historyDto: AgentMessageDto[] = this.messages.map(m => ({
      role: m.role,
      content: m.content
    }));

    this.agentService.execute(text, historyDto).subscribe({
      next: (res: AgentExecuteResponse) => {
        this.isLoading = false;
        this.messages.push({
          role: 'assistant',
          content: res.message || 'Không nhận được câu trả lời từ hệ thống.',
          status: res.status,
          timestamp: new Date()
        });
      },
      error: (err: any) => {
        this.isLoading = false;
        const errMsg = err?.error?.detail || err?.error?.message || 'Có lỗi xảy ra khi kết nối tới AI Agent.';
        this.messages.push({
          role: 'assistant',
          content: `⚠️ ${errMsg}`,
          status: 'Failure',
          timestamp: new Date()
        });
      }
    });
  }

  clearHistory(): void {
    this.messages.length = 0;
  }

  /**
   * Phân tích và định dạng chuỗi Markdown từ Gemini AI thành HTML đẹp mắt, ngăn nắp.
   */
  renderMarkdown(content: string): string {
    if (!content) return '';

    // 1. Thoát các ký tự đặc biệt HTML (trừ trường hợp đã escape)
    let text = content
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');

    // 2. Chuyển đổi tiêu đề markdown ### hoặc ##
    text = text.replace(/^### (.*$)/gim, '<div class="md-h3">$1</div>');
    text = text.replace(/^## (.*$)/gim, '<div class="md-h3">$1</div>');

    // 3. Chuyển đổi in đậm **text**
    text = text.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

    // 4. Chuyển đổi in nghiêng *text* (nếu không phải bullet point)
    text = text.replace(/(^|[^*])\*([^*\n]+)\*([^*]|$)/g, '$1<em>$2</em>$3');

    // 5. Chuyển đổi inline code `code`
    text = text.replace(/`([^`]+)`/g, '<code class="md-code">$1</code>');

    // 6. Xử lý danh sách gạch đầu dòng và ngắt dòng
    const lines = text.split('\n');
    const result: string[] = [];
    let inList = false;

    for (const rawLine of lines) {
      const trimmed = rawLine.trim();

      // Kiểm tra gạch đầu dòng: •, -, * (với khoảng trắng)
      if (trimmed.startsWith('• ') || trimmed.startsWith('- ') || trimmed.startsWith('* ')) {
        if (!inList) {
          result.push('<ul class="md-list">');
          inList = true;
        }
        const item = trimmed.replace(/^[•\-*]\s+/, '');
        result.push(`<li>${item}</li>`);
      } else {
        if (inList) {
          result.push('</ul>');
          inList = false;
        }
        if (trimmed.length > 0) {
          result.push(`<p class="md-p">${rawLine}</p>`);
        }
      }
    }

    if (inList) {
      result.push('</ul>');
    }

    return result.join('');
  }

  private scrollToBottom(): void {
    try {
      if (this.scrollContainer) {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
      }
    } catch {
      // Bỏ qua lỗi scroll
    }
  }
}
