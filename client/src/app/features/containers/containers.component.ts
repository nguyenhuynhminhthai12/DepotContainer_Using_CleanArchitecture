import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ContainerService } from '../../core/services/container.service';
import { Container, ContainerType } from '../../core/models/api.models';

@Component({
  selector: 'app-containers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
      <div>
        <h2>Containers</h2>
        <p class="muted" style="margin: 0;">Master data. Container numbers are validated server-side via ISO 6346 Modulo-11.</p>
      </div>
      <button (click)="showCreate.set(!showCreate())" style="padding: 8px 16px; background: #2563eb; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
        {{ showCreate() ? '✕ Cancel' : '+ Register Container' }}
      </button>
    </div>

    <!-- Create Container Card -->
    <div *ngIf="showCreate()" class="card" style="margin-bottom: 20px; border-left: 4px solid #2563eb;">
      <h3>Register New Container</h3>
      <form (ngSubmit)="createContainer()" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; margin-top: 12px;">
        <label>Container Number (ISO 6346)
          <input [(ngModel)]="newCntr.containerNumber" name="containerNumber" placeholder="e.g. MSCU1234566" required />
        </label>
        <label>Container Type
          <select [(ngModel)]="newCntr.containerTypeId" name="containerTypeId" (change)="onTypeChange()" required>
            <option value="">Select Type…</option>
            <option *ngFor="let t of types()" [value]="t.id">{{ t.code }} — {{ t.name }}</option>
          </select>
        </label>
        <label>ISO Code
          <input [(ngModel)]="newCntr.isoCode" name="isoCode" placeholder="22G1" required />
        </label>
        <label>Size (Feet)
          <select [(ngModel)]="newCntr.sizeFeet" name="sizeFeet">
            <option [ngValue]="20">20 ft</option>
            <option [ngValue]="40">40 ft</option>
            <option [ngValue]="45">45 ft</option>
          </select>
        </label>
        <label>Max Weight (kg)
          <input type="number" [(ngModel)]="newCntr.maxWeightKg" name="maxWeightKg" required />
        </label>
        <label>Tare Weight (kg)
          <input type="number" [(ngModel)]="newCntr.tareWeightKg" name="tareWeightKg" required />
        </label>
        <label>Manufacture Date
          <input type="date" [(ngModel)]="newCntr.manufactureDate" name="manufactureDate" required />
        </label>
        <label>Owner / Shipping Line
          <input [(ngModel)]="newCntr.owner" name="owner" placeholder="e.g. MSC, CMA CGM, MAERSK" required />
        </label>
        <label>Condition
          <select [(ngModel)]="newCntr.condition" name="condition">
            <option *ngFor="let c of conditions" [value]="c">{{ c }}</option>
          </select>
        </label>

        <div style="grid-column: 1 / -1; display: flex; gap: 12px; align-items: center; margin-top: 8px;">
          <button type="submit" [disabled]="submitting()" style="padding: 8px 20px; background: #16a34a; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
            {{ submitting() ? 'Saving…' : 'Save Container' }}
          </button>
          <span *ngIf="successMsg()" style="color: #16a34a; font-weight: 500;">✓ {{ successMsg() }}</span>
          <span *ngIf="errorMsg()" class="error" style="font-weight: 500;">{{ errorMsg() }}</span>
        </div>
      </form>
    </div>

    <!-- Filter Bar -->
    <div class="filters card">
      <input [(ngModel)]="search" placeholder="Search container number / owner…" (ngModelChange)="refresh()" />
      <select [(ngModel)]="condition" (change)="refresh()">
        <option value="">All conditions</option>
        <option *ngFor="let c of conditions" [value]="c">{{ c }}</option>
      </select>
      <button (click)="refresh()" style="padding: 6px 12px; border-radius: 4px; border: 1px solid var(--color-border); cursor: pointer;">🔄 Refresh</button>
    </div>

    <!-- Table -->
    <table class="card">
      <thead>
        <tr>
          <th>Container #</th>
          <th>Owner</th>
          <th>Size</th>
          <th>ISO Code</th>
          <th>Max (kg)</th>
          <th>Tare (kg)</th>
          <th>Condition</th>
          <th>Manufactured</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let c of items()">
          <td><b>{{ c.containerNumber }}</b></td>
          <td>{{ c.owner }}</td>
          <td>{{ c.sizeFeet }} ft</td>
          <td><span class="badge">{{ c.isoCode }}</span></td>
          <td>{{ c.maxWeightKg | number }}</td>
          <td>{{ c.tareWeightKg | number }}</td>
          <td>
            <span [class.badge-danger]="c.condition !== 'Normal'" class="badge">{{ c.condition }}</span>
          </td>
          <td>{{ c.manufactureDate | date:'mediumDate' }}</td>
        </tr>
        <tr *ngIf="!loading() && items().length === 0">
          <td colspan="8" class="muted" style="text-align: center; padding: 20px;">No containers match.</td>
        </tr>
      </tbody>
    </table>

    <p class="muted" style="margin-top: 8px;">Showing {{ items().length }} of {{ total() }} containers</p>
  `,
  styles: [`
    .filters { display: flex; gap: 8px; margin-bottom: 12px; padding: 12px; }
    .filters input { flex: 1; padding: 8px 12px; border-radius: 4px; border: 1px solid var(--color-border); }
    .filters select { padding: 8px 12px; border-radius: 4px; border: 1px solid var(--color-border); }
    table { width: 100%; border-collapse: collapse; overflow: hidden; }
    th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid var(--color-border); font-size: 13px; }
    tbody tr td:first-child { font-family: 'SFMono-Regular', Consolas, monospace; }
    label { display: flex; flex-direction: column; gap: 4px; font-size: 12px; font-weight: 500; }
    label input, label select { padding: 8px 10px; border-radius: 4px; border: 1px solid var(--color-border); font-size: 13px; }
    .badge { padding: 2px 8px; border-radius: 12px; font-size: 11px; background: #e0f2fe; color: #0369a1; }
    .badge-danger { background: #fee2e2; color: #b91c1c; }
  `],
})
export class ContainersComponent implements OnInit {
  private readonly svc = inject(ContainerService);
  loading = signal(true);
  submitting = signal(false);
  showCreate = signal(false);
  items = signal<Container[]>([]);
  types = signal<ContainerType[]>([]);
  total = signal(0);
  search = '';
  condition = '';
  conditions = ['Normal', 'Damaged', 'Dented', 'Twisted', 'Cracked', 'Leaking', 'Other'];
  successMsg = signal<string | null>(null);
  errorMsg = signal<string | null>(null);

  newCntr: Omit<Container, 'id' | 'tenantId'> = {
    containerNumber: '',
    containerTypeId: '',
    isoCode: '22G1',
    sizeFeet: 20,
    maxWeightKg: 30480,
    tareWeightKg: 2200,
    manufactureDate: '2024-01-15',
    owner: 'MSC',
    condition: 'Normal',
  };

  ngOnInit(): void {
    this.refresh();
    this.svc.listTypes().subscribe((t) => this.types.set(t));
  }

  onTypeChange(): void {
    const selected = this.types().find(t => t.id === this.newCntr.containerTypeId);
    if (selected) {
      this.newCntr.isoCode = selected.code;
      if (selected.code.startsWith('4')) {
        this.newCntr.sizeFeet = 40;
      } else {
        this.newCntr.sizeFeet = 20;
      }
    }
  }

  createContainer(): void {
    this.submitting.set(true);
    this.errorMsg.set(null);
    this.successMsg.set(null);

    this.svc.create({
      ...this.newCntr,
      manufactureDate: new Date(this.newCntr.manufactureDate).toISOString(),
    }).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.successMsg.set(`Container ${created.containerNumber} registered successfully!`);
        this.newCntr.containerNumber = '';
        this.refresh();
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMsg.set(err?.error?.detail ?? err?.error?.title ?? 'Failed to register container.');
      }
    });
  }

  refresh(): void {
    this.loading.set(true);
    this.svc.list(1, 50, undefined, this.condition || undefined, this.search || undefined).subscribe({
      next: (p) => { this.items.set(p.items); this.total.set(p.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
