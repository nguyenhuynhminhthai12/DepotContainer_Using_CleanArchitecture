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

  update(id: string, req: any): Observable<DeliveryOrder> {
    return this.http.put<DeliveryOrder>(`/api/delivery-orders/${id}`, req);
  }

  close(id: string): Observable<void> {
    return this.http.post<void>(`/api/delivery-orders/${id}/close`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/delivery-orders/${id}`);
  }

  customers(): Observable<Customer[]> {
    return this.http.get<Customer[]>('/api/lookups/customers');
  }

  createCustomer(req: { name: string; taxCode: string; address?: string; phone?: string; email?: string }): Observable<Customer> {
    return this.http.post<Customer>('/api/lookups/customers', req);
  }

  lineOperators(): Observable<LineOperator[]> {
    return this.http.get<LineOperator[]>('/api/lookups/line-operators');
  }

  containerTypes(): Observable<{ id: string; code: string; name: string }[]> {
    return this.http.get<{ id: string; code: string; name: string }[]>('/api/lookups/container-types');
  }
}
