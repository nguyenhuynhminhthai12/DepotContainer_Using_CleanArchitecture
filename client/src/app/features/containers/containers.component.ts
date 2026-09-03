import { Component, OnInit, inject, signal, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ContainerService } from '../../core/services/container.service';
import { Container, ContainerType } from '../../core/models/api.models';

const ISO_LETTER_VALUES: { [key: string]: number } = {
  A: 10, B: 12, C: 13, D: 14, E: 15, F: 16, G: 17, H: 18, I: 19, J: 20,
  K: 21, L: 23, M: 24, N: 25, O: 26, P: 27, Q: 28, R: 29, S: 30, T: 31,
  U: 32, V: 34, W: 35, X: 36, Y: 37, Z: 38
};

const POPULAR_PREFIXES = [
  { code: 'MSCU', owner: 'MSC' },
  { code: 'CMAU', owner: 'CMA CGM' },
  { code: 'APLU', owner: 'APL (CMA CGM)' },
  { code: 'MSKU', owner: 'MAERSK' },
  { code: 'COSU', owner: 'COSCO' },
  { code: 'ONEU', owner: 'ONE' },
  { code: 'HLCU', owner: 'Hapag-Lloyd' },
  { code: 'EMCU', owner: 'Evergreen' },
  { code: 'YMLU', owner: 'Yang Ming' },
  { code: 'OOCU', owner: 'OOCL' },
  { code: 'WHLU', owner: 'Wan Hai Lines' },
  { code: 'ZIMU', owner: 'ZIM' },
  { code: 'SMLU', owner: 'SM Line' },
  { code: 'KMTC', owner: 'KMTC Line' },
  { code: 'PILU', owner: 'PIL' },
  { code: 'TEMU', owner: 'Textainer' },
  { code: 'TGHU', owner: 'Textainer' },
  { code: 'TRHU', owner: 'Triton' }
];

