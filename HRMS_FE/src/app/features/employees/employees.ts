import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HrmsApiService } from '../../core/api/hrms-api.service';
import { AuthService } from '../../core/auth/auth.service';
import { CreateEmployee, Employee, ProblemDetails } from '../../core/api/models';

@Component({
  selector: 'app-employees',
  imports: [FormsModule],
  template: `
    <h1>The <strong>directory</strong></h1>
    <div class="rule"></div>

    @if (error()) {
      <div class="alert">{{ error() }}</div>
    }

    <div class="layout">
      <div class="card">
        @if (employees().length === 0) {
          <p class="muted">No employees yet.</p>
        } @else {
          <table class="table">
            <thead>
              <tr>
                <th>Number</th>
                <th>Name</th>
                <th>Work email</th>
                <th>Type</th>
                <th>Hired</th>
                @if (auth.can('employees.delete')) { <th></th> }
              </tr>
            </thead>
            <tbody>
              @for (employee of employees(); track employee.id) {
                <tr>
                  <td><span class="tag">{{ employee.employeeNumber }}</span></td>
                  <td>{{ employee.firstName }} {{ employee.lastName }}</td>
                  <td class="muted">{{ employee.workEmail }}</td>
                  <td>{{ employee.employmentType }}</td>
                  <td class="muted">{{ employee.hireDate }}</td>
                  @if (auth.can('employees.delete')) {
                    <td><button class="btn btn-ghost" type="button" (click)="remove(employee)">Remove</button></td>
                  }
                </tr>
              }
            </tbody>
          </table>
        }
      </div>

      @if (auth.can('employees.create')) {
        <div class="card">
          <h3>Add someone</h3>
          <form (ngSubmit)="create()">
            <label class="field"><span>Employee number</span><input name="num" [(ngModel)]="draft.employeeNumber" placeholder="E-001" /></label>
            <label class="field"><span>First name</span><input name="first" [(ngModel)]="draft.firstName" /></label>
            <label class="field"><span>Last name</span><input name="last" [(ngModel)]="draft.lastName" /></label>
            <label class="field"><span>Work email</span><input name="mail" [(ngModel)]="draft.workEmail" /></label>
            <label class="field"><span>Hire date</span><input name="hire" type="date" [(ngModel)]="draft.hireDate" /></label>
            <label class="field">
              <span>Employment type</span>
              <select name="type" [(ngModel)]="draft.employmentType">
                <option [value]="1">Full time</option>
                <option [value]="2">Part time</option>
                <option [value]="3">Contract</option>
                <option [value]="4">Intern</option>
              </select>
            </label>
            <button class="btn btn-red" type="submit" [disabled]="busy()">Add employee</button>
          </form>
        </div>
      }
    </div>
  `,
  styles: [`
    .layout { display:grid; grid-template-columns:1fr minmax(0,330px); gap:20px; align-items:start; }
    .card { padding:22px; }
    @media (max-width:900px) { .layout { grid-template-columns:1fr; } }
  `]
})
export class Employees {
  readonly auth = inject(AuthService);
  private readonly api = inject(HrmsApiService);

  readonly employees = signal<Employee[]>([]);
  readonly error = signal<string | null>(null);
  readonly busy = signal(false);

  draft: CreateEmployee = {
    employeeNumber: '',
    firstName: '',
    lastName: '',
    workEmail: '',
    hireDate: new Date().toISOString().slice(0, 10),
    employmentType: 1
  };

  constructor() {
    this.load();
  }

  load(): void {
    this.api.listEmployees().subscribe({
      next: list => this.employees.set(list),
      error: (err: HttpErrorResponse) => this.error.set(this.describe(err))
    });
  }

  create(): void {
    this.busy.set(true);
    this.error.set(null);

    this.api.createEmployee({ ...this.draft, employmentType: Number(this.draft.employmentType) }).subscribe({
      next: () => {
        this.busy.set(false);
        this.draft = { ...this.draft, employeeNumber: '', firstName: '', lastName: '', workEmail: '' };
        this.load();
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(this.describe(err));
      }
    });
  }

  remove(employee: Employee): void {
    this.api.deleteEmployee(employee.id).subscribe({
      next: () => this.load(),
      error: (err: HttpErrorResponse) => this.error.set(this.describe(err))
    });
  }

  private describe(err: HttpErrorResponse): string {
    const problem = err.error as ProblemDetails | undefined;

    return problem?.detail ?? problem?.title ?? `Request failed (${err.status}).`;
  }
}
