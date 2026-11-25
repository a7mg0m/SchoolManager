import { AfterViewInit, Directive, ElementRef, OnDestroy, booleanAttribute, input, numberAttribute } from '@angular/core';
import { Subscription } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

/** One logical row: a data row plus its detail rows (tr.dt-child right after it) */
interface RowGroup {
  rows: HTMLTableRowElement[];
}

/**
 * Turns any rendered table into a data table: a search box and a rows-per-page selector above it,
 * "Showing x–y of z" and page buttons under it. Works on the rows Angular renders (no data input needed):
 *
 *   <table class="table" appDataTable>                       search + paging (10 per page)
 *   <table class="table" appDataTable [dtSearch]="false">    paging only (the page has its own search)
 *   <table class="table" appDataTable [dtPageSize]="7">      another default page size
 *
 * Row rules (tbody, direct rows only — nested tables are left alone):
 *  - tr.dt-child: a detail row of the row before it (expanded details); paged and searched with it.
 *  - a row with one cell spanning the columns (e.g. "No data" from @empty): left as Angular shows it, not counted.
 *  - a row Angular hides ([hidden]) counts as filtered out by the page's own filters.
 * Rows are only hidden / shown (display), never moved or removed, so Angular keeps owning them.
 */
@Directive({
  selector: 'table[appDataTable]',
  standalone: false
})
export class DataTableDirective implements AfterViewInit, OnDestroy {
  /** Show the search box (false when the page already has its own search) */
  dtSearch = input(true, { transform: booleanAttribute });
  /** Rows per page at start */
  dtPageSize = input(10, { transform: numberAttribute });
  /** Choices of the rows-per-page selector */
  dtPageSizes = input<number[]>([7, 10, 25, 50, 100]);
  /** Editor tables: when a row is added, go to the page that shows it */
  dtFollowNew = input(false, { transform: booleanAttribute });

  /** Rows seen so far (to find the ones just added) */
  private known = new WeakSet<HTMLTableRowElement>();
  private initialized = false;

  private table: HTMLTableElement;
  private toolbar: HTMLDivElement;
  private footer: HTMLDivElement;
  private searchInput: HTMLInputElement;
  private sizeSelect: HTMLSelectElement;
  private sizeLabel: HTMLLabelElement;
  private info: HTMLElement;
  private pager: HTMLUListElement;

  private term = '';
  private pageSize = 10;
  private page = 1;
  private observer?: MutationObserver;
  private frame = 0;
  private langSub?: Subscription;

  constructor(
    host: ElementRef<HTMLTableElement>,
    private translate: TranslateService
  ) {
    this.table = host.nativeElement;
  }

