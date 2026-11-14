import { Component, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { GroupPermissionService } from '../../core/services/group-permission.service';
import { MenuPermissionService } from '../../core/services/menu-permission.service';
import { EmployeeGroupService } from '../../core/services/employee-group.service';
import {
  PermissionFlags,
  PermissionKey,
  PermissionMenuItem,
  PERMISSION_KEYS,
  GroupPermissionItem,
  GROUP_PERMISSION_ERROR_KEYS
} from '../../core/models/group-permission.model';
import { EmployeeGroup } from '../../core/models/employee-group.model';
import { LanguageService } from '../../ThemeOptions/store/language.service';
import { Subject, timer } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/** One table row: a menu item with its place in the tree */
interface PermissionRow {
  item: PermissionMenuItem;
  depth: number;
  hasChildren: boolean;
  /** Ids of the ancestors (to hide rows of collapsed groups) */
  ancestorIds: string[];
  /** The item and all its descendants (a group's checkbox applies to all of them) */
  subtreeIds: string[];
}

type CellState = { checked: boolean; indeterminate: boolean };

const NO_FLAGS: PermissionFlags = { canView: false, canAdd: false, canEdit: false, canDelete: false };

@Component({
  selector: 'app-group-permissions',
  standalone: false,
  styleUrl: './group-permissions.component.scss',
  templateUrl: './group-permissions.component.html',
})
export class GroupPermissionsComponent implements OnInit, OnDestroy {
  readonly keys = PERMISSION_KEYS;
  readonly keyLabels: Record<PermissionKey, string> = {
    canView: 'groupPermissions.view',
    canAdd: 'groupPermissions.add',
    canEdit: 'groupPermissions.edit',
    canDelete: 'groupPermissions.delete'
  };

  groups = signal<EmployeeGroup[]>([]);
  selectedGroupId = signal<string>('');
  rows = signal<PermissionRow[]>([]);
  /** Current flags per menu item id */
  perms = signal<Record<string, PermissionFlags>>({});
  /** Flags as saved on the server */
  private savedPerms = signal<Record<string, PermissionFlags>>({});
  collapsed = signal<Set<string>>(new Set());
  search = signal('');

  currentLanguage = signal<string>('en');
  isLoadingGroups = signal(false);
  isLoading = signal(false);
  isSaving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  private allIds = computed(() => this.rows().map(r => r.item.menuItemId));

  /** Rows shown: not inside a collapsed group, and matching the search (with their ancestors) */
  visibleRows = computed(() => {
    const collapsed = this.collapsed();
    const term = this.search().trim().toLowerCase();
    const rows = this.rows();

    let matchIds: Set<string> | null = null;
    if (term) {
      matchIds = new Set<string>();
      for (const r of rows) {
        const hit = r.item.nameAr.toLowerCase().includes(term) ||
          r.item.nameEn.toLowerCase().includes(term) ||
          (r.item.route || '').toLowerCase().includes(term);
        if (hit) {
          matchIds.add(r.item.menuItemId);
          r.ancestorIds.forEach(id => matchIds!.add(id));
        }
      }
    }

    return rows.filter(r =>
      (matchIds ? matchIds.has(r.item.menuItemId) : true) &&
      // while searching, show matches even inside collapsed groups
      (matchIds || !r.ancestorIds.some(id => collapsed.has(id))));
  });

  /** Number of menu items whose flags differ from the saved ones */
  changesCount = computed(() => {
    const current = this.perms();
    const saved = this.savedPerms();
    return this.allIds().filter(id => !this.sameFlags(current[id], saved[id])).length;
  });

  /** Menu items with at least View */
  grantedCount = computed(() => {
    const current = this.perms();
    return this.allIds().filter(id => current[id]?.canView).length;
  });

  selectedGroup = computed(() => this.groups().find(g => g.id === this.selectedGroupId()) ?? null);

  // Subject for cleanup
  private destroy$ = new Subject<void>();

  constructor(
    private groupPermissionService: GroupPermissionService,
    private menuPermissionService: MenuPermissionService,
    private employeeGroupService: EmployeeGroupService,
    private languageService: LanguageService,
    private translate: TranslateService,
    private activatedRoute: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.languageService.currentLanguage$
      .pipe(takeUntil(this.destroy$))
      .subscribe((language) => this.currentLanguage.set(language));

    this.loadGroups();

    // /users/group-permissions/:groupId (also reacts to back / forward)
    this.activatedRoute.paramMap
      .pipe(takeUntil(this.destroy$))
      .subscribe(params => {
        const groupId = params.get('groupId') || '';
        this.selectedGroupId.set(groupId);
        if (groupId) {
          this.loadPermissions(groupId);
        } else {
          this.clearPermissions();
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ---------------------------------------------------------------------------
  // Loading
  // ---------------------------------------------------------------------------

  private loadGroups(): void {
    this.isLoadingGroups.set(true);
    this.employeeGroupService
      .getAll()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.groups.set(response.status === 200 && response.data ? response.data : []);
          this.isLoadingGroups.set(false);
        },
        error: (error) => {
          console.error('Error loading employee groups:', error);
          this.errorMessage.set(this.translate.instant('groupPermissions.loadGroupsFailed'));
          this.isLoadingGroups.set(false);
        }
      });
  }

  loadPermissions(groupId: string): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.groupPermissionService
      .getByGroup(groupId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200 && response.data) {
            this.applyTree(response.data.menuItems);
          } else {
            this.clearPermissions();
            this.errorMessage.set(response.message || this.translate.instant('groupPermissions.loadFailed'));
          }
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Error loading group permissions:', error);
          this.clearPermissions();
          this.errorMessage.set(this.apiError(error, 'groupPermissions.loadFailed'));
          this.isLoading.set(false);
        }
      });
  }

  /** Flatten the tree into rows and remember the saved flags */
  private applyTree(tree: PermissionMenuItem[]): void {
    const rows: PermissionRow[] = [];
    const flags: Record<string, PermissionFlags> = {};

    const walk = (items: PermissionMenuItem[], depth: number, ancestors: string[]): string[] => {
      const ids: string[] = [];
      for (const item of items) {
        const row: PermissionRow = {
          item,
          depth,
          hasChildren: item.children.length > 0,
          ancestorIds: ancestors,
          subtreeIds: []
        };
        rows.push(row);
        flags[item.menuItemId] = {
          canView: item.canView, canAdd: item.canAdd, canEdit: item.canEdit, canDelete: item.canDelete
        };
        row.subtreeIds = [item.menuItemId, ...walk(item.children, depth + 1, [...ancestors, item.menuItemId])];
        ids.push(...row.subtreeIds);
      }
      return ids;
    };
    walk(tree, 0, []);

    this.rows.set(rows);
    this.savedPerms.set(flags);
    this.perms.set(this.copy(flags));
    this.collapsed.set(new Set());
  }

  private clearPermissions(): void {
    this.rows.set([]);
    this.perms.set({});
    this.savedPerms.set({});
  }

  // ---------------------------------------------------------------------------
  // Group selection
  // ---------------------------------------------------------------------------

  onGroupChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const groupId = select.value;

    if (this.changesCount() > 0 && !window.confirm(this.translate.instant('groupPermissions.confirmDiscard'))) {
      select.value = this.selectedGroupId(); // stay on the current group
      return;
    }

    this.successMessage.set(null);
    this.router.navigate(groupId ? ['/users/group-permissions', groupId] : ['/users/group-permissions'], { replaceUrl: true });
  }

  // ---------------------------------------------------------------------------
  // Checkbox state
  // ---------------------------------------------------------------------------

  /** A group's checkbox reflects the item and all its descendants */
  cellState(row: PermissionRow, key: PermissionKey): CellState {
    return this.stateOf(row.hasChildren ? row.subtreeIds : [row.item.menuItemId], key);
  }

  /** "All" checkbox of a row: every flag of the row (and its descendants) */
  rowState(row: PermissionRow): CellState {
    return this.stateOfAll(row.hasChildren ? row.subtreeIds : [row.item.menuItemId]);
  }

  /** Column header checkbox: one flag on every menu item */
  columnState(key: PermissionKey): CellState {
    return this.stateOf(this.allIds(), key);
  }

  allState(): CellState {
    return this.stateOfAll(this.allIds());
  }

  private stateOf(ids: string[], key: PermissionKey): CellState {
    const perms = this.perms();
    const on = ids.filter(id => perms[id]?.[key]).length;
    return { checked: ids.length > 0 && on === ids.length, indeterminate: on > 0 && on < ids.length };
  }

  private stateOfAll(ids: string[]): CellState {
    const perms = this.perms();
    let on = 0;
    for (const id of ids) {
      for (const key of PERMISSION_KEYS) {
        if (perms[id]?.[key]) on++;
      }
    }
    const total = ids.length * PERMISSION_KEYS.length;
    return { checked: total > 0 && on === total, indeterminate: on > 0 && on < total };
  }

  // ---------------------------------------------------------------------------
  // Changing permissions
  // ---------------------------------------------------------------------------

  toggleCell(row: PermissionRow, key: PermissionKey, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.setFlag(row.hasChildren ? row.subtreeIds : [row.item.menuItemId], key, checked);
  }

  toggleRow(row: PermissionRow, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.setAll(row.hasChildren ? row.subtreeIds : [row.item.menuItemId], checked);
  }

  toggleColumn(key: PermissionKey, event: Event): void {
    this.setFlag(this.allIds(), key, (event.target as HTMLInputElement).checked);
  }

  toggleAll(event: Event): void {
    this.setAll(this.allIds(), (event.target as HTMLInputElement).checked);
  }

  /** Add / Edit / Delete turn View on; turning View off removes every flag */
  private setFlag(ids: string[], key: PermissionKey, value: boolean): void {
    this.perms.update(current => {
      const next = { ...current };
      for (const id of ids) {
        const f = { ...(next[id] ?? NO_FLAGS) };
        f[key] = value;
        if (value && key !== 'canView') {
          f.canView = true;
        }
        if (!value && key === 'canView') {
          f.canAdd = f.canEdit = f.canDelete = false;
        }
        next[id] = f;
      }
      return next;
    });
    this.successMessage.set(null);
  }

  private setAll(ids: string[], value: boolean): void {
    this.perms.update(current => {
      const next = { ...current };
      for (const id of ids) {
        next[id] = { canView: value, canAdd: value, canEdit: value, canDelete: value };
      }
      return next;
    });
    this.successMessage.set(null);
  }

  isRowChanged(row: PermissionRow): boolean {
    const id = row.item.menuItemId;
    return !this.sameFlags(this.perms()[id], this.savedPerms()[id]);
  }

  // ---------------------------------------------------------------------------
  // Tree
  // ---------------------------------------------------------------------------

  toggleCollapse(row: PermissionRow): void {
    this.collapsed.update(set => {
      const next = new Set(set);
      next.has(row.item.menuItemId) ? next.delete(row.item.menuItemId) : next.add(row.item.menuItemId);
      return next;
    });
  }

  isCollapsed(row: PermissionRow): boolean {
    return this.collapsed().has(row.item.menuItemId);
  }

  expandAll(): void {
    this.collapsed.set(new Set());
  }

  collapseAll(): void {
    this.collapsed.set(new Set(this.rows().filter(r => r.hasChildren).map(r => r.item.menuItemId)));
  }

  onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  // ---------------------------------------------------------------------------
  // Save
  // ---------------------------------------------------------------------------

  save(): void {
    const groupId = this.selectedGroupId();
    if (!groupId || this.isSaving() || this.changesCount() === 0) {
      return;
    }

    const perms = this.perms();
    const permissions: GroupPermissionItem[] = this.allIds()
      .map(id => ({ menuItemId: id, ...(perms[id] ?? NO_FLAGS) }))
      .filter(p => p.canView || p.canAdd || p.canEdit || p.canDelete);

    this.isSaving.set(true);
    this.errorMessage.set(null);

    this.groupPermissionService
      .save({ employeeGroupId: groupId, permissions })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          if (response.status === 200) {
            this.savedPerms.set(this.copy(this.perms()));
            // The saved group may be the user's own: rebuild the sidebar
            this.menuPermissionService.refresh();
            const s = response.data;
            this.flashSuccess(s
              ? this.translate.instant('groupPermissions.savedSummary', s)
              : this.translate.instant('groupPermissions.saved'));
          } else {
            this.errorMessage.set(response.message || this.translate.instant('groupPermissions.saveFailed'));
          }
          this.isSaving.set(false);
        },
        error: (error) => {
          console.error('Error saving group permissions:', error);
          this.errorMessage.set(this.apiError(error, 'groupPermissions.saveFailed'));
          this.isSaving.set(false);
        }
      });
  }

  discardChanges(): void {
    this.perms.set(this.copy(this.savedPerms()));
    this.successMessage.set(null);
  }

  // ---------------------------------------------------------------------------
  // Helpers
  // ---------------------------------------------------------------------------

  getName(item: { nameAr: string; nameEn: string }): string {
    return this.currentLanguage() === 'ar' ? item.nameAr || item.nameEn : item.nameEn || item.nameAr;
  }

  getGroupName(group: EmployeeGroup): string {
    const name = this.currentLanguage() === 'ar' ? group.nameAr || group.nameEn : group.nameEn || group.nameAr;
    return group.isActive ? name : `${name} (${this.translate.instant('common.inactive')})`;
  }

  goToGroups(): void {
    this.router.navigate(['/users/employee-groups']);
  }

  private sameFlags(a: PermissionFlags | undefined, b: PermissionFlags | undefined): boolean {
    const x = a ?? NO_FLAGS;
    const y = b ?? NO_FLAGS;
    return x.canView === y.canView && x.canAdd === y.canAdd && x.canEdit === y.canEdit && x.canDelete === y.canDelete;
  }

  private copy(flags: Record<string, PermissionFlags>): Record<string, PermissionFlags> {
    return Object.fromEntries(Object.entries(flags).map(([id, f]) => [id, { ...f }]));
  }

  private flashSuccess(message: string): void {
    this.successMessage.set(message);
    timer(4000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.successMessage.set(null));
  }

  private apiError(error: any, fallbackKey: string): string {
    const key = error?.error?.message;
    return (GROUP_PERMISSION_ERROR_KEYS as readonly string[]).includes(key)
      ? this.translate.instant(`groupPermissions.errors.${key}`)
      : this.translate.instant(fallbackKey);
  }
}
