import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Customer, DeliveryOrder, LineOperator, CreateDeliveryOrderRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class DeliveryOrderService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<DeliveryOrder[]> {
    return this.http.get<DeliveryOrder[]>('/api/delivery-orders/active');
  }

  get(id: string): Observable<DeliveryOrder> {
    return this.http.get<DeliveryOrder>(`/api/delivery-orders/${id}`);
  }

  create(req: CreateDeliveryOrderRequest): Observable<DeliveryOrder> {
    return this.http.post<DeliveryOrder>('/api/delivery-orders', req);
  }

  close(id: string): Observable<void> {
    return this.http.post<void>(`/api/delivery-orders/${id}/close`, {});
  }

  customers(): Observable<Customer[]> {
    return this.http.get<Customer[]>('/api/lookups/customers');
  }

  lineOperators(): Observable<LineOperator[]> {
    return this.http.get<LineOperator[]>('/api/lookups/line-operators');
  }

  containerTypes(): Observable<{ id: string; code: string; name: string }[]> {
    return this.http.get<{ id: string; code: string; name: string }[]>('/api/lookups/container-types');
  }
}
