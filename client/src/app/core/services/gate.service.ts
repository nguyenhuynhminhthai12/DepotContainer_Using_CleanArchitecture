import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ContainerMovement, GateInRequest, GateOutRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class GateService {
  constructor(private readonly http: HttpClient) {}

  gateIn(req: GateInRequest): Observable<ContainerMovement> {
    return this.http.post<ContainerMovement>('/api/gate/in', req);
  }

  gateOut(req: GateOutRequest): Observable<ContainerMovement> {
    return this.http.post<ContainerMovement>('/api/gate/out', req);
  }

  move(req: { containerNumber: string; newBlockId: string; newBay: number; newRow: number; newTier: number; }): Observable<void> {
    return this.http.post<void>('/api/gate/move', req);
  }

  getHistory(containerNumber: string): Observable<ContainerMovement[]> {
    const p = new HttpParams().set('containerNumber', containerNumber);
    return this.http.get<ContainerMovement[]>(`/api/containers/${encodeURIComponent(containerNumber)}/movements`, { params: p });
  }
}
