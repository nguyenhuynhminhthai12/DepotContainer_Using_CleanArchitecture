/**
 * Dịch vụ quản lý Đơn Giao hàng (Delivery Order Service) cho ứng dụng Angular.
 * Cung cấp các phương thức CRUD cho Đơn giao hàng (D/O), Khách hàng (Customer), và Hãng tàu (Line Operator).
 * Đơn giao hàng (Delivery Order / Lệnh xuất hàng) là chứng từ cho phép depot xuất container cho chủ hàng/tài xế.
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Customer, DeliveryOrder, LineOperator, CreateDeliveryOrderRequest } from '../models/api.models';

/**
 * Service quản lý Lệnh giao nhận / Đơn giao hàng (Delivery Orders - D/O) và dữ liệu danh mục liên quan.
 */
@Injectable({ providedIn: 'root' })
export class DeliveryOrderService {
  /**
   * Khởi tạo DeliveryOrderService.
   * @param http Client gửi các HTTP request lên máy chủ Backend.
   */
  constructor(private readonly http: HttpClient) {}

  /**
   * Lấy danh sách tất cả các đơn giao hàng đang còn hiệu lực và chưa hoàn thành (Active D/O).
   * @returns Observable phát ra danh sách các DeliveryOrder.
   */
  list(): Observable<DeliveryOrder[]> {
    return this.http.get<DeliveryOrder[]>('/api/delivery-orders/active');
  }

  /**
   * Lấy thông tin chi tiết của một đơn giao hàng theo mã định danh (GUID).
   * @param id Mã định danh của DeliveryOrder cần tra cứu.
   * @returns Observable phát ra đối tượng DeliveryOrder kèm các dòng chi tiết lines.
   */
  get(id: string): Observable<DeliveryOrder> {
    return this.http.get<DeliveryOrder>(`/api/delivery-orders/${id}`);
  }

  /**
   * Tạo mới một Đơn giao hàng (D/O) kèm các dòng phân bổ số lượng container.
   * @param req Thông tin đơn: Số D/O, Khách hàng, Hãng tàu, Ngày hết hạn, Danh sách loại container và số lượng.
   * @returns Observable phát ra DeliveryOrder vừa được tạo.
   */
  create(req: CreateDeliveryOrderRequest): Observable<DeliveryOrder> {
    return this.http.post<DeliveryOrder>('/api/delivery-orders', req);
  }

  /**
   * Cập nhật thông tin đơn giao hàng hiện có.
   * @param id Mã định danh của DeliveryOrder cần sửa.
   * @param req Dữ liệu cập nhật.
   * @returns Observable phát ra DeliveryOrder sau khi sửa.
   */
  update(id: string, req: Partial<CreateDeliveryOrderRequest>): Observable<DeliveryOrder> {
    return this.http.put<DeliveryOrder>(`/api/delivery-orders/${id}`, req);
  }

  /**
   * Đóng đơn giao hàng (Đánh dấu đã hoàn thành hoặc hủy hiệu lực xuất container).
   * @param id Mã định danh của DeliveryOrder cần đóng.
   * @returns Observable hoàn thành khi đóng đơn thành công.
   */
  close(id: string): Observable<void> {
    return this.http.post<void>(`/api/delivery-orders/${id}/close`, {});
  }

  /**
   * Xóa một đơn giao hàng khỏi hệ thống.
   * @param id Mã định danh của DeliveryOrder cần xóa.
   * @returns Observable hoàn thành khi xóa thành công.
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/delivery-orders/${id}`);
  }

  /**
   * Lấy danh mục tất cả Khách hàng / Chủ hàng (Customers) đã đăng ký trong hệ thống.
   * @returns Observable phát ra danh sách Customer.
   */
  customers(): Observable<Customer[]> {
    return this.http.get<Customer[]>('/api/lookups/customers');
  }

  /**
   * Tạo mới một khách hàng / doanh nghiệp vận tải vào danh mục.
   * @param req Thông tin khách hàng gồm Tên, Mã số thuế, Địa chỉ, Số điện thoại, Email.
   * @returns Observable phát ra Customer vừa được tạo.
   */
  createCustomer(req: { 
    /** Tên khách hàng / Công ty */
    name: string; 
    /** Mã số thuế */
    taxCode: string; 
    /** Địa chỉ trụ sở (tùy chọn) */
    address?: string; 
    /** Số điện thoại liên hệ (tùy chọn) */
    phone?: string; 
    /** Địa chỉ email (tùy chọn) */
    email?: string 
  }): Observable<Customer> {
    return this.http.post<Customer>('/api/lookups/customers', req);
  }

  /**
   * Lấy danh mục các Hãng tàu / Đơn vị khai thác (Line Operators).
   * @returns Observable phát ra danh sách LineOperator.
   */
  lineOperators(): Observable<LineOperator[]> {
    return this.http.get<LineOperator[]>('/api/lookups/line-operators');
  }

  /**
   * Lấy danh mục các loại container phục vụ tạo dòng chi tiết đơn giao hàng.
   * @returns Observable phát ra danh sách mã và tên các loại container.
   */
  containerTypes(): Observable<{ 
    /** Mã định danh loại container (GUID) */
    id: string; 
    /** Mã viết tắt (VD: 20DC, 40HC) */
    code: string; 
    /** Tên đầy đủ của loại container */
    name: string 
  }[]> {
    return this.http.get<{ id: string; code: string; name: string }[]>('/api/lookups/container-types');
  }
}
