import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DeliveryOrderService } from '../../core/services/delivery-order.service';
import { Customer, DeliveryOrder, LineOperator } from '../../core/models/api.models';

@Component({
  selector: 'app-delivery-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
      <div>
        <h2>Active Delivery Orders</h2>
        <p class="muted" style="margin: 0;">Authorisation records that allow the depot to release empty containers.</p>
      </div>
      <div style="display: flex; gap: 8px;">
        <button (click)="refresh()" style="padding: 8px 14px; border-radius: 6px; border: 1px solid var(--color-border); cursor: pointer;">
          🔄 Refresh
        </button>
        <button (click)="showCreate.set(!showCreate())" style="padding: 8px 16px; background: #2563eb; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
          {{ showCreate() ? '✕ Cancel' : '+ Create Delivery Order' }}
        </button>
      </div>
    </div>

    <!-- Create Delivery Order Card -->
    <div *ngIf="showCreate()" class="card" style="margin-bottom: 20px; border-left: 4px solid #2563eb;">
      <h3>Create New Delivery Order</h3>
      <form (ngSubmit)="createOrder()" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; margin-top: 12px;">
        <label>Order Number
          <input [(ngModel)]="newOrder.orderNumber" name="orderNumber" placeholder="e.g. DO-2026-001" required />
        </label>
        <label>Customer
          <select [(ngModel)]="newOrder.customerId" name="customerId" required>
            <option value="">Select Customer…</option>
            <option *ngFor="let c of customers()" [value]="c.id">{{ c.name }} ({{ c.taxCode }})</option>
          </select>
        </label>
        <label>Line Operator (Shipping Line)
          <select [(ngModel)]="newOrder.lineOperatorId" name="lineOperatorId" required>
            <option value="">Select Operator…</option>
            <option *ngFor="let l of operators()" [value]="l.id">{{ l.code }} — {{ l.name }}</option>
          </select>
        </label>
        <label>Expiry Date
          <input type="date" [(ngModel)]="newOrder.expiryDate" name="expiryDate" required />
        </label>
        <label>Vessel / Voyage
          <input [(ngModel)]="newOrder.vesselVoyage" name="vesselVoyage" placeholder="e.g. MSC OSCAR / V-001" />
        </label>
        <label>Container Type
          <select [(ngModel)]="selectedTypeId" name="selectedTypeId" required>
            <option value="">Select Type…</option>
            <option *ngFor="let t of types()" [value]="t.id">{{ t.code }} — {{ t.name }}</option>
          </select>
        </label>
        <label>Requested Quantity
          <input type="number" min="1" [(ngModel)]="requestedQty" name="requestedQty" required />
        </label>
        <label>Notes
          <input [(ngModel)]="newOrder.notes" name="notes" placeholder="Optional notes" />
        </label>

        <div style="grid-column: 1 / -1; display: flex; gap: 12px; align-items: center; margin-top: 8px;">
          <button type="submit" [disabled]="submitting()" style="padding: 8px 20px; background: #16a34a; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
            {{ submitting() ? 'Creating…' : 'Create Order' }}
          </button>
          <span *ngIf="successMsg()" style="color: #16a34a; font-weight: 500;">✓ {{ successMsg() }}</span>
          <span *ngIf="errorMsg()" class="error" style="font-weight: 500;">{{ errorMsg() }}</span>
        </div>
      </form>
    </div>

    <!-- Table -->
    <table class="card">
      <thead>
        <tr>
          <th>Order #</th>
          <th>Customer</th>
          <th>Line Operator</th>
          <th>Expires</th>
          <th>Vessel / Voyage</th>
          <th>Requested Lines (Delivered / Total)</th>
          <th>Action</th>
        </tr>
      </thead>
      <tbody>
        <tr *ngFor="let o of orders()">
          <td><b>{{ o.orderNumber }}</b></td>
          <td>{{ o.customerName ?? o.customerId }}</td>
          <td>{{ o.lineOperatorName ?? o.lineOperatorId }}</td>
          <td>{{ o.expiryDate | date:'mediumDate' }}</td>
          <td>{{ o.vesselVoyage ?? '—' }}</td>
          <td>
            <span *ngFor="let l of o.lines" class="line-chip">
              {{ l.containerTypeName ?? l.containerTypeId }}: <b>{{ l.deliveredQuantity }}</b> / {{ l.requestedQuantity }}
            </span>
          </td>
          <td>
            <button (click)="closeOrder(o.id)" style="padding: 4px 10px; background: #fee2e2; color: #b91c1c; border: 1px solid #fca5a5; border-radius: 4px; cursor: pointer; font-size: 12px;">
              Close Order
            </button>
          </td>
        </tr>
        <tr *ngIf="!loading() && orders().length === 0">
          <td colspan="7" class="muted" style="text-align: center; padding: 20px;">No active orders found.</td>
        </tr>
      </tbody>
    </table>
  `,
  styles: [`
    table { width: 100%; border-collapse: collapse; overflow: hidden; }
    th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid var(--color-border); font-size: 13px; }
    .line-chip { display: inline-block; margin: 2px; padding: 2px 8px;
                 background: #eef2ff; border-radius: 10px; font-size: 12px; }
    label { display: flex; flex-direction: column; gap: 4px; font-size: 12px; font-weight: 500; }
    label input, label select { padding: 8px 10px; border-radius: 4px; border: 1px solid var(--color-border); font-size: 13px; }
  `],
})
export class DeliveryOrdersComponent implements OnInit {
  private svc = inject(DeliveryOrderService);
  loading = signal(true);
  submitting = signal(false);
  showCreate = signal(false);
  orders = signal<DeliveryOrder[]>([]);
  customers = signal<Customer[]>([]);
  operators = signal<LineOperator[]>([]);
  types = signal<{ id: string; code: string; name: string }[]>([]);
  successMsg = signal<string | null>(null);
  errorMsg = signal<string | null>(null);

  selectedTypeId = '';
  requestedQty = 5;

  newOrder = {
    orderNumber: `DO-${new Date().getFullYear()}-${Math.floor(1000 + Math.random() * 9000)}`,
    customerId: '',
    lineOperatorId: '',
    expiryDate: '2026-12-31',
    vesselVoyage: '',
    notes: '',
  };

  ngOnInit(): void {
    this.refresh();
    this.svc.customers().subscribe((c) => {
      this.customers.set(c);
      if (c.length > 0 && !this.newOrder.customerId) this.newOrder.customerId = c[0].id;
    });
    this.svc.lineOperators().subscribe((l) => {
      this.operators.set(l);
      if (l.length > 0 && !this.newOrder.lineOperatorId) this.newOrder.lineOperatorId = l[0].id;
    });
    this.svc.containerTypes().subscribe((t) => {
      this.types.set(t);
      if (t.length > 0 && !this.selectedTypeId) this.selectedTypeId = t[0].id;
    });
  }

  createOrder(): void {
    this.submitting.set(true);
    this.errorMsg.set(null);
    this.successMsg.set(null);

    const payload = {
      orderNumber: this.newOrder.orderNumber,
      customerId: this.newOrder.customerId,
      lineOperatorId: this.newOrder.lineOperatorId,
      expiryDate: new Date(this.newOrder.expiryDate).toISOString(),
      vesselVoyage: this.newOrder.vesselVoyage || undefined,
      notes: this.newOrder.notes || undefined,
      lines: [
        {
          containerTypeId: this.selectedTypeId,
          requestedQuantity: this.requestedQty,
          deliveredQuantity: 0,
        }
      ]
    };

    this.svc.create(payload).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.successMsg.set(`Delivery Order ${res.orderNumber} created successfully!`);
        this.newOrder.orderNumber = `DO-${new Date().getFullYear()}-${Math.floor(1000 + Math.random() * 9000)}`;
        this.refresh();
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMsg.set(err?.error?.detail ?? err?.error?.title ?? 'Failed to create order.');
      }
    });
  }

  closeOrder(id: string): void {
    if (!confirm('Are you sure you want to close this Delivery Order?')) return;
    this.svc.close(id).subscribe({
      next: () => this.refresh(),
      error: (err) => alert(err?.error?.detail ?? 'Failed to close order.'),
    });
  }

  refresh(): void {
    this.loading.set(true);
    this.svc.list().subscribe({
      next: (o) => { this.orders.set(o); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
