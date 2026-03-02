import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { ChatWidget } from './components/chat-widget/chat-widget';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ChatWidget],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  sideMenuOpen = signal(false);

  toggleMenu(): void {
    this.sideMenuOpen.update((v: boolean) => !v);
  }

  closeMenu(): void {
    this.sideMenuOpen.set(false);
  }
}
