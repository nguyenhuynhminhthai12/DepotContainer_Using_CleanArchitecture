/**
 * Dịch vụ quản lý Yard (Yard Service) cho ứng dụng Angular.
 * Cung cấp các phương thức:
 * - Liệt kê các Depot (kho bãi)
 * - Lấy sơ đồ Yard Map (Block + Slot)
 * - Tạo Block vật lý và Block ảo (Virtual Block)
 * - Resize Block (thay đổi kích thước Bay × Row × Tier)
 * - Cập nhật và xóa Block
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Block, YardMapDto, BlockWithSlots } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class YardService {
  constructor(private readonly http: HttpClient) {}

  listDepots(): Observable<{ id: string; code: string; name: string; address?: string; isVirtual?: boolean }[]> {
    return this.http.get<{ id: string; code: string; name: string; address?: string; isVirtual?: boolean }[]>('/api/yard/depots');
  }

  getYardMap(depotId: string): Observable<YardMapDto> {
    return this.http.get<YardMapDto>(`/api/yard/depots/${depotId}/map`);
  }

  createBlock(req: { depotId: string; code: string; name: string; maxBay: number; maxRow: number; maxTier: number; }): Observable<BlockWithSlots> {
    return this.http.post<BlockWithSlots>('/api/blocks', req);
  }

  createVirtualBlock(req: { depotId: string; code: string; name: string; }): Observable<BlockWithSlots> {
    return this.http.post<BlockWithSlots>('/api/blocks/virtual', req);
  }

  resizeBlock(id: string, maxBay: number, maxRow: number, maxTier: number): Observable<void> {
    return this.http.patch<void>(`/api/blocks/${id}/resize`, { maxBay, maxRow, maxTier });
  }

  updateBlock(id: string, req: { code: string; name: string }): Observable<BlockWithSlots> {
    return this.http.put<BlockWithSlots>(`/api/blocks/${id}`, req);
  }

  deleteBlock(id: string): Observable<void> {
    return this.http.delete<void>(`/api/blocks/${id}`);
  }
}
