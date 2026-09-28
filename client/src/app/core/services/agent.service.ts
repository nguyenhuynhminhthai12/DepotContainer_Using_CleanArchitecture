/**
 * Dịch vụ Trợ lý AI (Agent Service) cho ứng dụng Angular.
 * Cung cấp các phương thức giao tiếp với AI Agent của Depot:
 * - Thực thi câu lệnh tự nhiên (execute)
 * - Chạy kỹ năng chuyên biệt (executeSkill)
 * - Liệt kê các kỹ năng sẵn có (listSkills)
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AgentExecuteRequest, AgentExecuteResponse, AgentMessageDto, SkillInfo } from '../models/api.models';

/**
 * Service tương tác với AI Assistant Agent trên Backend để xử lý ngôn ngữ tự nhiên và tra cứu dữ liệu bãi.
 */
@Injectable({
  providedIn: 'root'
})
export class AgentService {
  /** Client gửi các HTTP request */
  private readonly http = inject(HttpClient);
  /** Đường dẫn API Agent */
  private readonly baseUrl = '/api/agents';

  /**
   * Gửi một câu lệnh tự nhiên cho Trợ lý AI xử lý kèm lịch sử hội thoại.
   * @param prompt Câu hỏi hoặc yêu cầu của người dùng (VD: 'Tìm 5 slot trống ở Block A').
   * @param history Lịch sử các tin nhắn trước đó để AI duy trì ngữ cảnh trò chuyện.
   * @param parameters Các tham số bổ sung truyền vào cho AI (tùy chọn).
   * @returns Observable phát ra AgentExecuteResponse chứa câu trả lời và dữ liệu tra cứu.
   */
  execute(prompt: string, history?: AgentMessageDto[], parameters?: Record<string, unknown>): Observable<AgentExecuteResponse> {
    const payload: AgentExecuteRequest = {
      prompt,
      parameters,
      history
    };
    return this.http.post<AgentExecuteResponse>(`${this.baseUrl}/execute`, payload);
  }

  /**
   * Chạy trực tiếp một Kỹ năng cụ thể của AI Agent theo mã định danh (Skill ID).
   * @param skillId Mã kỹ năng (VD: 'depot-query', 'yard-optimization').
   * @param prompt Lời nhắc câu lệnh.
   * @param parameters Các tham số đầu vào cho kỹ năng.
   * @returns Observable phát ra kết quả thực thi của kỹ năng.
   */
  executeSkill(skillId: string, prompt: string, parameters?: Record<string, unknown>): Observable<AgentExecuteResponse> {
    const payload: AgentExecuteRequest = {
      prompt,
      parameters
    };
    return this.http.post<AgentExecuteResponse>(`${this.baseUrl}/execute/${skillId}`, payload);
  }

  /**
   * Lấy danh sách tất cả các Kỹ năng AI mà hệ thống hỗ trợ.
   * @returns Observable phát ra danh sách thông tin kỹ năng SkillInfo kèm ví dụ câu lệnh.
   */
  listSkills(): Observable<SkillInfo[]> {
    return this.http.get<SkillInfo[]>(`${this.baseUrl}/skills`);
  }
}
