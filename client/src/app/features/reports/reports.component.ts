import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReportService } from '../../core/services/report.service';
import { DailyThroughputReport, YardAgingReport } from '../../core/models/api.models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="page-header">
      <div>
        <h2>Operational Reports & Analytics</h2>
        <p class="muted">Live yard dwell aging (0–10 / ≥10 days long stay) and daily gate throughput by Line Operator.</p>
      </div>
      <div class="header-actions">
        <button (click)="refresh()" class="secondary">🔄 Refresh Metrics</button>
        <button (click)="exportCsv()" class="secondary">📥 Export CSV</button>
      </div>
    </div>

    <!-- KPI Cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #eff6ff; color: #2563eb;">📦</div>
        <div class="kpi-info">
          <span class="kpi-label">Total In Yard</span>
          <span class="kpi-value">{{ totalInYard() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #ecfdf5; color: #059669;">⏱️</div>
        <div class="kpi-info">
          <span class="kpi-label">Within 10 Days</span>
          <span class="kpi-value">{{ totalWithinTenDays() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fef2f2; color: #dc2626;">⚠️</div>
        <div class="kpi-info">
          <span class="kpi-label">≥ 10 Days (Long Stay)</span>
          <span class="kpi-value" style="color: #dc2626;">{{ totalLongStay() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fdf4ff; color: #9333ea;">⚡</div>
        <div class="kpi-info">
          <span class="kpi-label">Total Gate Movements</span>
          <span class="kpi-value">{{ totalMovements() }}</span>
        </div>
      </div>
    </div>

    <!-- Yard Aging Table & Visual Distribution -->
    <section class="card report-section">
      <div class="section-title-bar">
        <div>
          <h3>⏳ Yard Aging & Dwell Time Analysis</h3>
          <span class="muted small">As of {{ yardAging()?.asOf | date:'medium' }}</span>
        </div>
        <span class="badge">Rule: ≥ 10 Days Demurrage / Long Stay Alert</span>
      </div>

      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Line Operator (Shipping Line)</th>
              <th>0–10 Days</th>
              <th>≥ 10 Days (Long Stay)</th>
              <th>Total Units</th>
              <th>Dwell Distribution</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of yardAging()?.rows">
              <td>
                <b>{{ r.lineOperatorCode }}</b> — <span class="muted">{{ r.lineOperatorName }}</span>
              </td>
              <td>
                <span class="badge badge-success">{{ r.buckets.withinTenDays }}</span>
              </td>
              <td>
                <span class="badge" [class.badge-danger]="r.buckets.tenDaysOrMore > 0" [class.badge-muted]="r.buckets.tenDaysOrMore === 0">
                  {{ r.buckets.tenDaysOrMore }}
                </span>
              </td>
              <td><b>{{ r.buckets.withinTenDays + r.buckets.tenDaysOrMore }}</b></td>
              <td style="min-width: 160px;">
                <div class="dist-bar">
                  <div
                    class="dist-segment dist-normal"
                    [style.width.%]="getAgingPercent(r.buckets.withinTenDays, r.buckets.withinTenDays + r.buckets.tenDaysOrMore)"
                    title="0-10 days"
                  ></div>
                  <div
                    class="dist-segment dist-warn"
                    [style.width.%]="getAgingPercent(r.buckets.tenDaysOrMore, r.buckets.withinTenDays + r.buckets.tenDaysOrMore)"
                    title="≥ 10 days"
                  ></div>
                </div>
              </td>
            </tr>
            <tr *ngIf="!yardAging()?.rows?.length">
              <td colspan="5" class="empty-state">
                <div class="empty-icon">📊</div>
                <div class="empty-text">No active yard inventory recorded.</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- Daily Throughput Table -->
    <section class="card report-section">
      <div class="section-title-bar">
        <div>
          <h3>🚛 Daily Gate Throughput Log</h3>
          <span class="muted small">Breakdown by Date and Shipping Line</span>
        </div>
      </div>

      <div class="table-container">
        <table>
          <thead>
            <tr>
              <th>Date</th>
              <th>Shipping Line</th>
              <th>📥 Gate In</th>
              <th>📤 Gate Out</th>
              <th>Net Flow</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let r of throughput()?.rows">
              <td><b>{{ r.date }}</b></td>
              <td><span class="badge badge-indigo">{{ r.lineOperatorCode }}</span></td>
              <td>
                <span class="badge badge-primary">📥 {{ r.gateIn }} in</span>
              </td>
              <td>
                <span class="badge badge-success">📤 {{ r.gateOut }} out</span>
              </td>
              <td>
                <span [class.text-success]="r.gateIn - r.gateOut > 0" [class.text-danger]="r.gateIn - r.gateOut < 0">
                  <b>{{ r.gateIn - r.gateOut > 0 ? '+' : '' }}{{ r.gateIn - r.gateOut }}</b>
                </span>
              </td>
            </tr>
            <tr *ngIf="!throughput()?.rows?.length">
              <td colspan="5" class="empty-state">
                <div class="empty-icon">🚛</div>
                <div class="empty-text">No gate transactions recorded for this period.</div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  `,
  styles: [`
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .header-actions { display: flex; gap: 8px; }

    .report-section {
      margin-bottom: 24px;
      padding: 20px;
    }
    .section-title-bar {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 16px;
    }
    .section-title-bar h3 { margin: 0; font-size: 16px; }

    .table-container {
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      overflow: hidden;
    }

    .badge-indigo { background: #eef2ff; color: #4f46e5; }
    .text-success { color: #16a34a; }
    .text-danger { color: #dc2626; }

    .dist-bar {
      height: 8px;
      width: 100%;
      background: #e2e8f0;
      border-radius: 999px;
      overflow: hidden;
      display: flex;
    }
    .dist-segment { height: 100%; }
    .dist-normal { background: #10b981; }
    .dist-warn { background: #ef4444; }

    .empty-state {
      text-align: center;
      padding: 30px !important;
    }
    .empty-icon { font-size: 28px; margin-bottom: 6px; }
    .empty-text { color: #64748b; font-size: 13px; }
  `],
})
export class ReportsComponent implements OnInit {
  private readonly svc = inject(ReportService);
  yardAging = signal<YardAgingReport | null>(null);
  throughput = signal<DailyThroughputReport | null>(null);

  totalWithinTenDays = computed(() =>
    this.yardAging()?.rows?.reduce((sum, r) => sum + r.buckets.withinTenDays, 0) ?? 0
  );
  totalLongStay = computed(() =>
    this.yardAging()?.rows?.reduce((sum, r) => sum + r.buckets.tenDaysOrMore, 0) ?? 0
  );
  totalInYard = computed(() => this.totalWithinTenDays() + this.totalLongStay());

  totalMovements = computed(() =>
    this.throughput()?.rows?.reduce((sum, r) => sum + r.gateIn + r.gateOut, 0) ?? 0
  );

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.svc.yardAging().subscribe((r) => this.yardAging.set(r));
    this.svc.dailyThroughput().subscribe((r) => this.throughput.set(r));
  }

  getAgingPercent(val: number, total: number): number {
    if (!total) return 0;
    return Math.round((val / total) * 100);
  }

  exportCsv(): void {
    const rows = this.yardAging()?.rows ?? [];
    let csv = 'LineOperatorCode,LineOperatorName,0_10_Days,Ten_Days_Or_More,Total\n';
    rows.forEach(r => {
      csv += `${r.lineOperatorCode},"${r.lineOperatorName}",${r.buckets.withinTenDays},${r.buckets.tenDaysOrMore},${r.buckets.withinTenDays + r.buckets.tenDaysOrMore}\n`;
    });
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `yard_aging_report_${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
