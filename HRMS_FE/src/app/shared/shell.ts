import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="topbar">
      <div class="shell bar">
        <a class="brand" routerLink="/dashboard">
          <span class="mark">H</span>
          <span class="word">HRMS<em>.</em></span>
        </a>

        <nav>
          <a routerLink="/dashboard" routerLinkActive="on">Dashboard</a>
          @if (auth.can('employees.read')) {
            <a routerLink="/employees" routerLinkActive="on">Employees</a>
          }
          <a routerLink="/billing" routerLinkActive="on">Billing</a>
        </nav>

        <div class="who">
          <span class="muted">{{ auth.user()?.fullName }}</span>
          <span class="tag tag-red">{{ auth.user()?.tenantSlug }}</span>
          <button class="btn btn-ghost" type="button" (click)="auth.logout()">Sign out</button>
        </div>
      </div>
    </header>

    <main class="shell page">
      <router-outlet />
    </main>
  `,
  styles: [`
    .topbar { background:var(--ink); border-bottom:1px solid var(--line); position:sticky; top:0; z-index:10; }
    .bar { display:flex; align-items:center; gap:28px; height:68px; }
    .brand { display:flex; align-items:center; gap:9px; color:var(--text); text-decoration:none; }
    .brand:hover { text-decoration:none; }
    .mark { display:grid; place-items:center; width:30px; height:30px; background:var(--red); font-family:var(--font-display); font-weight:800; font-size:16px; }
    .word { font-family:var(--font-display); font-weight:800; letter-spacing:.18em; font-size:14px; }
    .word em { color:var(--red); font-style:normal; }
    nav { display:flex; gap:4px; margin-right:auto; }
    nav a {
      padding:23px 18px; color:var(--text); font-family:var(--font-display);
      font-size:12px; font-weight:600; letter-spacing:.12em; text-transform:uppercase;
      border-bottom:2px solid transparent;
    }
    nav a:hover { color:var(--red); text-decoration:none; }
    nav a.on { background:var(--red); color:#fff; }
    .who { display:flex; align-items:center; gap:14px; font-size:13px; }
    .page { padding:40px 16px 70px; }
    @media (max-width:760px) {
      .bar { height:auto; flex-wrap:wrap; padding-top:12px; padding-bottom:12px; gap:12px; }
      nav a { padding:10px 12px; }
    }
  `]
})
export class Shell {
  readonly auth = inject(AuthService);
}
