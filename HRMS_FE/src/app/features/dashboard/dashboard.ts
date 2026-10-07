import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HrmsApiService } from '../../core/api/hrms-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Entitlements } from '../../core/api/models';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    <h1>Good to see you, <strong>{{ firstName() }}</strong></h1>
    <div class="rule"></div>

    <div class="grid cols-4">
      <div class="card stat">
        <span class="label">Workspace</span>
        <span class="value">{{ auth.user()?.tenantSlug }}</span>
      </div>

      <div class="card stat">
        <span class="label">Plan</span>
        <span class="value accent">{{ plan() }}</span>
      </div>

      <div class="card stat">
        <span class="label">Seats used</span>
        <span class="value">{{ seats() }}</span>
      </div>

      <div class="card stat">
        <span class="label">Permissions</span>
        <span class="value">{{ auth.permissions().length }}</span>
      </div>
    </div>

    <div class="grid cols-3 next">
      @if (auth.can('employees.read')) {
        <a class="card tile" routerLink="/employees">
          <h3>People</h3>
          <p class="muted">Browse and manage the employee directory.</p>
        </a>
      }

      <a class="card tile" routerLink="/billing">
        <h3>Billing</h3>
        <p class="muted">Review the plan, seats and included features.</p>
      </a>

      <div class="card tile">
        <h3>Your access</h3>
        <p class="muted">{{ auth.permissions().join(', ') || 'No permissions granted.' }}</p>
      </div>
    </div>
  `,
  styles: [`
    .stat { display:flex; flex-direction:column; gap:6px; padding:22px 24px; }
    .label { color:var(--text-muted); font-family:var(--font-display); font-size:10px; font-weight:600; letter-spacing:.16em; text-transform:uppercase; }
    .value { font-family:var(--font-display); font-size:24px; font-weight:800; }
    .next { margin-top:22px; }
    .tile { display:block; color:var(--text); }
    .tile:hover { border-color:var(--red); text-decoration:none; }
    .tile p { margin:0; font-size:13px; }
  `]
})
export class Dashboard {
  readonly auth = inject(AuthService);
  private readonly api = inject(HrmsApiService);

  private readonly entitlements = signal<Entitlements | null>(null);

  constructor() {
    this.api.entitlements().subscribe({
      next: value => this.entitlements.set(value),
      error: () => this.entitlements.set(null)
    });
  }

  firstName(): string {
    return this.auth.user()?.fullName.split(' ')[0] ?? '';
  }

  plan(): string {
    return this.entitlements()?.planCode?.toUpperCase() || 'NONE';
  }

  seats(): string {
    const value = this.entitlements();

    if (!value) {
      return '—';
    }

    return `${value.usedEmployees} / ${value.maxEmployees ?? '∞'}`;
  }
}
