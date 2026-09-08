import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AgentMessageDto {
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp?: string | Date;
}

export interface AgentExecuteRequest {
  prompt: string;
  parameters?: Record<string, unknown>;
  history?: AgentMessageDto[];
}

export interface AgentExecuteResponse {
  status: string;
  message: string;
  data?: unknown;
  metadata?: Record<string, unknown>;
}

export interface SkillInfo {
  skillId: string;
  name: string;
  description: string;
  examplePrompts: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AgentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/agents';

  execute(prompt: string, history?: AgentMessageDto[], parameters?: Record<string, unknown>): Observable<AgentExecuteResponse> {
    const payload: AgentExecuteRequest = {
      prompt,
      parameters,
      history
    };
    return this.http.post<AgentExecuteResponse>(`${this.baseUrl}/execute`, payload);
  }

  executeSkill(skillId: string, prompt: string, parameters?: Record<string, unknown>): Observable<AgentExecuteResponse> {
    const payload: AgentExecuteRequest = {
      prompt,
      parameters
    };
    return this.http.post<AgentExecuteResponse>(`${this.baseUrl}/execute/${skillId}`, payload);
  }

  listSkills(): Observable<SkillInfo[]> {
    return this.http.get<SkillInfo[]>(`${this.baseUrl}/skills`);
  }
}
