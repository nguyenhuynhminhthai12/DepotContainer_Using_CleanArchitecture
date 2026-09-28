/**
 * Dịch vụ quản lý Container (Container Service) cho ứng dụng Angular.
 * Cung cấp các phương thức CRUD cho Container Master Data:
 * - Liệt kê containers (có phân trang và bộ lọc theo hãng tàu, tình trạng, từ khóa)
 * - Lấy thông tin chi tiết container theo số hiệu (Container Number ISO 6346)
 * - Tạo mới / Cập nhật / Xóa container
 * - Liệt kê danh mục các loại container (Container Types)
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Container, ContainerType, PagedResult } from '../models/api.models';

/**
 * Service quản lý dữ liệu chủ của thùng hàng Container (Master Data).
 */
@Injectable({ providedIn: 'root' })
export class ContainerService {
  /**
   * Khởi tạo ContainerService.
   * @param http Client gửi các HTTP request lên máy chủ Backend.
   */
  constructor(private readonly http: HttpClient) {}

  /**
   * Lấy danh sách thùng hàng Container có hỗ trợ phân trang và tìm kiếm/lọc.
   * @param page Số trang hiện tại (mặc định: 1).
   * @param pageSize Số lượng dòng trên một trang (mặc định: 20).
   * @param lineOperatorId Lọc theo hãng tàu sở hữu (tùy chọn).
   * @param condition Lọc theo tình trạng vật lý (Normal, Damaged, Dented...) (tùy chọn).
   * @param search Từ khóa tìm kiếm theo số hiệu container (tùy chọn).
   * @returns Observable phát ra kết quả phân trang PagedResult chứa danh sách Container.
   */
  list(page = 1, pageSize = 20, lineOperatorId?: string, condition?: string, search?: string): Observable<PagedResult<Container>> {
    let p = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (lineOperatorId) p = p.set('lineOperatorId', lineOperatorId);
    if (condition) p = p.set('condition', condition);
    if (search) p = p.set('search', search);
    return this.http.get<PagedResult<Container>>('/api/containers', { params: p });
  }

  /**
   * Tìm kiếm thông tin chi tiết của container theo số hiệu ISO (VD: MSCU1234567).
   * @param number Số hiệu container cần tra cứu.
   * @returns Observable phát ra thông tin chi tiết của Container.
   */
  getByNumber(number: string): Observable<Container> {
    return this.http.get<Container>(`/api/containers/${encodeURIComponent(number)}`);
  }

  /**
   * Tạo mới một thùng hàng container vào cơ sở dữ liệu.
   * @param req Dữ liệu container mới (không cần truyền id và tenantId, backend tự sinh).
   * @returns Observable phát ra Container vừa được tạo thành công.
   */
  create(req: Omit<Container, 'id' | 'tenantId'>): Observable<Container> {
    return this.http.post<Container>('/api/containers', req);
  }

  /**
   * Cập nhật thông tin của một thùng hàng container hiện có.
   * @param id Mã định danh duy nhất (GUID) của container cần sửa.
   * @param req Các trường dữ liệu cần cập nhật.
   * @returns Observable phát ra Container sau khi cập nhật.
   */
  update(id: string, req: Partial<Container>): Observable<Container> {
    return this.http.put<Container>(`/api/containers/${id}`, { id, ...req });
  }

  /**
   * Xóa một container khỏi hệ thống.
   * @param id Mã định danh (GUID) của container cần xóa.
   * @returns Observable hoàn thành khi xóa thành công.
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/containers/${id}`);
  }

  /**
   * Lấy danh mục tất cả các loại container trong hệ thống (VD: 20DC, 40HC, 40RF...).
   * @returns Observable phát ra danh sách các ContainerType.
   */
  listTypes(): Observable<ContainerType[]> {
    return this.http.get<ContainerType[]>('/api/lookups/container-types');
  }
}
