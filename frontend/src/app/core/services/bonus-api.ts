import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BonusFilter, BonusListItem, CreateBonusRequest, Employee } from '../models/bonus';
import { PagedResponse } from '../models/paged-response';

@Injectable({ providedIn: 'root' })
export class BonusApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/Bonus`;

  getBonuses(filter: BonusFilter): Observable<PagedResponse<BonusListItem>> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filter)) {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, value);
      }
    }
    return this.http.get<PagedResponse<BonusListItem>>(this.baseUrl, { params });
  }

  getEmployee(departmentCode: string, employeeNumber: number): Observable<Employee> {
    return this.http.get<Employee>(`${this.baseUrl}/employee/${departmentCode}/${employeeNumber}`);
  }

  create(request: CreateBonusRequest): Observable<number> {
    return this.http.post<number>(this.baseUrl, request);
  }

  cancel(bonusId: number): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${bonusId}/cancel`, null);
  }

  delete(bonusId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${bonusId}`);
  }
}
