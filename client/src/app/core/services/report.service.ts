import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DailyThroughputReport, YardAgingReport } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ReportService {
  constructor(private readonly http: HttpClient) {}

  yardAging(): Observable<YardAgingReport> {
    return this.http.get<YardAgingReport>('/api/reports/yard-aging');
  }

  dailyThroughput(from?: string, to?: string): Observable<DailyThroughputReport> {
    let p = new HttpParams();
    if (from) p = p.set('from', from);
    if (to) p = p.set('to', to);
    return this.http.get<DailyThroughputReport>('/api/reports/daily-throughput', { params: p });
  }
}
