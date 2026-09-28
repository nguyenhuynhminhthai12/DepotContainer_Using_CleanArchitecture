/**
 * Dịch vụ thao tác Cổng (Gate Service) cho ứng dụng Angular.
 * Cung cấp các phương thức giao tiếp với API Cổng (Gate):
 * - Gate-In: Tạo biên nhận EIR khi container vào bãi
 * - Gate-Out: Xác nhận xuất container dựa trên Đơn giao hàng
 * - Move: Di chuyển container trong nội bộ bãi (thay đổi vị trí Yard Slot)
 * - GetHistory: Lấy lịch sử di chuyển container
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ContainerMovement, GateInRequest, GateOutRequest } from '../models/api.models';

/**
 * Service quản lý các hoạt động tại cổng Depot (Check-in, Check-out, Di chuyển bãi, Lịch sử).
 */
@Injectable({ providedIn: 'root' })
export class GateService {
  /**
   * Khởi tạo GateService với HttpClient của Angular.
   * @param http Client gửi các HTTP request lên máy chủ Backend.
   */
  constructor(private readonly http: HttpClient) {}

  /**
   * Thực hiện thủ tục Nhập cổng (Gate-In) cho container vào bãi Depot.
   * Tạo bản ghi ContainerMovement (EIR) và phân bổ vị trí Yard Slot tương ứng.
   * @param req Dữ liệu thông tin xe, tài xế, mã container và vị trí hạ bãi.
   * @returns Observable phát ra thông tin chi tiết của lượt di chuyển ContainerMovement vừa tạo.
   */
  gateIn(req: GateInRequest): Observable<ContainerMovement> {
    return this.http.post<ContainerMovement>('/api/gate/in', req);
  }

  /**
   * Thực hiện thủ tục Xuất cổng (Gate-Out) cho container ra khỏi bãi Depot.
   * Kiểm tra và khấu trừ số lượng container vào Lệnh giao nhận (Delivery Order).
   * @param req Dữ liệu thông tin xe nhận, tài xế, mã container và mã D/O.
   * @returns Observable phát ra thông tin cập nhật của lượt di chuyển ContainerMovement.
   */
  gateOut(req: GateOutRequest): Observable<ContainerMovement> {
    return this.http.post<ContainerMovement>('/api/gate/out', req);
  }

  /**
   * Thực hiện di chuyển container sang vị trí ô (Yard Slot) mới trong nội bộ bãi.
   * Giải phóng ô cũ và đánh dấu ô mới là đã bị chiếm dụng (Occupied).
   * @param req Đối tượng chứa số container và tọa độ đích (Block, Bay, Row, Tier).
   * @returns Observable hoàn thành khi thao tác di chuyển thành công trên Backend.
   */
  move(req: { 
    /** Số hiệu container cần di chuyển */
    containerNumber: string; 
    /** Mã định danh Block đích */
    newBlockId: string; 
    /** Số Bay mới */
    newBay: number; 
    /** Số Row mới */
    newRow: number; 
    /** Số Tier mới */
    newTier: number; 
  }): Observable<void> {
    return this.http.post<void>('/api/gate/move', req);
  }

  /**
   * Lấy toàn bộ lịch sử các lần di chuyển, vào bãi, xuất bãi của một container.
   * @param containerNumber Số hiệu container cần tra cứu lịch sử (VD: MSCU1234567).
   * @returns Observable phát ra danh sách các lượt di chuyển ContainerMovement theo thứ tự thời gian.
   */
  getHistory(containerNumber: string): Observable<ContainerMovement[]> {
    const p = new HttpParams().set('containerNumber', containerNumber);
    return this.http.get<ContainerMovement[]>(`/api/containers/${encodeURIComponent(containerNumber)}/movements`, { params: p });
  }
}
