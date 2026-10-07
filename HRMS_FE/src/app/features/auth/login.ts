import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../../core/auth/auth.service';
import { ProblemDetails } from '../../core/api/models';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <div class="auth-page">
      <div class="auth-panel">
        <div class="brand">
          <span class="mark">H</span>
          <span class="word">HRMS<em>.</em></span>
        </div>

        <h1>Welcome <strong>back</strong></h1>
        <p class="muted lead">Sign in to your workspace.</p>
        <div class="rule"></div>

        @if (error()) {
          <div class="alert">{{ error() }}</div>
        }

        @if (!challenge()) {
          <form (ngSubmit)="submit()">
            <label class="field">
              <span>Email</span>
              <input name="email" type="email" [(ngModel)]="email" placeholder="you@company.com" autocomplete="username" />
            </label>

            <label class="field">
              <span>Password</span>
              <input name="password" type="password" [(ngModel)]="password" placeholder="••••••••••••" autocomplete="current-password" />
            </label>

            <button class="btn btn-red full" type="submit" [disabled]="busy()">
              {{ busy() ? 'Signing in…' : 'Sign in' }}
            </button>
          </form>
        } @else {
          <form (ngSubmit)="submitCode()">
            <p class="muted lead">Enter the six digit code from your authenticator app, or a recovery code.</p>

            <label class="field">
              <span>Verification code</span>
              <input name="code" [(ngModel)]="code" placeholder="000000" autocomplete="one-time-code" />
            </label>

            <button class="btn btn-red full" type="submit" [disabled]="busy()">Verify</button>
          </form>
        }
      </div>

      <div class="auth-art">
        <h2>Human resources,<br /><span class="accent">without the friction.</span></h2>
        <p class="muted">Multi-tenant HR for teams that move fast.</p>
      </div>
    </div>
  `,
  styles: [`
    .auth-page { display:grid; grid-template-columns:minmax(0,440px) 1fr; min-height:100vh; }
    .auth-panel { padding:56px 48px; background:var(--ink); border-right:1px solid var(--line); }
    .auth-art {
      display:flex; flex-direction:column; justify-content:center; padding:48px;
      background:radial-gradient(circle at 70% 35%, #2a2a2a 0%, var(--ink-deep) 62%);
    }
    .auth-art h2 { font-size:clamp(24px,3.4vw,44px); line-height:1.15; }
    .brand { display:flex; align-items:center; gap:10px; margin-bottom:48px; }
    .mark {
      display:grid; place-items:center; width:34px; height:34px;
      background:var(--red); color:#fff; font-family:var(--font-display);
      font-weight:800; font-size:18px;
    }
    .word { font-family:var(--font-display); font-weight:800; letter-spacing:.18em; font-size:15px; }
    .word em { color:var(--red); font-style:normal; }
    .lead { font-size:14px; margin:0 0 4px; }
    .full { width:100%; margin-top:10px; }
    @media (max-width:860px) { .auth-page { grid-template-columns:1fr; } .auth-art { display:none; } .auth-panel { border-right:0; padding:40px 20px; } }
  `]
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';
  code = '';

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly challenge = signal<string | null>(null);

  submit(): void {
    this.busy.set(true);
    this.error.set(null);

    this.auth.login(this.email, this.password).subscribe({
      next: result => {
        this.busy.set(false);

        if (result.requiresTwoFactor && result.challenge) {
          this.challenge.set(result.challenge.challengeToken);
          return;
        }

        this.afterSignIn();
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.describe(err));
      }
    });
  }

  submitCode(): void {
    const token = this.challenge();

    if (!token) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.auth.completeTwoFactor(token, this.code).subscribe({
      next: () => {
        this.busy.set(false);
        this.afterSignIn();
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.describe(err));
      }
    });
  }

  private afterSignIn(): void {
    this.auth.loadCurrentUser().subscribe({
      next: () => void this.router.navigate(['/dashboard']),
      error: () => this.error.set('Signed in, but the profile could not be loaded.')
    });
  }

  private describe(err: HttpErrorResponse): string {
    const problem = err.error as ProblemDetails | undefined;

    return problem?.detail ?? problem?.title ?? 'Something went wrong. Please try again.';
  }
}
