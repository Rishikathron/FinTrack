import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { NetWorthSummary, AssetValuation } from '../models/models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ValuationService {
  private url = `${environment.apiBaseUrl}/valuation`;

  constructor(private http: HttpClient) {}

  getNetWorth(): Observable<NetWorthSummary> {
    return this.http.get<NetWorthSummary>(`${this.url}/networth`);
  }

  getBreakdown(): Observable<AssetValuation[]> {
    return this.http.get<AssetValuation[]>(`${this.url}/breakdown`);
  }
}
