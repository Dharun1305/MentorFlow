import { afterNextRender, Component, DestroyRef, ElementRef, inject, input, output, viewChild } from '@angular/core';

@Component({
  selector: 'app-modal',
  template: `<dialog #dialog aria-labelledby="modal-title" (cancel)="cancel($event)" (click)="backdrop($event)">
    <div class="modal-card"><header><h2 id="modal-title">{{ title() }}</h2><button type="button" aria-label="Close dialog" [disabled]="busy()" (click)="closed.emit()">×</button></header><ng-content /></div>
  </dialog>`,
  styles: `dialog { border: 1px solid #dfe7f2; border-radius: 15px; padding: 0; width: min(660px, calc(100vw - 32px)); max-height: calc(100dvh - 40px); color: var(--text); box-shadow: 0 24px 90px #14274b30; }
    dialog::backdrop { background: #14274480; } .modal-card { padding: 27px; } header { display: flex; justify-content: space-between; align-items: center; gap: 20px; margin-bottom: 23px; }
    h2 { margin: 0; font-size: 1.2rem; letter-spacing: -.025em; } header button { width: 34px; height: 34px; border: 1px solid var(--border); border-radius: 7px; background: white; color: var(--muted); font-size: 1.3rem; }
    @media(max-width: 600px) { .modal-card { padding: 20px; } }`,
})
export class Modal {
  readonly title = input.required<string>();
  readonly busy = input(false);
  readonly closed = output<void>();
  private readonly dialog = viewChild<ElementRef<HTMLDialogElement>>('dialog');
  constructor() {
    afterNextRender(() => this.dialog()?.nativeElement.showModal());
    const opener = document.activeElement as HTMLElement | null;
    inject(DestroyRef).onDestroy(() => {
      this.dialog()?.nativeElement.close();
      queueMicrotask(() => (opener?.isConnected ? opener : document.querySelector<HTMLElement>('#main-content'))?.focus());
    });
  }
  cancel(event: Event): void { event.preventDefault(); if (!this.busy()) this.closed.emit(); }
  backdrop(event: MouseEvent): void { if (event.target === this.dialog()?.nativeElement && !this.busy()) this.closed.emit(); }
}
