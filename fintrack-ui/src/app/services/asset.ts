import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Asset, AddAssetRequest, UpdateAssetRequest } from '../models/models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AssetService {
  private url = `${environment.apiBaseUrl}/assets`;

  constructor(private http: HttpClient) {}

  getAssets(): Observable<Asset[]> {
    return this.http.get<Asset[]>(`${this.url}/list`);
  }

  getAssetById(id: string): Observable<Asset> {
    return this.http.get<Asset>(`${this.url}/detail/${id}`);
  }

  addAsset(request: AddAssetRequest): Observable<Asset> {
    return this.http.post<Asset>(`${this.url}/add`, request);
  }

  updateAsset(id: string, request: UpdateAssetRequest): Observable<Asset> {
    return this.http.put<Asset>(`${this.url}/edit/${id}`, request);
  }

  deleteAsset(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/remove/${id}`);
  }
}
