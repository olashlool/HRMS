import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateEmployee, Employee, Entitlements, Plan } from './models';

@Injectable({ providedIn: 'root' })
export class HrmsApiService {
  private readonly http = inject(HttpClient);

  listEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(`${environment.apiBase}/employees`);
  }

  createEmployee(employee: CreateEmployee): Observable<Employee> {
    return this.http.post<Employee>(`${environment.apiBase}/employees`, employee);
  }

  deleteEmployee(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBase}/employees/${id}`);
  }

  listPlans(): Observable<Plan[]> {
    return this.http.get<Plan[]>(`${environment.apiBase}/plans`);
  }

  entitlements(): Observable<Entitlements> {
    return this.http.get<Entitlements>(`${environment.apiBase}/subscriptions/entitlements`);
  }

  changePlan(planCode: string, billingCycle: number): Observable<unknown> {
    return this.http.put(`${environment.apiBase}/subscriptions/plan`, { planCode, billingCycle });
  }

  subscribe(planCode: string, billingCycle: number, startTrial: boolean): Observable<unknown> {
    return this.http.post(`${environment.apiBase}/subscriptions`, { planCode, billingCycle, startTrial });
  }
}
