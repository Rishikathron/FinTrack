import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Asset, AddAssetRequest } from '../models/models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AssetService {
  private url = `${environment.apiBaseUrl}/assets`;

  constructor(private http: HttpClient) {}

  getAssets(): Observable<Asset[]> {
    return this.http.get<Asset[]>(this.url);
  }

  addAsset(request: AddAssetRequest): Observable<Asset> {
    return this.http.post<Asset>(this.url, request);
  }

  deleteAsset(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
