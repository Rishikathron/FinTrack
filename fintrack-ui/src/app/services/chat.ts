import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { ChatRequest, ChatResponse, ChatMessage } from '../models/models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private url = `${environment.apiBaseUrl}/chat`;

  /** Chat history — persists across navigation */
  messages: ChatMessage[] = [];

  /** Whether the chat panel is open */
  isOpen = false;

  /** Emits whenever the AI modifies asset data — any component can subscribe to refresh */
  dataChanged$ = new Subject<void>();

  constructor(private http: HttpClient) {}

  send(message: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${this.url}/send`, { message } as ChatRequest);
  }

  reset(): Observable<unknown> {
    return this.http.post(`${this.url}/reset`, {});
  }
}