  ngAfterViewInit(): void {
    this.pageSize = this.dtPageSizes().includes(this.dtPageSize()) ? this.dtPageSize() : this.dtPageSizes()[0];
    this.buildChrome();
    this.apply();

    // Rows come and go with the data (and [hidden] changes with the page's filters): re-apply, once per frame
    this.observer = new MutationObserver(() => this.schedule());
    this.observer.observe(this.table, {
      childList: true,
      subtree: true,
      characterData: true,
      attributes: true,
      attributeFilter: ['hidden', 'class']
    });
    this.langSub = this.translate.onLangChange.subscribe(() => {
      this.setLabels();
      this.apply();
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.langSub?.unsubscribe();
    cancelAnimationFrame(this.frame);
    this.toolbar?.remove();
    this.footer?.remove();
  }

  // ---------- DOM ----------

  /** Toolbar before the table (or its .table-responsive wrapper), footer after it */
  private buildChrome(): void {
    const anchor = this.table.parentElement?.classList.contains('table-responsive') ? this.table.parentElement : this.table;

    this.toolbar = document.createElement('div');
    this.toolbar.className = 'dt-toolbar';

    const sizeBox = document.createElement('div');
    sizeBox.className = 'dt-size';
    this.sizeLabel = document.createElement('label');
    this.sizeSelect = document.createElement('select');
    this.sizeSelect.className = 'form-select form-select-sm';
    const id = 'dt-size-' + Math.random().toString(36).slice(2, 9);
    this.sizeSelect.id = id;
    this.sizeLabel.htmlFor = id;
    for (const n of this.dtPageSizes()) {
      const option = document.createElement('option');
      option.value = String(n);
      option.textContent = String(n);
      option.selected = n === this.pageSize;
      this.sizeSelect.appendChild(option);
    }
    this.sizeSelect.addEventListener('change', () => {
      this.pageSize = Number(this.sizeSelect.value) || this.pageSize;
      this.page = 1;
      this.apply();
    });
    sizeBox.append(this.sizeLabel, this.sizeSelect);
    this.toolbar.appendChild(sizeBox);

    if (this.dtSearch()) {
      const searchBox = document.createElement('div');
      searchBox.className = 'dt-search';
      const icon = document.createElement('i');
      icon.className = 'pe-7s-search';
      this.searchInput = document.createElement('input');
      this.searchInput.type = 'search';
      this.searchInput.className = 'form-control form-control-sm';
      this.searchInput.addEventListener('input', () => {
        this.term = this.searchInput.value.trim().toLowerCase();
        this.page = 1;
        this.apply();
      });
      searchBox.append(icon, this.searchInput);
      this.toolbar.appendChild(searchBox);
    }

    this.footer = document.createElement('div');
    this.footer.className = 'dt-footer';
    this.info = document.createElement('small');
    this.info.className = 'dt-info';
    const nav = document.createElement('nav');
    this.pager = document.createElement('ul');
    this.pager.className = 'pagination pagination-sm mb-0';
    nav.appendChild(this.pager);
    this.footer.append(this.info, nav);

    anchor.before(this.toolbar);
    anchor.after(this.footer);
    this.setLabels();
  }

  private setLabels(): void {
    this.sizeLabel.textContent = this.translate.instant('common.dataTable.rowsPerPage');
    if (this.searchInput) {
      const placeholder = this.translate.instant('common.dataTable.search');
      this.searchInput.placeholder = placeholder;
      this.searchInput.setAttribute('aria-label', placeholder);
    }
  }

  // ---------- Paging / search ----------

  private schedule(): void {
    cancelAnimationFrame(this.frame);
    this.frame = requestAnimationFrame(() => this.apply());
  }

  /** Data rows grouped with their detail rows; colspan placeholder rows are skipped */
  private groups(): RowGroup[] {
    const groups: RowGroup[] = [];
    for (const body of Array.from(this.table.tBodies)) {
      for (const row of Array.from(body.rows)) {
        if (row.classList.contains('dt-child')) {
          if (groups.length) groups[groups.length - 1].rows.push(row);
          continue;
        }
        if (this.isPlaceholder(row)) continue;
        groups.push({ rows: [row] });
      }
    }
    return groups;
  }

  /**
   * What a row shows, for the search: its text, the chosen option of a select (not all its options),
   * the value of inputs / textareas. Buttons' icons and hidden template text are skipped.
   */
  private rowText(row: HTMLTableRowElement): string {
    const parts: string[] = [];
    const walk = (node: Node) => {
      if (node.nodeType === Node.TEXT_NODE) {
        parts.push(node.textContent || '');
        return;
      }
      if (!(node instanceof HTMLElement)) return;
      if (node instanceof HTMLSelectElement) {
        parts.push(node.selectedOptions[0]?.textContent || '');
        return;
      }
      if (node instanceof HTMLInputElement) {
        if (!['checkbox', 'radio', 'hidden', 'button', 'submit', 'file'].includes(node.type)) parts.push(node.value);
        return;
      }
      if (node instanceof HTMLTextAreaElement) {
        parts.push(node.value);
        return;
      }
      node.childNodes.forEach(walk);
    };
    row.childNodes.forEach(walk);
    return parts.join(' ');
  }

  /** "No data" style rows: a single cell spanning several columns */
  private isPlaceholder(row: HTMLTableRowElement): boolean {
    return row.cells.length === 1 && row.cells[0].colSpan > 1;
  }

  private apply(): void {
    if (!this.footer) return;
    // Our own display changes must not wake the observer again
    this.observer?.disconnect();

    const groups = this.groups();
    // Rows the page itself hides ([hidden]) stay out of the count
    const available = groups.filter(g => !g.rows[0].hidden);
    const matching = this.term
      ? available.filter(g => g.rows.map(r => this.rowText(r)).join(' ').toLowerCase().includes(this.term))
      : available;

    // Editor tables: a row was just added → show its page
    const added = groups.filter(g => !this.known.has(g.rows[0]));
    for (const g of added) this.known.add(g.rows[0]);
    if (this.dtFollowNew() && this.initialized && added.length) {
      const index = matching.indexOf(added[added.length - 1]);
      if (index >= 0) this.page = Math.floor(index / this.pageSize) + 1;
    }
    this.initialized = true;

    const pages = Math.max(1, Math.ceil(matching.length / this.pageSize));
    this.page = Math.min(Math.max(1, this.page), pages);
    const start = (this.page - 1) * this.pageSize;
    const visible = new Set(matching.slice(start, start + this.pageSize));

    for (const g of groups) {
      const show = visible.has(g);
      for (const row of g.rows) {
        if (show) {
          if (row.dataset['dtHidden']) {
            row.style.removeProperty('display');
            delete row.dataset['dtHidden'];
          }
        } else if (!row.dataset['dtHidden']) {
          row.style.display = 'none';
          row.dataset['dtHidden'] = '1';
        }
      }
    }

    // Toolbar and footer only when the table has data (or a search is typed, so it can be cleared)
    const hasData = available.length > 0 || !!this.term;
    this.toolbar.style.display = hasData ? '' : 'none';
    this.footer.style.display = hasData ? '' : 'none';

    if (matching.length === 0) {
      this.info.textContent = this.term ? this.translate.instant('common.dataTable.noMatches') : '';
    } else {
      this.info.textContent = this.translate.instant('common.dataTable.showing', {
        from: start + 1,
        to: start + visible.size,
        total: matching.length
      });
    }
    this.renderPager(pages);

    this.observer?.observe(this.table, {
      childList: true,
      subtree: true,
      characterData: true,
      attributes: true,
      attributeFilter: ['hidden', 'class']
    });
  }

  private renderPager(pages: number): void {
    this.pager.replaceChildren();
    this.pager.parentElement!.style.display = pages > 1 ? '' : 'none';
    if (pages <= 1) return;

    const rtl = getComputedStyle(this.table).direction === 'rtl';
    const add = (label: string, page: number, opts: { disabled?: boolean; active?: boolean; aria?: string } = {}) => {
      const li = document.createElement('li');
      li.className = 'page-item' + (opts.disabled ? ' disabled' : '') + (opts.active ? ' active' : '');
      const button = document.createElement('button');
      button.type = 'button';
      button.className = 'page-link';
      button.textContent = label;
      if (opts.aria) button.setAttribute('aria-label', opts.aria);
      if (opts.active) button.setAttribute('aria-current', 'page');
      button.disabled = !!opts.disabled;
      button.addEventListener('click', () => {
        this.page = page;
        this.apply();
      });
      li.appendChild(button);
      this.pager.appendChild(li);
    };

    const [first, prev, next, last] = rtl ? ['»', '›', '‹', '«'] : ['«', '‹', '›', '»'];
    add(first, 1, { disabled: this.page === 1, aria: this.translate.instant('common.dataTable.first') });
    add(prev, this.page - 1, { disabled: this.page === 1, aria: this.translate.instant('common.previous') });

    // Up to 5 page numbers around the current page
    const span = 5;
    let from = Math.max(1, this.page - Math.floor(span / 2));
    const to = Math.min(pages, from + span - 1);
    from = Math.max(1, to - span + 1);
    for (let p = from; p <= to; p++) add(String(p), p, { active: p === this.page });

    add(next, this.page + 1, { disabled: this.page === pages, aria: this.translate.instant('common.next') });
    add(last, pages, { disabled: this.page === pages, aria: this.translate.instant('common.dataTable.last') });
  }
}
