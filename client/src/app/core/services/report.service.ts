/**
 * Dịch vụ báo cáo (Report Service) cho ứng dụng Angular.
 * Cung cấp các phương thức lấy dữ liệu báo cáo:
 * - Yard Aging Report: Phân tích thời gian lưu trữ container trong bãi (0-10 ngày / ≥10 ngày)
 * - Daily Throughput Report: Thông lượng cổng theo ngày, phân nhóm theo Line Operator
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DailyThroughputReport, YardAgingReport } from '../models/api.models';

/**
 * Service trích xuất dữ liệu báo cáo thống kê bãi và sản lượng xuất nhập cổng.
 */
@Injectable({ providedIn: 'root' })
export class ReportService {
  /**
   * Khởi tạo ReportService.
   * @param http Client gửi các HTTP request lên máy chủ Backend.
   */
  constructor(private readonly http: HttpClient) {}

  /**
   * Lấy dữ liệu báo cáo tuổi container tồn bãi (Yard Aging Report) theo từng hãng tàu.
   * @returns Observable phát ra YardAgingReport phân loại container <10 ngày và ≥10 ngày.
   */
  yardAging(): Observable<YardAgingReport> {
    return this.http.get<YardAgingReport>('/api/reports/yard-aging');
  }

  /**
   * Lấy báo cáo sản lượng xuất nhập cổng hàng ngày (Daily Throughput Report) theo khoảng ngày.
   * @param from Ngày bắt đầu thống kê (định dạng YYYY-MM-DD, tùy chọn).
   * @param to Ngày kết thúc thống kê (định dạng YYYY-MM-DD, tùy chọn).
   * @returns Observable phát ra DailyThroughputReport chứa số lượt Gate-In và Gate-Out theo từng ngày.
   */
  dailyThroughput(from?: string, to?: string): Observable<DailyThroughputReport> {
    let p = new HttpParams();
    if (from) p = p.set('from', from);
    if (to) p = p.set('to', to);
    return this.http.get<DailyThroughputReport>('/api/reports/daily-throughput', { params: p });
  }
}
