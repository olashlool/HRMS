import { HttpErrorResponse } from '@angular/common/http';
import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { HrmsApiService } from '../../core/api/hrms-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { Entitlements, Plan, ProblemDetails } from '../../core/api/models';

@Component({
  selector: 'app-billing',
  imports: [DecimalPipe],
  template: `
    <h1>Plan &amp; <strong>entitlements</strong></h1>
    <div class="rule"></div>

    @if (error()) {
      <div class="alert">{{ error() }}</div>
    }

    @if (entitlements(); as current) {
      <div class="card summary">
        <div>
          <span class="label">Current plan</span>
          <span class="value accent">{{ current.planCode || 'none' }}</span>
        </div>
        <div>
          <span class="label">Seats</span>
          <span class="value">{{ current.usedEmployees }} / {{ current.maxEmployees ?? 'unlimited' }}</span>
        </div>
        <div>
          <span class="label">Included</span>
          <span class="chips">
            @for (feature of current.features; track feature) {
              <span class="tag tag-ok">{{ feature }}</span>
            }
            @if (current.features.length === 0) {
              <span class="muted">Nothing beyond core HR.</span>
            }
          </span>
        </div>
      </div>
    }

    <div class="grid cols-4 plans">
      @for (plan of plans(); track plan.id) {
        <div class="card plan" [class.is-current]="plan.code === entitlements()?.planCode">
          <h3>{{ plan.name }}</h3>
          <p class="price">
            <span class="amount">{{ plan.monthlyPriceMinor / 100 | number: '1.0-0' }}</span>
            <span class="muted">{{ plan.currency }} / mo</span>
          </p>
          <p class="muted seats">{{ plan.maxEmployees ?? 'Unlimited' }} seats</p>
          <ul>
            @for (feature of plan.features; track feature) {
              <li>{{ feature }}</li>
            }
            @if (plan.features.length === 0) {
              <li class="muted">Core HR only</li>
            }
          </ul>
          @if (plan.code !== entitlements()?.planCode && auth.can('users.manage')) {
            <button class="btn btn-red" type="button" (click)="choose(plan)">Choose</button>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .summary { display:flex; flex-wrap:wrap; gap:46px; margin-bottom:24px; }
    .label { display:block; color:var(--text-muted); font-family:var(--font-display); font-size:10px; font-weight:600; letter-spacing:.16em; text-transform:uppercase; }
    .value { font-family:var(--font-display); font-size:22px; font-weight:800; }
    .chips { display:flex; flex-wrap:wrap; gap:6px; padding-top:6px; }
    .plan { display:flex; flex-direction:column; gap:8px; }
    .plan.is-current { border-color:var(--red); }
    .price { margin:0; }
    .amount { font-family:var(--font-display); font-size:32px; font-weight:800; margin-right:6px; }
    .seats { margin:0; font-size:13px; }
    ul { margin:6px 0 16px; padding-left:18px; font-size:13px; color:var(--text-muted); }
    .plan button { margin-top:auto; }
  `]
})
export class Billing {
  readonly auth = inject(AuthService);
  private readonly api = inject(HrmsApiService);

  readonly plans = signal<Plan[]>([]);
  readonly entitlements = signal<Entitlements | null>(null);
  readonly error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  load(): void {
    this.api.listPlans().subscribe({ next: list => this.plans.set(list) });

    this.api.entitlements().subscribe({
      next: value => this.entitlements.set(value),
      error: (err: HttpErrorResponse) => this.error.set(this.describe(err))
    });
  }

  choose(plan: Plan): void {
    this.error.set(null);

    const current = this.entitlements();

    const request = current?.hasSubscription
      ? this.api.changePlan(plan.code, 1)
      : this.api.subscribe(plan.code, 1, true);

    request.subscribe({
      next: () => this.load(),
      error: (err: HttpErrorResponse) => this.error.set(this.describe(err))
    });
  }

  private describe(err: HttpErrorResponse): string {
    const problem = err.error as ProblemDetails | undefined;

    return problem?.detail ?? problem?.title ?? `Request failed (${err.status}).`;
  }
}
