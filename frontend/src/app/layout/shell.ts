import { afterNextRender, Component, ElementRef, HostListener, inject, Injector, signal, viewChild } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({ selector: 'app-shell', imports: [RouterLink, RouterLinkActive, RouterOutlet], templateUrl: './shell.html', styleUrl: './shell.scss' })
export class Shell {
  readonly auth = inject(AuthService);
  private readonly injector = inject(Injector);
  readonly menuOpen = signal(false);
  readonly sidebar = viewChild<ElementRef<HTMLElement>>('sidebar');
  readonly menuButton = viewChild<ElementRef<HTMLButtonElement>>('menuButton');
  readonly nextFeatures = {
    Admin: ['Students', 'Mentors', 'Assignments'],
    Mentor: ['My students', 'Meetings', 'Goals'],
    Student: ['My mentor', 'Meetings', 'Goals'],
  };

  initials(): string {
    const user = this.auth.user();
    return `${user?.firstName.charAt(0) ?? ''}${user?.lastName.charAt(0) ?? ''}`.toUpperCase();
  }

  openMenu(): void {
    this.menuOpen.set(true);
    afterNextRender(() => this.sidebar()?.nativeElement.querySelector<HTMLAnchorElement>('a[href]')?.focus(), { injector: this.injector });
  }

  closeMenu(): void {
    this.menuOpen.set(false);
    afterNextRender(() => this.menuButton()?.nativeElement.focus(), { injector: this.injector });
  }

  @HostListener('window:resize')
  onResize(): void { if (window.innerWidth > 900) this.menuOpen.set(false); }

  @HostListener('document:keydown', ['$event'])
  onMenuKeydown(event: KeyboardEvent): void {
    if (!this.menuOpen()) return;
    if (event.key === 'Escape') { this.closeMenu(); return; }
    if (event.key !== 'Tab') return;
    const controls = this.sidebar()?.nativeElement.querySelectorAll<HTMLElement>('a[href], button');
    if (!controls?.length) return;
    const first = controls[0], last = controls[controls.length - 1];
    if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
    else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
  }
}
