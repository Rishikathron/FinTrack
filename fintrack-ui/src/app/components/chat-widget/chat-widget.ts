import { Component, ViewChild, ElementRef, AfterViewChecked, ChangeDetectorRef, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../services/chat';
import { ChatMessage, ChatResponse } from '../../models/models';

@Component({
  selector: 'app-chat-widget',
  imports: [CommonModule, FormsModule],
  templateUrl: './chat-widget.html',
  styleUrl: './chat-widget.css',
})
export class ChatWidget implements AfterViewChecked {
  chatInput = '';
  chatSending = false;

  @ViewChild('chatBody') chatBody!: ElementRef<HTMLDivElement>;
  private shouldScroll = false;

  constructor(
    public chat: ChatService,
    private cdr: ChangeDetectorRef,
    private zone: NgZone
  ) {}

  ngAfterViewChecked(): void {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  toggle(): void {
    this.chat.isOpen = !this.chat.isOpen;
    if (this.chat.isOpen && this.chat.messages.length === 0) {
      this.chat.messages.push({
        role: 'ai',
        text: 'Hi! I\'m FinTrack AI 🤖\nAsk me to add gold, check your net worth, delete assets, or anything about your portfolio.\n\nType "what can you do?" to see all my capabilities.'
      });
    }
    this.shouldScroll = true;
  }

  sendMessage(): void {
    const msg = this.chatInput.trim();
    if (!msg || this.chatSending) return;

    this.chat.messages.push({ role: 'user', text: msg });
    this.chatInput = '';
    this.chatSending = true;
    this.shouldScroll = true;
    this.cdr.detectChanges();

    this.chat.send(msg).subscribe({
      next: (res: ChatResponse) => {
        this.zone.run(() => {
          this.chat.messages.push({ role: 'ai', text: res.reply });
          this.chatSending = false;
          this.shouldScroll = true;
          this.cdr.detectChanges();

          if (res.dataChanged) {
            this.chat.dataChanged$.next();
          }
        });
      },
      error: (err: any) => {
        this.zone.run(() => {
          const errorMsg = err?.error?.message || err?.message || 'Failed to reach AI. Is the server running?';
          this.chat.messages.push({ role: 'ai', text: `⚠️ ${errorMsg}` });
          this.chatSending = false;
          this.shouldScroll = true;
          this.cdr.detectChanges();
        });
      }
    });
  }

  resetChat(): void {
    this.chat.reset().subscribe({
      next: () => {
        this.chat.messages = [{ role: 'ai', text: 'Chat cleared! How can I help you?' }];
        this.shouldScroll = true;
        this.cdr.detectChanges();
      },
      error: () => {}
    });
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    if (this.chatBody) {
      const el = this.chatBody.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
