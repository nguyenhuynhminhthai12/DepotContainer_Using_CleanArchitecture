import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DeliveryOrderService } from '../../core/services/delivery-order.service';
import { Customer, DeliveryOrder, LineOperator } from '../../core/models/api.models';

interface FormOrderLine {
  containerTypeId: string;
  requestedQuantity: number;
  deliveredQuantity: number;
}

@Component({
  selector: 'app-delivery-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page-header">
      <div>
        <h2>Active Delivery Orders (Release Permits)</h2>
        <p class="muted">Shipping Line release permits allowing the depot to discharge empty/laden containers.</p>
      </div>
      <div class="header-actions">
        <button (click)="refresh()" class="secondary">🔄 Refresh</button>
        <button (click)="toggleCreateForm()" [class.secondary]="showCreate() || isEditing()">
          {{ showCreate() || isEditing() ? '✕ Close Form' : '+ Create Delivery Order' }}
        </button>
      </div>
    </div>

    <!-- KPI Metric Cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #eff6ff; color: #2563eb;">📋</div>
        <div class="kpi-info">
          <span class="kpi-label">Active Orders</span>
          <span class="kpi-value">{{ activeOrdersCount() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #ecfdf5; color: #059669;">📥</div>
        <div class="kpi-info">
          <span class="kpi-label">Delivered Units</span>
          <span class="kpi-value">{{ totalDeliveredUnits() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fffbeb; color: #d97706;">📦</div>
        <div class="kpi-info">
          <span class="kpi-label">Total Requested</span>
          <span class="kpi-value">{{ totalRequestedUnits() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fdf4ff; color: #9333ea;">⚡</div>
        <div class="kpi-info">
          <span class="kpi-label">Fulfillment Rate</span>
          <span class="kpi-value">{{ fulfillmentRate() }}%</span>
        </div>
      </div>
    </div>

    <!-- Create / Edit Form -->
    <div *ngIf="showCreate() || isEditing()" class="card create-card" [class.edit-card]="isEditing()">
      <div class="card-title-bar">
        <h3>{{ isEditing() ? '✏️ Edit Delivery Order: ' + formOrder.orderNumber : '✨ Create Delivery Order' }}</h3>
        <span class="badge" [class.badge-indigo]="isEditing()">
          {{ isEditing() ? 'Edit Mode' : 'Shipping Line Authorisation' }}
        </span>
      </div>

      <form (ngSubmit)="saveOrder()" class="create-form">
        <label>Order Number <span style="color: #ef4444;">*</span>
          <input [(ngModel)]="formOrder.orderNumber" name="orderNumber" placeholder="e.g. DO-2026-001" [readonly]="isEditing()" required />
        </label>

        <div>
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 4px;">
            <label style="margin: 0; font-size: 13px;">Customer / Consignee <span style="color: #ef4444;">*</span></label>
            <button type="button" class="btn-quick-add" (click)="openNewCustomerModal()" title="Add a new customer to master list">
              + New Customer
            </button>
          </div>
          <select [(ngModel)]="formOrder.customerId" name="customerId" [disabled]="isEditing()" required>
            <option value="">Select Customer…</option>
            <option *ngFor="let c of customers()" [value]="c.id">{{ c.name }} ({{ c.taxCode }})</option>
          </select>
        </div>

        <label>Line Operator (Shipping Line) <span style="color: #ef4444;">*</span>
          <select [(ngModel)]="formOrder.lineOperatorId" name="lineOperatorId" [disabled]="isEditing()" required>
            <option value="">Select Operator…</option>
            <option *ngFor="let l of operators()" [value]="l.id">{{ l.code }} — {{ l.name }}</option>
          </select>
        </label>

        <div class="date-group">
          <label>Expiry Date <span style="color: #ef4444;">*</span>
            <input type="date" [(ngModel)]="formOrder.expiryDate" name="expiryDate" required />
          </label>
          <div class="quick-dates">
            <button type="button" class="btn-quick-date" (click)="setExpiryDays(7)">+7 Days</button>
            <button type="button" class="btn-quick-date" (click)="setExpiryDays(14)">+14 Days</button>
            <button type="button" class="btn-quick-date" (click)="setExpiryDays(30)">+30 Days</button>
          </div>
        </div>

        <label>Vessel / Voyage
          <input [(ngModel)]="formOrder.vesselVoyage" name="vesselVoyage" placeholder="e.g. MSC OSCAR / V-001" />
        </label>

        <label>Notes / Instructions
          <input [(ngModel)]="formOrder.notes" name="notes" placeholder="e.g. For Export loading at Tan Cang" />
        </label>

        <!-- Multi Container Types Requirements Section -->
        <div class="lines-container-card">
          <div class="lines-card-header">
            <div>
              <strong>📦 Container Types & Quantities</strong>
              <span class="muted small" style="margin-left: 8px;">(You can add multiple container types to 1 DO)</span>
            </div>
            <button type="button" class="btn-add-line-btn" (click)="addLine()">
              + Add Another Container Type
            </button>
          </div>

          <div class="line-items-list">
            <div *ngFor="let line of formLines; let i = index" class="line-item-row">
              <div class="line-num">#{{ i + 1 }}</div>
              
              <label class="line-type-label">Container Type <span style="color: #ef4444;">*</span>
                <select [(ngModel)]="line.containerTypeId" [name]="'type_' + i" required>
                  <option value="">Select Type…</option>
                  <option *ngFor="let t of types()" [value]="t.id">{{ t.code }} — {{ t.name }}</option>
                </select>
              </label>

              <label class="line-qty-label">Requested Quantity <span style="color: #ef4444;">*</span>
                <input type="number" min="1" [(ngModel)]="line.requestedQuantity" [name]="'qty_' + i" required />
              </label>

              <button
                type="button"
                class="btn-remove-line-btn"
                *ngIf="formLines.length > 1"
                (click)="removeLine(i)"
                title="Remove this container type requirement"
              >
                ✕
              </button>
            </div>
          </div>
        </div>

        <div class="form-actions">
          <button type="submit" [disabled]="submitting()" class="btn-success">
            {{ submitting() ? (isEditing() ? 'Saving Changes…' : 'Creating Delivery Order…') : (isEditing() ? '✓ Save Changes' : '✓ Create Order') }}
          </button>
          <button type="button" class="secondary" (click)="cancelForm()">Cancel</button>

          <span *ngIf="successMsg()" class="success-msg">✓ {{ successMsg() }}</span>
          <span *ngIf="errorMsg()" class="error-msg">⚠️ {{ errorMsg() }}</span>
        </div>
      </form>
    </div>

    <!-- Search & Filter Bar -->
    <div class="filters-card card">
      <div class="search-input-wrap">
        <span class="search-icon">🔍</span>
        <input [(ngModel)]="search" placeholder="Search by DO number, customer, shipping line, or container type…" />
      </div>
    </div>

    <!-- Table -->
    <div class="table-container card">
      <table>
        <thead>
          <tr>
            <th>Order #</th>
            <th>Customer</th>
            <th>Shipping Line</th>
            <th>Container Type(s)</th>
            <th>Expires On</th>
            <th>Vessel / Voyage</th>
            <th>Fulfillment Progress</th>
            <th>Status</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let o of filteredOrders()">
            <td>
              <span class="do-number">{{ o.orderNumber }}</span>
            </td>
            <td><b>{{ o.customerName ?? o.customerId }}</b></td>
            <td>
              <span class="badge badge-indigo">{{ o.lineOperatorName ?? o.lineOperatorId }}</span>
            </td>
            <td>
              <div class="type-badge-wrap">
                <span *ngFor="let l of o.lines" class="badge badge-type">
                  🏷️ {{ getTypeLabel(l.containerTypeId, l.containerTypeName) }}
                </span>
              </div>
            </td>
            <td>
              <span [class.text-danger]="isExpired(o.expiryDate)">
                {{ o.expiryDate | date:'mediumDate' }}
                <span *ngIf="isExpired(o.expiryDate)" class="badge badge-danger">Expired</span>
              </span>
            </td>
            <td>{{ o.vesselVoyage || '—' }}</td>
            <td>
              <div *ngFor="let l of o.lines" class="line-progress-wrap">
                <div class="line-info">
                  <span class="type-name-label">{{ getTypeLabel(l.containerTypeId, l.containerTypeName) }}:</span>
                  <b>{{ l.deliveredQuantity }} / {{ l.requestedQuantity }} units</b>
                  <span class="muted">({{ getPercent(l.deliveredQuantity, l.requestedQuantity) }}%)</span>
                </div>
                <div class="progress-bar-wrap">
                  <div class="progress-bar-fill" [style.width.%]="getPercent(l.deliveredQuantity, l.requestedQuantity)"></div>
                </div>
              </div>
            </td>
            <td>
              <span *ngIf="!o.isClosed && !isExpired(o.expiryDate)" class="badge badge-success">● Active</span>
              <span *ngIf="o.isClosed" class="badge badge-muted">Closed</span>
            </td>
            <td>
              <div class="action-btn-group">
                <button *ngIf="!o.isClosed" (click)="openEdit(o)" class="btn-edit-order" title="Edit this Delivery Order">
                  ✏️ Edit
                </button>
                <button *ngIf="!o.isClosed" (click)="closeOrder(o.id)" class="btn-close-order" title="Close this Delivery Order">
                  Close
                </button>
                <button *ngIf="!hasDischarged(o)" (click)="deleteOrder(o)" class="btn-delete-order" title="Delete Delivery Order">
                  🗑️ Delete
                </button>
              </div>
            </td>
          </tr>
          <tr *ngIf="!loading() && filteredOrders().length === 0">
            <td colspan="9" class="empty-state">
              <div class="empty-icon">📋</div>
              <div class="empty-text">No active delivery orders found.</div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- New Customer Quick-Create Modal -->
    <div *ngIf="showNewCustomerModal()" class="modal-backdrop" (click)="showNewCustomerModal.set(false)">
      <div class="modal-card card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>🏢 Add New Customer / Consignee</h3>
          <button type="button" class="btn-close-modal" (click)="showNewCustomerModal.set(false)">✕</button>
        </div>

        <form (ngSubmit)="saveNewCustomer()" class="modal-form">
          <label>Company / Consignee Name <span style="color: #ef4444;">*</span>
            <input [(ngModel)]="newCustomer.name" name="custName" placeholder="e.g. Tan Cang Logistics JSC" required />
          </label>

          <label>Tax Code (MST) <span style="color: #ef4444;">*</span>
            <input [(ngModel)]="newCustomer.taxCode" name="custTax" placeholder="e.g. 0312345678" required />
          </label>

          <label>Contact Phone
            <input [(ngModel)]="newCustomer.phone" name="custPhone" placeholder="e.g. 0901234567" />
          </label>

          <label>Contact Email
            <input type="email" [(ngModel)]="newCustomer.email" name="custEmail" placeholder="e.g. logistics@tancang.vn" />
          </label>

          <label style="grid-column: 1 / -1;">Company Address
            <input [(ngModel)]="newCustomer.address" name="custAddr" placeholder="e.g. Cat Lai Port, Dist 2, HCMC" />
          </label>

          <div *ngIf="customerErr()" class="error-msg" style="grid-column: 1 / -1;">
            ⚠️ {{ customerErr() }}
          </div>

          <div class="modal-actions">
            <button type="submit" [disabled]="savingCustomer()" class="btn-success">
              {{ savingCustomer() ? 'Saving Customer…' : '✓ Create Customer' }}
            </button>
            <button type="button" class="secondary" (click)="showNewCustomerModal.set(false)">Cancel</button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .header-actions { display: flex; gap: 8px; }

    .create-card {
      margin-bottom: 20px;
      border-left: 4px solid #2563eb;
    }
    .edit-card {
      border-left-color: #6366f1;
      background: #fafafa;
    }
    .card-title-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }
    .card-title-bar h3 { margin: 0; }

    .create-form {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 14px;
    }

    .date-group { display: flex; flex-direction: column; gap: 4px; }
    .quick-dates { display: flex; gap: 4px; margin-top: 4px; }
    .btn-quick-date {
      padding: 2px 6px;
      font-size: 11px;
      background: #f1f5f9;
      color: #334155;
      border: 1px solid #cbd5e1;
      border-radius: 4px;
      cursor: pointer;
    }
    .btn-quick-date:hover { background: #e2e8f0; }

    /* Multi Line Requirements Section */
    .lines-container-card {
      grid-column: 1 / -1;
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      padding: 14px;
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .lines-card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 4px;
    }
    .btn-add-line-btn {
      background: #eff6ff;
      color: #2563eb;
      border: 1px solid #93c5fd;
      padding: 4px 10px;
      font-size: 12px;
      font-weight: 600;
      border-radius: 6px;
      cursor: pointer;
    }
    .btn-add-line-btn:hover {
      background: #dbeafe;
    }

    .line-items-list {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
    .line-item-row {
      display: flex;
      gap: 12px;
      align-items: flex-end;
      background: #ffffff;
      padding: 8px 12px;
      border-radius: 6px;
      border: 1px solid #e2e8f0;
    }
    .line-num {
      font-weight: 700;
      color: #64748b;
      font-size: 13px;
      padding-bottom: 8px;
    }
    .line-type-label { flex: 2; margin: 0; }
    .line-qty-label { flex: 1; margin: 0; }
    .btn-remove-line-btn {
      background: #fef2f2;
      color: #dc2626;
      border: 1px solid #fecaca;
      border-radius: 4px;
      padding: 6px 10px;
      font-size: 13px;
      cursor: pointer;
      margin-bottom: 2px;
    }
    .btn-remove-line-btn:hover {
      background: #fee2e2;
    }

    .form-actions {
      grid-column: 1 / -1;
      display: flex;
      gap: 12px;
      align-items: center;
      margin-top: 8px;
      padding-top: 12px;
      border-top: 1px solid #f1f5f9;
    }

    .success-msg { color: #16a34a; font-weight: 600; font-size: 13px; }
    .error-msg { color: #dc2626; font-weight: 600; font-size: 13px; }

    .filters-card {
      padding: 12px 16px;
      margin-bottom: 16px;
    }
    .search-input-wrap {
      position: relative;
      display: flex;
      align-items: center;
    }
    .search-icon {
      position: absolute;
      left: 10px;
      color: #94a3b8;
    }
    .search-input-wrap input {
      padding-left: 32px;
      width: 100%;
    }

    .table-container {
      overflow-x: auto;
      border-radius: var(--radius-md);
    }
    table {
      width: 100%;
      border-collapse: collapse;
      text-align: left;
    }
    th, td {
      padding: 12px 14px;
      border-bottom: 1px solid var(--color-border);
      font-size: 13px;
    }
    th {
      background: #f8fafc;
      color: #475569;
      font-weight: 600;
    }
    tr:hover { background: #f8fafc; }

    .do-number {
      font-family: var(--font-mono);
      font-weight: 700;
      color: #0f172a;
    }

    .badge-indigo { background: #eef2ff; color: #4f46e5; }
    .type-badge-wrap { display: flex; flex-direction: column; gap: 4px; }
    .badge-type {
      background: #f1f5f9;
      color: #0f172a;
      border: 1px solid #cbd5e1;
      font-family: var(--font-mono);
      font-size: 11px;
      font-weight: 600;
    }

    .line-progress-wrap {
      min-width: 170px;
      display: flex;
      flex-direction: column;
      gap: 4px;
      margin-bottom: 6px;
    }
    .line-progress-wrap:last-child { margin-bottom: 0; }
    .line-info {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 12px;
    }
    .type-name-label { font-weight: 600; font-size: 11px; color: #475569; }

    .action-btn-group {
      display: flex;
      gap: 6px;
      align-items: center;
    }
    .btn-edit-order {
      background: #f1f5f9;
      color: #2563eb;
      border: 1px solid #cbd5e1;
      padding: 4px 8px;
      font-size: 12px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .btn-edit-order:hover {
      background: #eff6ff;
      border-color: #93c5fd;
    }

    .btn-close-order {
      background: none;
      color: #dc2626;
      border: 1px solid #fecaca;
      padding: 4px 8px;
      font-size: 12px;
      border-radius: 4px;
      cursor: pointer;
    }
    .btn-close-order:hover {
      background: #fef2f2;
    }
    .btn-delete-order {
      background: none;
      color: #dc2626;
      border: 1px solid #fecaca;
      padding: 4px 8px;
      font-size: 12px;
      border-radius: 4px;
      cursor: pointer;
    }
    .btn-delete-order:hover {
      background: #fef2f2;
      border-color: #f87171;
    }

    .btn-quick-add {
      background: none;
      border: none;
      color: #2563eb;
      font-size: 11px;
      font-weight: 600;
      cursor: pointer;
      padding: 0;
    }
    .btn-quick-add:hover {
      text-decoration: underline;
    }

    /* Modal Backdrop & Card */
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      background: rgba(15, 23, 42, 0.6);
      backdrop-filter: blur(4px);
      z-index: 1000;
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 20px;
    }
    .modal-card {
      width: 100%;
      max-width: 520px;
      background: #ffffff;
      border-radius: 12px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.2), 0 10px 10px -5px rgba(0, 0, 0, 0.04);
      padding: 24px;
    }
    .modal-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      padding-bottom: 12px;
      border-bottom: 1px solid #f1f5f9;
    }
    .modal-header h3 { margin: 0; font-size: 16px; }
    .btn-close-modal {
      background: none;
      border: none;
      font-size: 18px;
      color: #94a3b8;
      cursor: pointer;
      padding: 4px;
    }
    .btn-close-modal:hover { color: #0f172a; }

    .modal-form {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }
    .modal-actions {
      grid-column: 1 / -1;
      display: flex;
      gap: 10px;
      justify-content: flex-end;
      margin-top: 12px;
      padding-top: 12px;
      border-top: 1px solid #f1f5f9;
    }

    .empty-state {
      text-align: center;
      padding: 40px !important;
    }
    .empty-icon { font-size: 32px; margin-bottom: 8px; }
    .empty-text { color: #64748b; font-size: 14px; }
  `],
})
export class DeliveryOrdersComponent implements OnInit {
  private readonly svc = inject(DeliveryOrderService);

  loading = signal(true);
  submitting = signal(false);
  showCreate = signal(false);
  isEditing = signal(false);
  editingOrderId: string | null = null;

  orders = signal<DeliveryOrder[]>([]);
  customers = signal<Customer[]>([]);
  operators = signal<LineOperator[]>([]);
  types = signal<{ id: string; code: string; name: string }[]>([]);
  successMsg = signal<string | null>(null);
  errorMsg = signal<string | null>(null);
  search = '';

  // New Customer Modal state
  showNewCustomerModal = signal(false);
  savingCustomer = signal(false);
  customerErr = signal<string | null>(null);
  newCustomer = {
    name: '',
    taxCode: '',
    phone: '',
    email: '',
    address: '',
  };

  formOrder = {
    orderNumber: `DO-${new Date().getFullYear()}-${Date.now() % 9000 + 1000}`,
    customerId: '',
    lineOperatorId: '',
    expiryDate: new Date(Date.now() + 14 * 86400000).toISOString().split('T')[0],
    vesselVoyage: '',
    notes: '',
  };

  formLines: FormOrderLine[] = [];

  // KPIs
  activeOrdersCount = computed(() => this.orders().filter(o => !o.isClosed).length);
  totalRequestedUnits = computed(() =>
    this.orders().reduce((sum, o) => sum + (o.lines?.reduce((s, l) => s + l.requestedQuantity, 0) ?? 0), 0)
  );
  totalDeliveredUnits = computed(() =>
    this.orders().reduce((sum, o) => sum + (o.lines?.reduce((s, l) => s + l.deliveredQuantity, 0) ?? 0), 0)
  );
  fulfillmentRate = computed(() => {
    const req = this.totalRequestedUnits();
    if (req === 0) return 0;
    return Math.round((this.totalDeliveredUnits() / req) * 100);
  });

  filteredOrders = computed(() => {
    const q = this.search.toLowerCase().trim();
    if (!q) return this.orders();
    return this.orders().filter(o =>
      o.orderNumber.toLowerCase().includes(q) ||
      o.customerName?.toLowerCase().includes(q) ||
      o.lineOperatorName?.toLowerCase().includes(q) ||
      o.lines?.some(l => this.getTypeLabel(l.containerTypeId, l.containerTypeName).toLowerCase().includes(q))
    );
  });

  ngOnInit(): void {
    this.refresh();
    this.svc.customers().subscribe((c) => {
      this.customers.set(c);
      if (c.length > 0 && !this.formOrder.customerId) this.formOrder.customerId = c[0].id;
    });
    this.svc.lineOperators().subscribe((l) => {
      this.operators.set(l);
      if (l.length > 0 && !this.formOrder.lineOperatorId) this.formOrder.lineOperatorId = l[0].id;
    });
    this.svc.containerTypes().subscribe((t) => {
      this.types.set(t);
      if (this.formLines.length === 0 && t.length > 0) {
        this.formLines = [{ containerTypeId: t[0].id, requestedQuantity: 5, deliveredQuantity: 0 }];
      }
    });
  }

  openNewCustomerModal(): void {
    this.customerErr.set(null);
    this.newCustomer = {
      name: '',
      taxCode: '',
      phone: '',
      email: '',
      address: '',
    };
    this.showNewCustomerModal.set(true);
  }

  saveNewCustomer(): void {
    if (!this.newCustomer.name || !this.newCustomer.taxCode) {
      this.customerErr.set('Company Name and Tax Code are required.');
      return;
    }

    this.savingCustomer.set(true);
    this.customerErr.set(null);

    this.svc.createCustomer(this.newCustomer).subscribe({
      next: (created) => {
        this.savingCustomer.set(false);
        this.showNewCustomerModal.set(false);
        // Refresh customer list and auto-select the new customer
        this.svc.customers().subscribe((list) => {
          this.customers.set(list);
          this.formOrder.customerId = created.id;
        });
      },
      error: (err) => {
        this.savingCustomer.set(false);
        this.customerErr.set(err?.error?.detail ?? err?.error?.title ?? 'Failed to create customer.');
      }
    });
  }

  addLine(): void {
    const firstType = this.types()[0]?.id ?? '';
    this.formLines.push({ containerTypeId: firstType, requestedQuantity: 5, deliveredQuantity: 0 });
  }

  removeLine(index: number): void {
    if (this.formLines.length > 1) {
      this.formLines.splice(index, 1);
    }
  }

  toggleCreateForm(): void {
    if (this.showCreate() || this.isEditing()) {
      this.cancelForm();
    } else {
      this.showCreate.set(true);
      this.isEditing.set(false);
      this.editingOrderId = null;
      this.resetForm();
    }
  }

  resetForm(): void {
    this.formOrder = {
      orderNumber: `DO-${new Date().getFullYear()}-${Date.now() % 9000 + 1000}`,
      customerId: this.customers()[0]?.id ?? '',
      lineOperatorId: this.operators()[0]?.id ?? '',
      expiryDate: new Date(Date.now() + 14 * 86400000).toISOString().split('T')[0],
      vesselVoyage: '',
      notes: '',
    };
    this.formLines = [{ containerTypeId: this.types()[0]?.id ?? '', requestedQuantity: 5, deliveredQuantity: 0 }];
    this.errorMsg.set(null);
    this.successMsg.set(null);
  }

  cancelForm(): void {
    this.showCreate.set(false);
    this.isEditing.set(false);
    this.editingOrderId = null;
    this.errorMsg.set(null);
    this.successMsg.set(null);
  }

  openEdit(order: DeliveryOrder): void {
    this.isEditing.set(true);
    this.showCreate.set(false);
    this.editingOrderId = order.id;
    this.errorMsg.set(null);
    this.successMsg.set(null);

    this.formOrder = {
      orderNumber: order.orderNumber,
      customerId: order.customerId,
      lineOperatorId: order.lineOperatorId,
      expiryDate: new Date(order.expiryDate).toISOString().split('T')[0],
      vesselVoyage: order.vesselVoyage ?? '',
      notes: '',
    };

    if (order.lines && order.lines.length > 0) {
      this.formLines = order.lines.map(l => ({
        containerTypeId: l.containerTypeId,
        requestedQuantity: l.requestedQuantity,
        deliveredQuantity: l.deliveredQuantity
      }));
    } else {
      this.formLines = [{ containerTypeId: this.types()[0]?.id ?? '', requestedQuantity: 5, deliveredQuantity: 0 }];
    }

    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  setExpiryDays(days: number): void {
    const d = new Date(Date.now() + days * 86400000);
    this.formOrder.expiryDate = d.toISOString().split('T')[0];
  }

  isExpired(dateStr: string): boolean {
    return new Date(dateStr).getTime() < Date.now();
  }

  getTypeLabel(typeId: string, typeName?: string): string {
    if (typeName) return typeName;
    const match = this.types().find(t => t.id === typeId);
    if (match) return `${match.code} (${match.name})`;
    return typeId ? typeId.substring(0, 8) : 'General';
  }

  getPercent(del: number, req: number): number {
    if (!req) return 0;
    return Math.min(100, Math.round((del / req) * 100));
  }

  refresh(): void {
    this.loading.set(true);
    this.svc.list().subscribe({
      next: (d) => { this.orders.set(d); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  saveOrder(): void {
    if (this.isEditing() && this.editingOrderId) {
      this.updateOrder();
    } else {
      this.createOrder();
    }
  }

  createOrder(): void {
    if (this.formLines.length === 0 || this.formLines.some(l => !l.containerTypeId || l.requestedQuantity < 1)) {
      this.errorMsg.set('Please select a valid container type and quantity (min 1) for each line.');
      return;
    }

    this.submitting.set(true);
    this.errorMsg.set(null);
    this.successMsg.set(null);

    const payload = {
      orderNumber: this.formOrder.orderNumber,
      customerId: this.formOrder.customerId,
      lineOperatorId: this.formOrder.lineOperatorId,
      expiryDate: new Date(this.formOrder.expiryDate).toISOString(),
      vesselVoyage: this.formOrder.vesselVoyage || undefined,
      notes: this.formOrder.notes || undefined,
      lines: this.formLines.map(l => ({
        containerTypeId: l.containerTypeId,
        requestedQuantity: Number(l.requestedQuantity),
        deliveredQuantity: 0,
      }))
    };

    this.svc.create(payload).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.successMsg.set(`Delivery Order ${res.orderNumber} created successfully!`);
        this.resetForm();
        this.refresh();
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMsg.set(err?.error?.detail ?? err?.error?.title ?? 'Failed to create order.');
      }
    });
  }

  updateOrder(): void {
    if (!this.editingOrderId) return;
    if (this.formLines.length === 0 || this.formLines.some(l => !l.containerTypeId || l.requestedQuantity < 1)) {
      this.errorMsg.set('Please select a valid container type and quantity (min 1) for each line.');
      return;
    }

    this.submitting.set(true);
    this.errorMsg.set(null);
    this.successMsg.set(null);

    const payload = {
      id: this.editingOrderId,
      expiryDate: new Date(this.formOrder.expiryDate).toISOString(),
      vesselVoyage: this.formOrder.vesselVoyage || undefined,
      notes: this.formOrder.notes || undefined,
      lines: this.formLines.map(l => ({
        containerTypeId: l.containerTypeId,
        requestedQuantity: Number(l.requestedQuantity),
        deliveredQuantity: l.deliveredQuantity ?? 0,
      }))
    };

    this.svc.update(this.editingOrderId, payload).subscribe({
      next: (res) => {
        this.submitting.set(false);
        this.successMsg.set(`Delivery Order ${res.orderNumber} updated successfully!`);
        this.refresh();
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMsg.set(err?.error?.detail ?? err?.error?.title ?? 'Failed to update order.');
      }
    });
  }

  hasDischarged(o: DeliveryOrder): boolean {
    return o.lines?.some(l => l.deliveredQuantity > 0) ?? false;
  }

  deleteOrder(order: DeliveryOrder): void {
    if (!confirm(`Are you sure you want to permanently delete Delivery Order ${order.orderNumber}?`)) return;

    this.svc.delete(order.id).subscribe({
      next: () => {
        this.refresh();
      },
      error: (err) => {
        alert(err?.error?.detail ?? err?.error?.title ?? 'Failed to delete Delivery Order.');
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
}
