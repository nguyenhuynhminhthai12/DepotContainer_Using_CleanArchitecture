import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService } from '../../core/services/report.service';
import { DailyThroughputReport, YardAgingReport } from '../../core/models/api.models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule],
  template: `
    <h2>Reports</h2>
    <p class="muted">Yard aging (0-10 / ≥10 days) and daily gate throughput by Line Operator.</p>

    <section class="card">
      <header><strong>Yard Aging</strong> <span class="muted">— as of {{ yardAging()?.asOf | date:'medium' }}</span></header>
      <table>
        <thead>
          <tr>
            <th>Line Operator</th>
            <th>0–10 days</th>
            <th>≥ 10 days (long stay)</th>
            <th>Total</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of yardAging()?.rows">
            <td>{{ r.lineOperatorCode }} — {{ r.lineOperatorName }}</td>
            <td>{{ r.buckets.withinTenDays }}</td>
            <td class="warn">{{ r.buckets.tenDaysOrMore }}</td>
            <td>{{ r.buckets.withinTenDays + r.buckets.tenDaysOrMore }}</td>
          </tr>
        </tbody>
      </table>
    </section>

    <section class="card">
      <header><strong>Daily Throughput</strong></header>
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Line Operator</th>
            <th>Gate In</th>
            <th>Gate Out</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let r of throughput()?.rows">
            <td>{{ r.date }}</td>
            <td>{{ r.lineOperatorCode }}</td>
            <td>{{ r.gateIn }}</td>
            <td>{{ r.gateOut }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  `,
  styles: [`
    section.card { margin-bottom: 16px; }
    header { margin-bottom: 8px; }
    .warn { color: var(--color-warning); font-weight: 600; }
  `],
})
export class ReportsComponent implements OnInit {
  private svc = inject(ReportService);
  yardAging = signal<YardAgingReport | null>(null);
  throughput = signal<DailyThroughputReport | null>(null);

  ngOnInit(): void {
    this.svc.yardAging().subscribe((r) => this.yardAging.set(r));
    this.svc.dailyThroughput().subscribe((r) => this.throughput.set(r));
  }
}