@Component({
  selector: 'app-containers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page-header">
      <div>
        <h2>Container Master Fleet</h2>
        <p class="muted">ISO 6346 registered container assets, tare & payload specifications, and condition grading.</p>
      </div>
      <div class="header-actions">
        <button (click)="refresh()" class="secondary">🔄 Refresh</button>
        <button (click)="toggleCreate()" [class.secondary]="showCreate() || isEditing()">
          {{ showCreate() || isEditing() ? '✕ Close Form' : '+ Register Container' }}
        </button>
      </div>
    </div>

    <!-- KPI Metric Cards -->
    <div class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #eff6ff; color: #2563eb;">📦</div>
        <div class="kpi-info">
          <span class="kpi-label">Total Fleet Units</span>
          <span class="kpi-value">{{ total() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #ecfdf5; color: #059669;">📏</div>
        <div class="kpi-info">
          <span class="kpi-label">20ft Standard</span>
          <span class="kpi-value">{{ count20ft() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fdf4ff; color: #9333ea;">📐</div>
        <div class="kpi-info">
          <span class="kpi-label">40ft / 45ft HC</span>
          <span class="kpi-value">{{ count40ft() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fffbeb; color: #d97706;">🛡️</div>
        <div class="kpi-info">
          <span class="kpi-label">Normal Condition</span>
          <span class="kpi-value">{{ countNormal() }}</span>
        </div>
      </div>
    </div>

    <!-- Register / Edit Container Form -->
    <div *ngIf="showCreate() || isEditing()" class="card create-card" [class.edit-card]="isEditing()">
      <div class="card-title-bar">
        <h3>{{ isEditing() ? '✏️ Edit Container: ' + newCntr.containerNumber : '✨ Register New Container Asset' }}</h3>
        <span class="badge" [class.badge-indigo]="isEditing()">
          {{ isEditing() ? 'Edit Asset' : 'ISO 6346 Standard' }}
        </span>
      </div>

      <form (ngSubmit)="saveContainer()" class="create-form">
        <!-- 3-Part ISO 6346 Input Cluster -->
        <div class="iso-input-group" *ngIf="!isEditing()">
          <label class="iso-label">
            Container Number (ISO 6346) <span style="color: #ef4444;">*</span>
          </label>
          <div class="iso-fields">
            <div class="iso-box prefix-box" style="position: relative;">
              <div class="input-with-arrow">
                <input
                  type="text"
                  [(ngModel)]="cntrPrefix"
                  name="cntrPrefix"
                  (focus)="showPrefixPicker.set(true)"
                  (click)="$event.stopPropagation(); showPrefixPicker.set(true)"
                  (ngModelChange)="onPrefixChange($event)"
                  placeholder="MSCU"
                  maxlength="4"
                  required
                  autocomplete="off"
                  title="4-letter Owner Code + Category Identifier (U/J/Z)"
                />
                <button
                  type="button"
                  class="btn-arrow-toggle"
                  (click)="$event.stopPropagation(); showPrefixPicker.set(!showPrefixPicker())"
                  title="Show all shipping lines"
                >
                  ▼
                </button>
              </div>
              <span class="iso-sub">Owner (4 letters)</span>

              <!-- Dropdown Picker for all Shipping Lines -->
              <div *ngIf="showPrefixPicker()" class="prefix-dropdown" (click)="$event.stopPropagation()">
                <div class="prefix-dropdown-header">
                  <span>Major Shipping Line Codes</span>
                  <button type="button" class="btn-close-picker" (click)="showPrefixPicker.set(false)">✕</button>
                </div>
                <div class="prefix-dropdown-list">
                  <div
                    *ngFor="let p of filteredPrefixSuggestions()"
                    class="prefix-dropdown-item"
                    [class.selected]="cntrPrefix === p.code"
                    (click)="selectPrefix(p)"
                  >
                    <span class="prefix-code">{{ p.code }}</span>
                    <span class="prefix-owner">{{ p.owner }}</span>
                  </div>
                </div>
              </div>
            </div>

            <span class="iso-separator">–</span>

            <div class="iso-box serial-box">
              <input
                type="text"
                [(ngModel)]="cntrSerial"
                name="cntrSerial"
                (ngModelChange)="onSerialChange($event)"
                placeholder="123456"
                maxlength="6"
                inputmode="numeric"
                pattern="[0-9]{6}"
                required
                autocomplete="off"
                title="6-digit Serial Number"
              />
              <span class="iso-sub">Serial (6 digits)</span>
            </div>

            <span class="iso-separator">–</span>

            <div class="iso-box check-box">
              <input
                type="text"
                [value]="cntrCheckDigit"
                readonly
                tabindex="-1"
                placeholder="—"
                [class.valid]="cntrCheckDigit !== '' && !isMod10"
                [class.invalid]="isMod10"
                title="Auto-calculated ISO 6346 Check Digit (Modulo 11)"
              />
              <span class="iso-sub">Check (Auto)</span>
            </div>
          </div>

          <div class="iso-preview">
            <span *ngIf="newCntr.containerNumber" class="iso-status-valid">
              ✓ Valid ISO 6346: <b>{{ newCntr.containerNumber }}</b>
            </span>
            <span *ngIf="isMod10" class="iso-status-error">
              ⚠️ Modulo-11 is 10 — Invalid serial number per ISO 6346 standard.
            </span>
            <span *ngIf="!newCntr.containerNumber && !isMod10" class="iso-status-hint">
              Enter 4 letters (Owner Code) + 6 digits (Serial) to automatically calculate Check Digit.
            </span>
          </div>
        </div>

        <label>
          <span class="label-title">Container Type <span class="req">*</span></span>
          <select [(ngModel)]="newCntr.containerTypeId" name="containerTypeId" (change)="onTypeChange()" required>
            <option value="">Select Type…</option>
            <option *ngFor="let t of types()" [value]="t.id">{{ t.code }} — {{ t.name }}</option>
          </select>
        </label>

        <label>
          <span class="label-title">ISO Code</span>
          <input [(ngModel)]="newCntr.isoCode" name="isoCode" placeholder="22G1" required />
        </label>

        <label>
          <span class="label-title">Size (Feet)</span>
          <select [(ngModel)]="newCntr.sizeFeet" name="sizeFeet">
            <option [ngValue]="20">20 ft Standard</option>
            <option [ngValue]="40">40 ft High Cube</option>
            <option [ngValue]="45">45 ft Extra</option>
          </select>
        </label>

        <label>
          <span class="label-title">Max Weight (kg)</span>
          <input type="number" [(ngModel)]="newCntr.maxWeightKg" name="maxWeightKg" required />
        </label>

        <label>
          <span class="label-title">Tare Weight (kg)</span>
          <input type="number" [(ngModel)]="newCntr.tareWeightKg" name="tareWeightKg" required />
        </label>

        <label>
          <span class="label-title">Manufacture Date</span>
          <input type="date" [(ngModel)]="newCntr.manufactureDate" name="manufactureDate" required />
        </label>

        <label>
          <span class="label-title">Owner / Shipping Line</span>
          <input [(ngModel)]="newCntr.owner" name="owner" placeholder="e.g. MSC, CMA CGM, MAERSK" required />
        </label>

        <label>
          <span class="label-title">Condition</span>
          <select [(ngModel)]="newCntr.condition" name="condition">
            <option *ngFor="let c of conditions" [value]="c">{{ c }}</option>
          </select>
        </label>

        <div class="form-actions">
          <button type="submit" [disabled]="submitting() || !newCntr.containerNumber" class="btn-success">
            {{ submitting() ? (isEditing() ? 'Saving Changes…' : 'Registering Container…') : (isEditing() ? '✓ Save Changes' : '✓ Save Container') }}
          </button>
          <button type="button" class="secondary" (click)="cancelForm()">Cancel</button>

          <span *ngIf="successMsg()" class="success-msg">✓ {{ successMsg() }}</span>
          <span *ngIf="errorMsg()" class="error-msg">⚠️ {{ errorMsg() }}</span>
        </div>
      </form>
    </div>

    <!-- Filter & Search Bar -->
    <div class="filters-card card">
      <div class="search-input-wrap">
        <span class="search-icon">🔍</span>
        <input
          [(ngModel)]="search"
          placeholder="Search by container number or owner…"
          (ngModelChange)="refresh()"
        />
        <button *ngIf="search" class="clear-search" (click)="search = ''; refresh()">✕</button>
      </div>

      <select [(ngModel)]="condition" (change)="refresh()" class="filter-select">
        <option value="">All Conditions</option>
        <option *ngFor="let c of conditions" [value]="c">{{ c }}</option>
      </select>

      <select [(ngModel)]="sizeFilter" (change)="refresh()" class="filter-select">
        <option value="">All Sizes</option>
        <option value="20">20 ft</option>
        <option value="40">40 ft</option>
        <option value="45">45 ft</option>
      </select>
    </div>

    <!-- Containers Data Table -->
    <div class="table-container card">
      <table>
        <thead>
          <tr>
            <th>Container Number</th>
            <th>Owner</th>
            <th>Size</th>
            <th>ISO Code</th>
            <th>Max Weight</th>
            <th>Tare Weight</th>
            <th>Condition</th>
            <th>Manufactured</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr *ngFor="let c of filteredItems()">
            <td>
              <div class="cntr-cell">
                <span class="cntr-code">{{ c.containerNumber }}</span>
                <button
                  type="button"
                  class="btn-copy"
                  [title]="copiedId === c.id ? 'Copied!' : 'Copy Container Number'"
                  (click)="copyToClipboard(c.containerNumber, c.id)"
                >
                  {{ copiedId === c.id ? '✓' : '📋' }}
                </button>
              </div>
            </td>
            <td><b>{{ c.owner }}</b></td>
            <td>
              <span class="badge" [class.badge-primary]="c.sizeFeet === 20" [class.badge-indigo]="c.sizeFeet >= 40">
                {{ c.sizeFeet }} ft
              </span>
            </td>
            <td><code class="iso-badge">{{ c.isoCode }}</code></td>
            <td>{{ c.maxWeightKg | number }} kg</td>
            <td>{{ c.tareWeightKg | number }} kg</td>
            <td>
              <span [class.badge-danger]="c.condition !== 'Normal'" [class.badge-success]="c.condition === 'Normal'" class="badge">
                {{ c.condition }}
              </span>
            </td>
            <td>{{ c.manufactureDate | date:'mediumDate' }}</td>
            <td>
              <div class="action-btn-group">
                <button class="secondary btn-action" (click)="openGate(c.containerNumber)" title="Go to Gate Operations">
                  🚪 Gate
                </button>
                <button class="btn-edit-action" (click)="openEdit(c)" title="Edit Container Asset">
                  ✏️ Edit
                </button>
                <button class="btn-delete-action" (click)="deleteContainer(c)" title="Delete Container">
                  🗑️ Delete
                </button>
              </div>
            </td>
          </tr>
          <tr *ngIf="!loading() && filteredItems().length === 0">
            <td colspan="9" class="empty-state">
              <div class="empty-icon">📦</div>
              <div class="empty-text">No containers match your search or filter.</div>
              <button class="secondary" (click)="search = ''; condition = ''; sizeFilter = ''; refresh()">
                Reset Filters
              </button>
            </td>
          </tr>
        </tbody>
      </table>
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
      margin-bottom: 24px;
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
      margin-bottom: 16px;
    }
    .card-title-bar h3 { margin: 0; }

    .create-form {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 14px;
    }
    .label-title {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      font-size: 12px;
      font-weight: 600;
      color: #334155;
    }
    .req {
      color: #ef4444;
      font-weight: 700;
    }

    .iso-input-group {
      grid-column: 1 / -1;
      background: #f8fafc;
      border: 1px solid #cbd5e1;
      border-radius: var(--radius-sm);
      padding: 14px;
    }
    .iso-label {
      font-size: 13px;
      font-weight: 700;
      color: #0f172a;
      display: block;
      margin-bottom: 8px;
    }
    .iso-fields {
      display: flex;
      align-items: center;
      gap: 8px;
      flex-wrap: wrap;
    }
    .iso-box {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .prefix-box { width: 120px; }
    .serial-box { width: 140px; }
    .check-box { width: 80px; }
    .input-with-arrow {
      position: relative;
      display: flex;
      align-items: center;
    }
    .btn-arrow-toggle {
      position: absolute;
      right: 4px;
      background: none;
      border: none;
      font-size: 10px;
      color: #64748b;
      cursor: pointer;
      padding: 4px 6px;
      border-radius: 4px;
    }
    .btn-arrow-toggle:hover {
      background: #e2e8f0;
      color: #0f172a;
    }

    /* Custom Prefix Dropdown */
    .prefix-dropdown {
      position: absolute;
      top: calc(100% + 4px);
      left: 0;
      width: 260px;
      background: #ffffff;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
      z-index: 100;
      overflow: hidden;
    }
    .prefix-dropdown-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 12px;
      background: #f8fafc;
      border-bottom: 1px solid #e2e8f0;
      font-size: 11px;
      font-weight: 700;
      color: #475569;
    }
    .btn-close-picker {
      background: none;
      border: none;
      font-size: 12px;
      color: #94a3b8;
      cursor: pointer;
      padding: 0;
    }
    .btn-close-picker:hover { color: #0f172a; }
    .prefix-dropdown-list {
      max-height: 220px;
      overflow-y: auto;
    }
    .prefix-dropdown-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 8px 12px;
      cursor: pointer;
      font-size: 13px;
      border-bottom: 1px solid #f1f5f9;
      transition: background 0.15s ease;
    }
    .prefix-dropdown-item:last-child { border-bottom: none; }
    .prefix-dropdown-item:hover {
      background: #eff6ff;
    }
    .prefix-dropdown-item.selected {
      background: #e0e7ff;
      font-weight: 700;
    }
    .prefix-code {
      font-family: var(--font-mono);
      font-weight: 700;
      color: #1e293b;
    }
    .prefix-owner {
      font-size: 12px;
      color: #64748b;
    }

    .iso-separator {
      font-size: 20px;
      font-weight: bold;
      color: #94a3b8;
      align-self: center;
      margin-top: -16px;
    }
    .iso-box input {
      font-family: var(--font-mono);
      font-size: 16px;
      font-weight: 700;
      letter-spacing: 1px;
      text-align: center;
      padding: 8px 10px;
    }
    .check-box input {
      background: #f1f5f9;
      color: #475569;
      border: 2px dashed #cbd5e1;
      cursor: default;
    }
    .check-box input.valid {
      background: #ecfdf5;
      color: #047857;
      border: 2px solid #10b981;
    }
    .check-box input.invalid {
      background: #fef2f2;
      color: #b91c1c;
      border: 2px solid #ef4444;
    }
    .iso-sub {
      font-size: 11px;
      color: #64748b;
      text-align: center;
    }

    .iso-preview { margin-top: 8px; font-size: 12px; }
    .iso-status-valid { color: #15803d; font-weight: 600; }
    .iso-status-error { color: #b91c1c; font-weight: 600; }
    .iso-status-hint { color: #64748b; }

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
      display: flex;
      gap: 12px;
      padding: 12px 16px;
      margin-bottom: 16px;
      flex-wrap: wrap;
      align-items: center;
    }
    .search-input-wrap {
      position: relative;
      flex: 1;
      min-width: 220px;
      display: flex;
      align-items: center;
    }
    .search-icon { position: absolute; left: 10px; color: #94a3b8; }
    .search-input-wrap input { padding-left: 32px; width: 100%; }
    .clear-search {
      position: absolute;
      right: 8px;
      background: none;
      border: none;
      color: #94a3b8;
      cursor: pointer;
    }
    .filter-select { width: auto; min-width: 140px; }

    .table-container {
      overflow-x: auto;
      border-radius: var(--radius-md);
    }
    table { width: 100%; border-collapse: collapse; text-align: left; }
    th, td {
      padding: 12px 14px;
      border-bottom: 1px solid var(--color-border);
      font-size: 13px;
    }
    th { background: #f8fafc; color: #475569; font-weight: 600; }
    tr:hover { background: #f8fafc; }

    .cntr-cell { display: inline-flex; align-items: center; gap: 6px; }
    .cntr-code {
      font-family: var(--font-mono);
      font-weight: 700;
      color: #0f172a;
      letter-spacing: 0.5px;
    }
    .btn-copy {
      background: none;
      border: none;
      padding: 2px 4px;
      font-size: 12px;
      cursor: pointer;
      border-radius: 4px;
      color: #64748b;
    }
    .btn-copy:hover { background: #e2e8f0; }

    .iso-badge {
      background: #f1f5f9;
      color: #334155;
      padding: 2px 6px;
      border-radius: 4px;
      font-family: var(--font-mono);
      font-size: 11px;
    }
    .badge-indigo { background: #eef2ff; color: #4f46e5; }

    .action-btn-group {
      display: flex;
      gap: 6px;
      align-items: center;
    }
    .btn-action { padding: 4px 8px; font-size: 11px; }
    .btn-edit-action {
      background: #f1f5f9;
      color: #2563eb;
      border: 1px solid #cbd5e1;
      padding: 4px 8px;
      font-size: 11px;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 600;
    }
    .btn-edit-action:hover {
      background: #eff6ff;
      border-color: #93c5fd;
    }
    .btn-delete-action {
      background: none;
      color: #dc2626;
      border: 1px solid #fecaca;
      padding: 4px 8px;
      font-size: 11px;
      border-radius: 4px;
      cursor: pointer;
    }
    .btn-delete-action:hover {
      background: #fef2f2;
    }

    .empty-state { text-align: center; padding: 40px !important; }
    .empty-icon { font-size: 32px; margin-bottom: 8px; }
    .empty-text { color: #64748b; margin-bottom: 12px; }
  `],
})
export class ContainersComponent implements OnInit {
  private readonly svc = inject(ContainerService);
  private readonly router = inject(Router);

  loading = signal(true);
  submitting = signal(false);
  showCreate = signal(false);
  isEditing = signal(false);
  editingId: string | null = null;

  items = signal<Container[]>([]);
  types = signal<ContainerType[]>([]);
  total = signal(0);
  search = '';
  condition = '';
  sizeFilter = '';
  readonly conditions = ['Normal', 'Damaged', 'Dented', 'Twisted', 'Cracked', 'Leaking', 'Other'];
  successMsg = signal<string | null>(null);
  errorMsg = signal<string | null>(null);
  copiedId: string | null = null;

  // KPI computations
  count20ft = computed(() => this.items().filter(c => c.sizeFeet === 20).length);
  count40ft = computed(() => this.items().filter(c => c.sizeFeet >= 40).length);
  countNormal = computed(() => this.items().filter(c => c.condition === 'Normal').length);

  // Filtered items
  filteredItems = computed(() => {
    let result = this.items();
    if (this.sizeFilter) {
      const size = Number(this.sizeFilter);
      result = result.filter(c => c.sizeFeet === size);
    }
    return result;
  });

  // ISO 6346 3-Part inputs
  cntrPrefix = 'MSCU';
  cntrSerial = '123456';
  cntrCheckDigit = '6';
  isMod10 = false;
  readonly prefixSuggestions = POPULAR_PREFIXES;
  showPrefixPicker = signal(false);

  filteredPrefixSuggestions = computed(() => {
    const q = this.cntrPrefix.toUpperCase().trim();
    // If input is empty OR exactly matches a known 4-char prefix, show ALL items so user can browse and choose!
    if (!q || this.prefixSuggestions.some(p => p.code === q)) {
      return this.prefixSuggestions;
    }
    const filtered = this.prefixSuggestions.filter(p => p.code.includes(q) || p.owner.toUpperCase().includes(q));
    return filtered.length > 0 ? filtered : this.prefixSuggestions;
  });

  @HostListener('document:click')
  onDocumentClick(): void {
    this.showPrefixPicker.set(false);
  }

  selectPrefix(p: { code: string; owner: string }): void {
    this.cntrPrefix = p.code;
    this.newCntr.owner = p.owner;
    this.showPrefixPicker.set(false);
    this.recomputeIsoCheckDigit();
  }

  newCntr: Omit<Container, 'id' | 'tenantId'> = {
    containerNumber: 'MSCU1234566',
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
    this.svc.listTypes().subscribe((t) => {
      this.types.set(t);
      if (t.length > 0 && !this.newCntr.containerTypeId) {
        this.newCntr.containerTypeId = t[0].id;
        this.onTypeChange();
      }
    });
    this.recomputeIsoCheckDigit();
  }

  toggleCreate(): void {
    if (this.showCreate() || this.isEditing()) {
      this.cancelForm();
    } else {
      this.showCreate.set(true);
      this.isEditing.set(false);
      this.editingId = null;
      this.recomputeIsoCheckDigit();
    }
  }

  cancelForm(): void {
    this.showCreate.set(false);
    this.isEditing.set(false);
    this.editingId = null;
    this.errorMsg.set(null);
    this.successMsg.set(null);
  }

  openEdit(c: Container): void {
    this.isEditing.set(true);
    this.showCreate.set(false);
    this.editingId = c.id;
    this.errorMsg.set(null);
    this.successMsg.set(null);

    this.newCntr = {
      containerNumber: c.containerNumber,
      containerTypeId: c.containerTypeId,
      isoCode: c.isoCode,
      sizeFeet: c.sizeFeet,
      maxWeightKg: c.maxWeightKg,
      tareWeightKg: c.tareWeightKg,
      manufactureDate: new Date(c.manufactureDate).toISOString().split('T')[0],
      owner: c.owner,
      condition: c.condition,
    };

    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  deleteContainer(c: Container): void {
    if (!confirm(`Are you sure you want to delete container ${c.containerNumber}?`)) return;

    this.svc.delete(c.id).subscribe({
      next: () => {
        this.refresh();
      },
      error: (err) => {
        alert(err?.error?.detail ?? err?.error?.title ?? 'Failed to delete container. It may be occupying a yard slot.');
      }
    });
  }

  onPrefixChange(val: string): void {
    const cleaned = (val || '').replace(/[^a-zA-Z]/g, '').toUpperCase().slice(0, 4);
    this.cntrPrefix = cleaned;

    const match = this.prefixSuggestions.find(p => p.code === cleaned);
    if (match && (!this.newCntr.owner || this.prefixSuggestions.some(p => p.owner === this.newCntr.owner))) {
      this.newCntr.owner = match.owner;
    }

    this.recomputeIsoCheckDigit();
  }

  onSerialChange(val: string): void {
    const cleaned = (val || '').replace(/\D/g, '').slice(0, 6);
    this.cntrSerial = cleaned;
    this.recomputeIsoCheckDigit();
  }

  private recomputeIsoCheckDigit(): void {
    if (this.cntrPrefix.length === 4 && this.cntrSerial.length === 6) {
      const tenChars = (this.cntrPrefix + this.cntrSerial).toUpperCase();
      let sum = 0;
      for (let i = 0; i < 10; i++) {
        const char = tenChars[i];
        let val: number;
        if (char >= '0' && char <= '9') {
          val = parseInt(char, 10);
        } else if (ISO_LETTER_VALUES[char] !== undefined) {
          val = ISO_LETTER_VALUES[char];
        } else {
          this.cntrCheckDigit = '';
          this.isMod10 = false;
          this.newCntr.containerNumber = '';
          return;
        }
        sum += val * (1 << i);
      }

      const mod = sum % 11;
      if (mod === 10) {
        this.cntrCheckDigit = '!';
        this.isMod10 = true;
        this.newCntr.containerNumber = '';
      } else {
        this.cntrCheckDigit = mod.toString();
        this.isMod10 = false;
        this.newCntr.containerNumber = `${this.cntrPrefix}${this.cntrSerial}${this.cntrCheckDigit}`;
      }
    } else {
      this.cntrCheckDigit = '';
      this.isMod10 = false;
      this.newCntr.containerNumber = '';
    }
  }

  onTypeChange(): void {
    const selected = this.types().find(t => t.id === this.newCntr.containerTypeId);
    if (selected) {
      this.newCntr.isoCode = selected.code;
      if (selected.code.startsWith('4')) {
        this.newCntr.sizeFeet = 40;
        this.newCntr.tareWeightKg = 3800;
        this.newCntr.maxWeightKg = 32500;
      } else {
        this.newCntr.sizeFeet = 20;
        this.newCntr.tareWeightKg = 2200;
        this.newCntr.maxWeightKg = 30480;
      }
    }
  }

  copyToClipboard(text: string, id: string): void {
    navigator.clipboard.writeText(text);
    this.copiedId = id;
    setTimeout(() => {
      if (this.copiedId === id) this.copiedId = null;
    }, 1500);
  }

  openGate(cntrNumber: string): void {
    this.router.navigate(['/gate'], { queryParams: { cntr: cntrNumber } });
  }

  saveContainer(): void {
    if (this.isEditing() && this.editingId) {
      this.updateContainer();
    } else {
      this.createContainer();
    }
  }

  createContainer(): void {
    if (!this.newCntr.containerNumber) {
      this.errorMsg.set('Please provide a valid 11-character ISO 6346 Container Number.');
      return;
    }

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
        this.cntrSerial = '';
        this.recomputeIsoCheckDigit();
        this.refresh();
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMsg.set(err?.error?.detail ?? err?.error?.title ?? 'Failed to register container.');
      }
    });
  }

  updateContainer(): void {
    if (!this.editingId) return;

    this.submitting.set(true);
    this.errorMsg.set(null);
    this.successMsg.set(null);

    this.svc.update(this.editingId, {
      ...this.newCntr,
      manufactureDate: new Date(this.newCntr.manufactureDate).toISOString(),
    }).subscribe({
      next: (updated) => {
        this.submitting.set(false);
        this.successMsg.set(`Container ${updated.containerNumber} updated successfully!`);
        this.refresh();
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMsg.set(err?.error?.detail ?? err?.error?.title ?? 'Failed to update container.');
      }
    });
  }

  refresh(): void {
    this.loading.set(true);
    this.svc.list(1, 100, undefined, this.condition || undefined, this.search || undefined).subscribe({
      next: (p) => { this.items.set(p.items); this.total.set(p.totalCount); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
