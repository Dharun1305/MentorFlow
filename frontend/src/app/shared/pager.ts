import { Component, computed, input, output } from '@angular/core';

@Component({ selector: 'app-pager', template: `
  <nav aria-label="Pagination" class="pager"><span>{{ total() }} {{ label() }} · Page {{ page() }} of {{ pages() }}</span><div><button type="button" aria-label="Previous page" [disabled]="busy() || page() <= 1" (click)="changed.emit(page() - 1)">← Previous</button><button type="button" aria-label="Next page" [disabled]="busy() || page() >= pages()" (click)="changed.emit(page() + 1)">Next →</button></div></nav>`,
  styles: `.pager { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-top: 18px; font-size: .7rem; color: var(--muted); } .pager > div { display: flex; gap: 8px; } button { border: 1px solid var(--border); border-radius: 7px; padding: 9px 12px; background: white; color: var(--text); font-size: .7rem; } button:disabled { opacity: .45; cursor: default; } @media(max-width: 500px) { .pager { flex-direction: column; align-items: flex-start; } }`,
})
export class Pager {
  readonly page = input(1);
  readonly pageSize = input(20);
  readonly total = input(0);
  readonly busy = input(false);
  readonly label = input('records');
  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly changed = output<number>();
}
