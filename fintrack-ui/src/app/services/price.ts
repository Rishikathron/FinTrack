import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MetalPrices } from '../models/models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PriceService {
  private url = `${environment.apiBaseUrl}/prices`;

  constructor(private http: HttpClient) {}

  getCurrentPrices(): Observable<MetalPrices> {
    return this.http.get<MetalPrices>(`${this.url}/current`);
  }
}
