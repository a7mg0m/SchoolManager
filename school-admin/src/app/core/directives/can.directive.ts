import { Directive, ElementRef, Input, Renderer2, TemplateRef, ViewContainerRef, effect, signal } from '@angular/core';
import { MenuPermissionService, PermissionAction } from '../services/menu-permission.service';

/**
 * Shows the element only when the group of the logged-in user may do the action on the page.
 *
 *   <button *appCan="'add'" ...>                          current page
 *   <button *appCan="['edit', 'delete']" ...>             any of them
 *   <a *appCan="'add'; route: '/users/employee-groups'">  another page
 *   <a *appCan="'view'; route: '/x'; else plain">         template shown when not allowed
 */
@Directive({
  selector: '[appCan]',
  standalone: false
})
export class CanDirective {
  private readonly action = signal<PermissionAction | PermissionAction[]>('view');
  private readonly route = signal<string | null>(null);
  private readonly elseTemplate = signal<TemplateRef<unknown> | null>(null);
  /** Template currently rendered (main, else, or none) */
  private shown: TemplateRef<unknown> | null = null;

  @Input() set appCan(value: PermissionAction | PermissionAction[] | '' | null | undefined) {
    this.action.set(value || 'view');
  }

  @Input() set appCanRoute(value: string | null | undefined) {
    this.route.set(value || null);
  }

  @Input() set appCanElse(value: TemplateRef<unknown> | null | undefined) {
    this.elseTemplate.set(value || null);
  }

  constructor(
    private templateRef: TemplateRef<unknown>,
    private viewContainer: ViewContainerRef,
    private permissions: MenuPermissionService
  ) {
    effect(() => {
      const allowed = this.permissions.can(this.action(), this.route());
      const next = allowed ? this.templateRef : this.elseTemplate();
      if (next === this.shown) {
        return;
      }
      this.viewContainer.clear();
      if (next) {
        this.viewContainer.createEmbeddedView(next);
      }
      this.shown = next;
    });
  }
}

/**
 * Disables the element (e.g. a status switch) when the action is not allowed.
 * Only for elements without their own [disabled] binding.
 *
 *   <input type="checkbox" [appCanDisable]="'edit'" ...>
 */
@Directive({
  selector: '[appCanDisable]',
  standalone: false
})
export class CanDisableDirective {
  private readonly action = signal<PermissionAction | PermissionAction[]>('edit');
  private readonly route = signal<string | null>(null);

  @Input() set appCanDisable(value: PermissionAction | PermissionAction[] | '' | null | undefined) {
    this.action.set(value || 'edit');
  }

  @Input() set appCanDisableRoute(value: string | null | undefined) {
    this.route.set(value || null);
  }

  constructor(element: ElementRef<HTMLElement>, renderer: Renderer2, permissions: MenuPermissionService) {
    effect(() => {
      const allowed = permissions.can(this.action(), this.route());
      renderer.setProperty(element.nativeElement, 'disabled', !allowed);
    });
  }
}
