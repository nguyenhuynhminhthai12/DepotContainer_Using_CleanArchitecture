/**
 * Dịch vụ quản lý Yard (Yard Service) cho ứng dụng Angular.
 * Cung cấp các phương thức tương tác với sơ đồ và cấu trúc bãi container:
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
import { YardMapDto, BlockWithSlots } from '../models/api.models';

/**
 * Service quản lý sơ đồ và phân khu bãi container (Depots, Blocks, Yard Slots).
 */
@Injectable({ providedIn: 'root' })
export class YardService {
  /**
   * Khởi tạo YardService.
   * @param http Client gửi các HTTP request lên máy chủ Backend.
   */
  constructor(private readonly http: HttpClient) {}

  /**
   * Lấy danh sách tất cả các Depot / ICD / Cảng trong hệ thống.
   * @returns Observable phát ra danh sách thông tin cơ bản của các Depot.
   */
  listDepots(): Observable<{ 
    /** Mã định danh Depot (GUID) */
    id: string; 
    /** Mã viết tắt của Depot */
    code: string; 
    /** Tên đầy đủ của Depot */
    name: string; 
    /** Địa chỉ bãi (nếu có) */
    address?: string; 
    /** Đánh dấu bãi ảo */
    isVirtual?: boolean 
  }[]> {
    return this.http.get<{ id: string; code: string; name: string; address?: string; isVirtual?: boolean }[]>('/api/yard/depots');
  }

  /**
   * Lấy toàn bộ sơ đồ phân khu và các ô Slot của một Depot cụ thể.
   * @param depotId Mã định danh của Depot cần lấy sơ đồ.
   * @returns Observable phát ra cấu trúc YardMapDto bao gồm tất cả các Block và trạng thái Slot.
   */
  getYardMap(depotId: string): Observable<YardMapDto> {
    return this.http.get<YardMapDto>(`/api/yard/depots/${depotId}/map`);
  }

  /**
   * Tạo mới một Block vật lý với kích thước lưới xác định (Bay × Row × Tier).
   * Hệ thống Backend sẽ tự động sinh ra toàn bộ các ô YardSlot tương ứng.
   * @param req Thông tin Block cần tạo: Depot, Mã, Tên, Số Bay, Số Row, Số Tier tối đa.
   * @returns Observable phát ra thông tin Block vừa tạo kèm danh sách các ô Slot.
   */
  createBlock(req: { 
    /** Mã Depot chứa Block */
    depotId: string; 
    /** Mã viết tắt của Block (VD: A, B1) */
    code: string; 
    /** Tên hiển thị */
    name: string; 
    /** Số lượng Bay tối đa */
    maxBay: number; 
    /** Số lượng Row tối đa */
    maxRow: number; 
    /** Số lượng Tier tối đa */
    maxTier: number; 
  }): Observable<BlockWithSlots> {
    return this.http.post<BlockWithSlots>('/api/blocks', req);
  }

  /**
   * Tạo một Block ảo (Virtual Block) dành cho khu vực chờ, trạm sửa chữa, không chia ô cố định.
   * @param req Thông tin Block ảo cần tạo.
   * @returns Observable phát ra thông tin Block ảo vừa tạo.
   */
  createVirtualBlock(req: { 
    /** Mã Depot chứa Block */
    depotId: string; 
    /** Mã viết tắt của Block */
    code: string; 
    /** Tên hiển thị */
    name: string; 
  }): Observable<BlockWithSlots> {
    return this.http.post<BlockWithSlots>('/api/blocks/virtual', req);
  }

  /**
   * Thay đổi kích thước (Resize) của Block (Mở rộng hoặc thu hẹp số Bay, Row, Tier).
   * @param id Mã định danh của Block cần thay đổi kích thước.
   * @param maxBay Số Bay mới.
   * @param maxRow Số Row mới.
   * @param maxTier Số Tier mới.
   * @returns Observable hoàn thành khi cập nhật thành công.
   */
  resizeBlock(id: string, maxBay: number, maxRow: number, maxTier: number): Observable<void> {
    return this.http.patch<void>(`/api/blocks/${id}/resize`, { maxBay, maxRow, maxTier });
  }

  /**
   * Cập nhật thông tin mã và tên của một Block.
   * @param id Mã định danh của Block cần sửa.
   * @param req Dữ liệu mã và tên mới.
   * @returns Observable phát ra Block sau khi cập nhật.
   */
  updateBlock(id: string, req: { 
    /** Mã mới của Block */
    code: string; 
    /** Tên mới của Block */
    name: string 
  }): Observable<BlockWithSlots> {
    return this.http.put<BlockWithSlots>(`/api/blocks/${id}`, req);
  }

  /**
   * Xóa một Block khỏi sơ đồ bãi.
   * @param id Mã định danh của Block cần xóa.
   * @returns Observable hoàn thành khi xóa thành công trên Backend.
   */
  deleteBlock(id: string): Observable<void> {
    return this.http.delete<void>(`/api/blocks/${id}`);
  }
}
