import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  sideMenuOpen = signal(false);

  toggleMenu(): void {
    this.sideMenuOpen.update(v => !v);
  }

  closeMenu(): void {
    this.sideMenuOpen.set(false);
  }
}
