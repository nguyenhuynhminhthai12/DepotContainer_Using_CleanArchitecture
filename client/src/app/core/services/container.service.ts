/**
 * Dịch vụ quản lý Container (Container Service) cho ứng dụng Angular.
 * Cung cấp các phương thức CRUD cho Container Master Data:
 * - Liệt kê containers (có phân trang và bộ lọc)
 * - Lấy container theo số (Container Number ISO 6346)
 * - Tạo / cập nhật / xóa container
 * - Liệt kê các loại container (Container Types)
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Container, ContainerType, PagedResult } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ContainerService {
  constructor(private readonly http: HttpClient) {}

  list(page = 1, pageSize = 20, lineOperatorId?: string, condition?: string, search?: string): Observable<PagedResult<Container>> {
    let p = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (lineOperatorId) p = p.set('lineOperatorId', lineOperatorId);
    if (condition) p = p.set('condition', condition);
    if (search) p = p.set('search', search);
    return this.http.get<PagedResult<Container>>('/api/containers', { params: p });
  }

  getByNumber(number: string): Observable<Container> {
    return this.http.get<Container>(`/api/containers/${encodeURIComponent(number)}`);
  }

  create(req: Omit<Container, 'id' | 'tenantId'>): Observable<Container> {
    return this.http.post<Container>('/api/containers', req);
  }

  update(id: string, req: Partial<Container>): Observable<Container> {
    return this.http.put<Container>(`/api/containers/${id}`, { id, ...req });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/containers/${id}`);
  }

  listTypes(): Observable<ContainerType[]> {
    return this.http.get<ContainerType[]>('/api/lookups/container-types');
  }
}
