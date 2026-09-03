/**
 * Component thao tác Cổng (Gate Component).
 * Giao diện thực hiện 3 nghiệp vụ cổng chính của hệ thống TOS:
 * - Gate-In (Receiving): Tạo biên nhận EIR khi container vào bãi,
 *   chọn Block đích, nhập thông tin phương tiện và tài xế.
 * - Yard Relocation (Move): Di chuyển container trong nội bộ bãi đến vị trí Slot mới.
 *   Áp dụng quy tắc Bay Parity: container 20ft → Odd Bay (1,3,5...), container 40ft → Even Bay (2,4,6...).
 * - Gate-Out (Delivery): Xuất container dựa trên Delivery Order đã được phê duyệt,
 *   cập nhật số lượng đã giao trên đơn.
 * Dropdown gợi ý số container (từ API + presets phổ biến).
 * Bản quyền (c) 2026 TechSpherex.
 */
import { Component, OnInit, inject, signal, computed, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { GateService } from '../../core/services/gate.service';
import { Block, DeliveryOrder, LineOperator, Container } from '../../core/models/api.models';
import { DeliveryOrderService } from '../../core/services/delivery-order.service';
import { ContainerService } from '../../core/services/container.service';
import { YardService } from '../../core/services/yard.service';

export type GateOperationTarget = 'in' | 'move' | 'out';

const POPULAR_CONTAINER_PRESETS: { containerNumber: string; owner: string; sizeFeet: number; isoCode: string; condition: string }[] = [
  { containerNumber: 'MSCU1234566', owner: 'MSC', sizeFeet: 20, isoCode: '22G1', condition: 'Normal' },
  { containerNumber: 'CMAU1234564', owner: 'CMA CGM', sizeFeet: 40, isoCode: '42G1', condition: 'Normal' },
  { containerNumber: 'MSKU2345678', owner: 'MAERSK', sizeFeet: 20, isoCode: '22G1', condition: 'Normal' },
  { containerNumber: 'COSU8765432', owner: 'COSCO', sizeFeet: 40, isoCode: '42G1', condition: 'Normal' },
  { containerNumber: 'ONEU9876543', owner: 'ONE', sizeFeet: 20, isoCode: '22G1', condition: 'Normal' },
  { containerNumber: 'HLCU3456789', owner: 'Hapag-Lloyd', sizeFeet: 40, isoCode: '42G1', condition: 'Normal' },
  { containerNumber: 'EMCU5678901', owner: 'Evergreen', sizeFeet: 20, isoCode: '22G1', condition: 'Normal' },
  { containerNumber: 'TEMU6789012', owner: 'Textainer', sizeFeet: 40, isoCode: '42G1', condition: 'Normal' }
];

@Component({
  selector: 'app-gate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page-header">
      <div>
        <h2>Gate & Yard Operations (TOS)</h2>
        <p class="muted">Gate-In starts an Equipment Interchange Receipt (EIR); Gate-Out validates and discharges Delivery Orders.</p>
      </div>
      <button (click)="loadAllData()" class="secondary" title="Refresh all master data">
        🔄 Refresh Data
      </button>
    </div>

    <!-- Quick Mode Selector Tabs -->
    <div class="mode-tabs">
      <button
        type="button"
        class="tab-btn"
        [class.active-in]="activeTab === 'all' || activeTab === 'in'"
        (click)="activeTab = 'in'"
      >
        📥 Gate-In (Receiving)
      </button>
      <button
        type="button"
        class="tab-btn"
        [class.active-move]="activeTab === 'all' || activeTab === 'move'"
        (click)="activeTab = 'move'"
      >
        🔄 Yard Relocation (Move)
      </button>
      <button
        type="button"
        class="tab-btn"
        [class.active-out]="activeTab === 'all' || activeTab === 'out'"
        (click)="activeTab = 'out'"
      >
        📤 Gate-Out (Delivery)
      </button>
      <button
        type="button"
        class="tab-btn tab-all"
        [class.active-all]="activeTab === 'all'"
        (click)="activeTab = 'all'"
      >
        ⚡ View All 3 Columns
      </button>
    </div>

    <div class="grid-3" [class.single-col]="activeTab !== 'all'">
      <!-- Gate In -->
      <section class="card op-card card-in" *ngIf="activeTab === 'all' || activeTab === 'in'">
        <div class="op-card-header">
          <div class="op-badge-icon in-icon">📥</div>
          <div>
            <h3>Gate-In Container</h3>
            <span class="op-sub">Inward Gate EIR Generation</span>
          </div>
        </div>

        <form (ngSubmit)="gateIn()" class="op-form">
          <!-- Click & Focus Dropdown for Gate In -->
          <div class="dropdown-wrap gate-dropdown-in">
            <label>
              <div class="field-label-row">
                <span class="label-title">Container Number (ISO 6346) <span class="req">*</span></span>
                <button type="button" class="btn-picker-link" (click)="toggleDropdown('in', $event)">
                  ⚡ {{ showDropdownIn ? 'Hide options ✕' : 'Show suggestions ▼' }}
                </button>
              </div>
              <div class="input-with-picker">
                <input
                  [(ngModel)]="inForm.containerNumber"
                  name="inNumber"
                  (focus)="openDropdown('in')"
                  (click)="openDropdown('in')"
                  placeholder="Click to pick or enter container…"
                  autocomplete="off"
                  required
                />
                <button type="button" class="btn-picker-btn" (click)="toggleDropdown('in', $event)" title="Container suggestions">
                  {{ showDropdownIn ? '▲' : '▼' }}
                </button>
              </div>
            </label>

            <!-- Dropdown Menu (Stays open comfortably) -->
            <div class="hover-dropdown" *ngIf="showDropdownIn" (click)="$event.stopPropagation()">
              <div class="dropdown-header">
                <span>⚡ Available Containers ({{ getFilteredSuggestions(inForm.containerNumber).length }})</span>
                <button type="button" class="btn-close-drop" (click)="showDropdownIn = false">✕</button>
              </div>
              <div class="dropdown-items">
                <div
                  *ngFor="let c of getFilteredSuggestions(inForm.containerNumber)"
                  class="dropdown-item"
                  (click)="selectDropdownContainer(c, 'in')"
                >
                  <div class="item-left">
                    <span class="item-num">{{ c.containerNumber }}</span>
                    <span class="item-owner">{{ c.owner }}</span>
                  </div>
                  <div class="item-right">
                    <span class="badge" [class.badge-indigo]="c.sizeFeet >= 40">{{ c.sizeFeet }} ft</span>
                    <span class="badge badge-muted">{{ c.isoCode }}</span>
                  </div>
                </div>
                <div *ngIf="getFilteredSuggestions(inForm.containerNumber).length === 0" class="empty-drop">
                  No matching containers found
                </div>
              </div>
            </div>
          </div>

          <label>
            <span class="label-title">Line Operator (Shipping Line) <span class="req">*</span></span>
            <select [(ngModel)]="inForm.lineOperatorId" name="inOp" required>
              <option value="">Select Operator…</option>
              <option *ngFor="let l of operators()" [value]="l.id">{{ l.code }} — {{ l.name }}</option>
            </select>
          </label>

          <label>
            <span class="label-title">Target Yard Block <span class="req">*</span></span>
            <select [(ngModel)]="inForm.blockId" name="inBlock" required>
              <option value="">Select Block…</option>
              <option *ngFor="let b of blocks()" [value]="b.id">
                Block {{ b.code }} ({{ b.name }}) {{ b.isVirtual ? '[Virtual]' : '' }}
              </option>
            </select>
          </label>

          <div class="slot-grid">
            <label>
              <span class="label-title">Bay</span>
              <input type="number" [(ngModel)]="inForm.bay" name="inBay" placeholder="1" />
            </label>
            <label>
              <span class="label-title">Row</span>
              <input type="number" [(ngModel)]="inForm.row" name="inRow" placeholder="1" />
            </label>
            <label>
              <span class="label-title">Tier</span>
              <input type="number" [(ngModel)]="inForm.tier" name="inTier" placeholder="1" />
            </label>
          </div>

          <div class="row-2">
            <label>
              <span class="label-title">Vehicle Plate <span class="req">*</span></span>
              <input [(ngModel)]="inForm.vehicleInNumber" name="inVehicle" placeholder="KH-9999" required />
            </label>
            <label>
              <span class="label-title">Driver Name</span>
              <input [(ngModel)]="inForm.driverInName" name="inDriver" placeholder="Nguyen Van A" />
            </label>
          </div>

          <label>
            <span class="label-title">Classification</span>
            <select [(ngModel)]="inForm.classification" name="inClass">
              <option value="Export">Export</option>
              <option value="Import">Import</option>
              <option value="Domestic">Domestic</option>
              <option value="A">Grade A</option>
              <option value="B">Grade B</option>
              <option value="C">Grade C</option>
            </select>
          </label>

          <button type="submit" [disabled]="busy()" class="btn-submit-in">
            {{ busy() ? 'Processing Gate-In…' : '📥 Confirm Gate-In' }}
          </button>

          <div class="alert-success" *ngIf="lastIn()">
            ✓ EIR Generated Successfully!<br>
            <span class="mono">Movement ID: {{ lastIn() }}</span>
          </div>
          <div class="alert-error" *ngIf="errorIn()">⚠️ {{ errorIn() }}</div>
        </form>
      </section>

      <!-- Move Container -->
      <section class="card op-card card-move" *ngIf="activeTab === 'all' || activeTab === 'move'">
        <div class="op-card-header">
          <div class="op-badge-icon move-icon">🔄</div>
          <div>
            <h3>Yard Relocation</h3>
            <span class="op-sub">Internal Slot Reassignment</span>
          </div>
        </div>

        <form (ngSubmit)="moveContainer()" class="op-form">
          <!-- Click & Focus Dropdown for Move -->
          <div class="dropdown-wrap gate-dropdown-move">
            <label>
              <div class="field-label-row">
                <span class="label-title">Container Number <span class="req">*</span></span>
                <button type="button" class="btn-picker-link" (click)="toggleDropdown('move', $event)">
                  ⚡ {{ showDropdownMove ? 'Hide options ✕' : 'Show suggestions ▼' }}
                </button>
              </div>
              <div class="input-with-picker">
                <input
                  [(ngModel)]="moveForm.containerNumber"
                  name="moveContainerNumber"
                  (focus)="openDropdown('move')"
                  (click)="openDropdown('move')"
                  placeholder="Click to pick or enter container…"
                  autocomplete="off"
                  required
                />
                <button type="button" class="btn-picker-btn" (click)="toggleDropdown('move', $event)" title="Container suggestions">
                  {{ showDropdownMove ? '▲' : '▼' }}
                </button>
              </div>
            </label>

            <div class="hover-dropdown" *ngIf="showDropdownMove" (click)="$event.stopPropagation()">
              <div class="dropdown-header">
                <span>⚡ Available Containers ({{ getFilteredSuggestions(moveForm.containerNumber).length }})</span>
                <button type="button" class="btn-close-drop" (click)="showDropdownMove = false">✕</button>
              </div>
              <div class="dropdown-items">
                <div
                  *ngFor="let c of getFilteredSuggestions(moveForm.containerNumber)"
                  class="dropdown-item"
                  (click)="selectDropdownContainer(c, 'move')"
                >
                  <div class="item-left">
                    <span class="item-num">{{ c.containerNumber }}</span>
                    <span class="item-owner">{{ c.owner }}</span>
                  </div>
                  <div class="item-right">
                    <span class="badge" [class.badge-indigo]="c.sizeFeet >= 40">{{ c.sizeFeet }} ft</span>
                    <span class="badge badge-muted">{{ c.isoCode }}</span>
                  </div>
                </div>
                <div *ngIf="getFilteredSuggestions(moveForm.containerNumber).length === 0" class="empty-drop">
                  No matching containers found
                </div>
              </div>
            </div>
          </div>

          <label>
            <span class="label-title">Target Yard Block <span class="req">*</span></span>
            <select [(ngModel)]="moveForm.newBlockId" name="moveBlockId" required>
              <option value="">Select Block…</option>
              <option *ngFor="let b of blocks()" [value]="b.id">Block {{ b.code }} ({{ b.name }})</option>
            </select>
          </label>

          <div class="slot-grid">
            <label>
              <span class="label-title">New Bay</span>
              <input type="number" [(ngModel)]="moveForm.newBay" name="moveBay" placeholder="3" required />
            </label>
            <label>
              <span class="label-title">New Row</span>
              <input type="number" [(ngModel)]="moveForm.newRow" name="moveRow" placeholder="1" required />
            </label>
            <label>
              <span class="label-title">New Tier</span>
              <input type="number" [(ngModel)]="moveForm.newTier" name="moveTier" placeholder="1" required />
            </label>
          </div>

          <div class="rule-hint">
            💡 <b>Rule:</b> 20ft requires <b>Odd Bay</b> (1, 3, 5); 40ft requires <b>Even Bay</b> (2, 4, 6).
          </div>

          <button type="submit" [disabled]="busy()" class="btn-submit-move">
            {{ busy() ? 'Moving Container…' : '🔄 Relocate Container' }}
          </button>

          <div class="alert-success" *ngIf="moveSuccess()">✓ {{ moveSuccess() }}</div>
          <div class="alert-error" *ngIf="moveError()">⚠️ {{ moveError() }}</div>
        </form>
      </section>

      <!-- Gate Out -->
      <section class="card op-card card-out" *ngIf="activeTab === 'all' || activeTab === 'out'">
        <div class="op-card-header">
          <div class="op-badge-icon out-icon">📤</div>
          <div>
            <h3>Gate-Out Discharge</h3>
            <span class="op-sub">Delivery Order Release & EIR Close</span>
          </div>
        </div>

        <form (ngSubmit)="gateOut()" class="op-form">
          <!-- Click & Focus Dropdown for Gate Out -->
          <div class="dropdown-wrap gate-dropdown-out">
            <label>
              <div class="field-label-row">
                <span class="label-title">Container Number <span class="req">*</span></span>
                <button type="button" class="btn-picker-link" (click)="toggleDropdown('out', $event)">
                  ⚡ {{ showDropdownOut ? 'Hide options ✕' : 'Show suggestions ▼' }}
                </button>
              </div>
              <div class="input-with-picker">
                <input
                  [(ngModel)]="outForm.containerNumber"
                  name="outContainerNumber"
                  (focus)="openDropdown('out')"
                  (click)="openDropdown('out')"
                  placeholder="Click to pick or enter container…"
                  autocomplete="off"
                  required
                />
                <button type="button" class="btn-picker-btn" (click)="toggleDropdown('out', $event)" title="Container suggestions">
                  {{ showDropdownOut ? '▲' : '▼' }}
                </button>
              </div>
            </label>

            <div class="hover-dropdown" *ngIf="showDropdownOut" (click)="$event.stopPropagation()">
              <div class="dropdown-header">
                <span>⚡ Available Containers ({{ getFilteredSuggestions(outForm.containerNumber).length }})</span>
                <button type="button" class="btn-close-drop" (click)="showDropdownOut = false">✕</button>
              </div>
              <div class="dropdown-items">
                <div
                  *ngFor="let c of getFilteredSuggestions(outForm.containerNumber)"
                  class="dropdown-item"
                  (click)="selectDropdownContainer(c, 'out')"
                >
                  <div class="item-left">
                    <span class="item-num">{{ c.containerNumber }}</span>
                    <span class="item-owner">{{ c.owner }}</span>
                  </div>
                  <div class="item-right">
                    <span class="badge" [class.badge-indigo]="c.sizeFeet >= 40">{{ c.sizeFeet }} ft</span>
                    <span class="badge badge-muted">{{ c.isoCode }}</span>
                  </div>
                </div>
                <div *ngIf="getFilteredSuggestions(outForm.containerNumber).length === 0" class="empty-drop">
                  No matching containers found
                </div>
              </div>
            </div>
          </div>

          <label>
            <span class="label-title">Delivery Order (Release Permit) <span class="req">*</span></span>
            <select [(ngModel)]="outForm.deliveryOrderId" name="outOrder" required>
              <option value="">Select Delivery Order…</option>
              <option *ngFor="let do of deliveryOrders()" [value]="do.id">
                {{ do.orderNumber }} [{{ getDoTypes(do) }}] — {{ do.lineOperatorName }}
              </option>
            </select>
          </label>

          <div class="row-2">
            <label>
              <span class="label-title">Vehicle Plate <span class="req">*</span></span>
              <input [(ngModel)]="outForm.vehicleOutNumber" name="outVehicle" placeholder="KH-8888" required />
            </label>
            <label>
              <span class="label-title">Driver Name</span>
              <input [(ngModel)]="outForm.driverOutName" name="outDriver" placeholder="Tran Van B" />
            </label>
          </div>

          <label>
            <span class="label-title">Condition at Gate Out</span>
            <select [(ngModel)]="outForm.conditionAtGateOut" name="outCond">
              <option value="Normal">Normal</option>
              <option value="Damaged">Damaged</option>
              <option value="Dented">Dented</option>
              <option value="Twisted">Twisted</option>
              <option value="Cracked">Cracked</option>
              <option value="Leaking">Leaking</option>
              <option value="Other">Other</option>
            </select>
          </label>

          <button type="submit" [disabled]="busy()" class="btn-submit-out">
            {{ busy() ? 'Discharging Container…' : '📤 Confirm Gate-Out' }}
          </button>

          <div class="alert-success" *ngIf="lastOut()">
            ✓ Container discharged successfully!<br>
            <span class="mono">EIR Record: {{ lastOut() }}</span>
          </div>
          <div class="alert-error" *ngIf="errorOut()">⚠️ {{ errorOut() }}</div>
        </form>
      </section>
    </div>
  `,
  styles: [`
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .mode-tabs {
      display: flex;
      gap: 8px;
      margin-bottom: 20px;
      flex-wrap: wrap;
    }
    .tab-btn {
      background: #ffffff;
      color: #475569;
      border: 1px solid var(--color-border);
      padding: 8px 14px;
      font-size: 13px;
      font-weight: 600;
      border-radius: 8px;
    }
    .tab-btn:hover {
      background: #f8fafc;
      color: #0f172a;
    }
    .tab-btn.active-in { background: #eff6ff; color: #1d4ed8; border-color: #93c5fd; }
    .tab-btn.active-move { background: #eef2ff; color: #4338ca; border-color: #a5b4fc; }
    .tab-btn.active-out { background: #ecfdf5; color: #047857; border-color: #6ee7b7; }
    .tab-btn.active-all { background: #0f172a; color: #ffffff; border-color: #0f172a; }

    .grid-3 {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
      gap: 20px;
    }
    .grid-3.single-col {
      grid-template-columns: minmax(320px, 640px);
    }

    .op-card {
      padding: 22px;
      border-top: 4px solid transparent;
      display: flex;
      flex-direction: column;
      position: relative;
    }
    .card-in { border-top-color: #2563eb; }
    .card-move { border-top-color: #6366f1; }
    .card-out { border-top-color: #10b981; }

    .op-card-header {
      display: flex;
      align-items: center;
      gap: 12px;
      margin-bottom: 18px;
      padding-bottom: 14px;
      border-bottom: 1px solid #f1f5f9;
    }
    .op-card-header h3 { margin: 0; font-size: 16px; }
    .op-sub { font-size: 11px; color: #64748b; }

    .op-badge-icon {
      width: 40px;
      height: 40px;
      border-radius: 10px;
      display: grid;
      place-items: center;
      font-size: 18px;
      flex-shrink: 0;
    }
    .in-icon { background: #eff6ff; color: #2563eb; }
    .move-icon { background: #eef2ff; color: #6366f1; }
    .out-icon { background: #ecfdf5; color: #10b981; }

    .op-form {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .field-label-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      margin-bottom: 2px;
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
    .btn-picker-link {
      background: none;
      border: none;
      color: #2563eb;
      font-size: 11px;
      font-weight: 600;
      padding: 0;
      cursor: pointer;
      text-decoration: underline;
    }
    .btn-picker-link:hover { color: #1d4ed8; }

    /* Persistent Dropdown */
    .dropdown-wrap {
      position: relative;
      display: flex;
      flex-direction: column;
    }
    .input-with-picker {
      display: flex;
      gap: 4px;
    }
    .input-with-picker input {
      flex: 1;
      font-family: var(--font-mono);
      font-weight: 600;
      cursor: pointer;
    }
    .btn-picker-btn {
      padding: 6px 12px;
      font-size: 13px;
      background: #f1f5f9;
      color: #1e293b;
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      cursor: pointer;
    }
    .btn-picker-btn:hover {
      background: #e2e8f0;
    }

    .hover-dropdown {
      position: absolute;
      top: 100%;
      left: 0;
      right: 0;
      z-index: 100;
      background: #ffffff;
      border: 1px solid #cbd5e1;
      border-radius: 10px;
      box-shadow: 0 12px 28px -4px rgba(0, 0, 0, 0.18), 0 8px 10px -6px rgba(0, 0, 0, 0.1);
      padding: 6px 0;
      margin-top: 4px;
      animation: fadeIn 0.15s ease;
      display: flex;
      flex-direction: column;
    }
    .dropdown-header {
      padding: 8px 12px;
      font-size: 11px;
      font-weight: 700;
      color: #475569;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      border-bottom: 1px solid #f1f5f9;
      display: flex;
      justify-content: space-between;
      align-items: center;
      background: #f8fafc;
      border-top-left-radius: 9px;
      border-top-right-radius: 9px;
    }
    .btn-close-drop {
      background: none;
      border: none;
      font-size: 13px;
      color: #64748b;
      cursor: pointer;
      padding: 0 4px;
    }
    .btn-close-drop:hover { color: #0f172a; }

    .dropdown-items {
      overflow-y: auto;
      max-height: 220px;
      padding: 4px 0;
    }
    .dropdown-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 9px 12px;
      cursor: pointer;
      transition: background 0.12s ease;
    }
    .dropdown-item:hover {
      background: #eff6ff;
    }
    .item-left {
      display: flex;
      align-items: center;
      gap: 8px;
    }
    .item-num {
      font-family: var(--font-mono);
      font-weight: 700;
      color: #0f172a;
      font-size: 13px;
    }
    .item-owner {
      font-size: 11px;
      color: #475569;
      background: #f1f5f9;
      padding: 1px 6px;
      border-radius: 4px;
      font-weight: 500;
    }
    .item-right {
      display: flex;
      gap: 4px;
    }
    .badge-indigo { background: #eef2ff; color: #4f46e5; }
    .empty-drop {
      padding: 16px;
      text-align: center;
      font-size: 12px;
      color: #64748b;
    }

    .row-2 {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 10px;
    }

    .slot-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 8px;
      width: 100%;
    }
    .slot-grid label { min-width: 0; }
    .slot-grid input {
      width: 100%;
      min-width: 0;
      text-align: center;
      font-weight: 600;
      font-family: var(--font-mono);
    }

    .rule-hint {
      font-size: 11px;
      color: #64748b;
      background: #f8fafc;
      padding: 8px 10px;
      border-radius: 6px;
      border: 1px dashed #cbd5e1;
    }

    .btn-submit-in {
      background: #2563eb;
      color: #fff;
      padding: 10px 16px;
      margin-top: 6px;
      font-weight: 600;
    }
    .btn-submit-in:hover { background: #1d4ed8; }

    .btn-submit-move {
      background: #6366f1;
      color: #fff;
      padding: 10px 16px;
      margin-top: 6px;
      font-weight: 600;
    }
    .btn-submit-move:hover { background: #4f46e5; }

    .btn-submit-out {
      background: #10b981;
      color: #fff;
      padding: 10px 16px;
      margin-top: 6px;
      font-weight: 600;
    }
    .btn-submit-out:hover { background: #059669; }

    .alert-success {
      background: #ecfdf5;
      color: #047857;
      padding: 10px 12px;
      border-radius: 6px;
      font-size: 12px;
      border: 1px solid #a7f3d0;
      line-height: 1.4;
    }
    .alert-error {
      background: #fef2f2;
      color: #b91c1c;
      padding: 10px 12px;
      border-radius: 6px;
      font-size: 12px;
      border: 1px solid #fecaca;
    }
    .mono { font-family: var(--font-mono); font-size: 11px; }
  `],
})
export class GateComponent implements OnInit {
  private readonly gate = inject(GateService);
  private readonly orderSvc = inject(DeliveryOrderService);
  private readonly cntrSvc = inject(ContainerService);
  private readonly yardSvc = inject(YardService);
  private readonly route = inject(ActivatedRoute);
  private readonly eRef = inject(ElementRef);

  busy = signal(false);
  operators = signal<LineOperator[]>([]);
  blocks = signal<Block[]>([]);
  deliveryOrders = signal<DeliveryOrder[]>([]);
  registeredContainers = signal<Container[]>([]);
  typeCodeMap = new Map<string, string>();
  activeTab: 'all' | 'in' | 'move' | 'out' = 'all';

  // Persistent Dropdown States
  showDropdownIn = false;
  showDropdownMove = false;
  showDropdownOut = false;

  lastIn = signal<string | null>(null);
  lastOut = signal<string | null>(null);
  errorIn = signal<string | null>(null);
  errorOut = signal<string | null>(null);
  moveSuccess = signal<string | null>(null);
  moveError = signal<string | null>(null);

  // Forms with clean defaults
  inForm = {
    containerNumber: '',
    lineOperatorId: '',
    blockId: '',
    bay: 1,
    row: 1,
    tier: 1,
    vehicleInNumber: 'KH-9999',
    driverInName: 'Nguyen Van A',
    classification: 'Export',
    conditionAtGateIn: 'Normal',
  };

  moveForm = {
    containerNumber: '',
    newBlockId: '',
    newBay: 3,
    newRow: 1,
    newTier: 1,
  };

  outForm = {
    containerNumber: '',
    deliveryOrderId: '',
    vehicleOutNumber: 'KH-8888',
    driverOutName: 'Tran Van B',
    conditionAtGateOut: 'Normal',
  };

  // Combined container list for dropdown
  allAvailableContainers = computed(() => {
    const fromApi = this.registeredContainers();
    const existingNums = new Set(fromApi.map(c => c.containerNumber));
    const presetsToAdd = POPULAR_CONTAINER_PRESETS.filter(p => !existingNums.has(p.containerNumber));

    return [
      ...fromApi.map(c => ({
        containerNumber: c.containerNumber,
        owner: c.owner,
        sizeFeet: c.sizeFeet,
        isoCode: c.isoCode,
        condition: c.condition,
      })),
      ...presetsToAdd
    ];
  });

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    // If click is outside the dropdown container, close them
    const target = event.target as HTMLElement;
    if (!target.closest('.gate-dropdown-in')) this.showDropdownIn = false;
    if (!target.closest('.gate-dropdown-move')) this.showDropdownMove = false;
    if (!target.closest('.gate-dropdown-out')) this.showDropdownOut = false;
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['cntr']) {
        this.inForm.containerNumber = params['cntr'];
        this.moveForm.containerNumber = params['cntr'];
        this.outForm.containerNumber = params['cntr'];
      }
    });

    this.cntrSvc.listTypes().subscribe({
      next: (types) => {
        types.forEach(t => this.typeCodeMap.set(t.id, t.code));
        this.loadAllData();
      }
    });

    this.cntrSvc.list(1, 100).subscribe({
      next: (p) => this.registeredContainers.set(p.items)
    });
  }

  loadAllData(): void {
    this.orderSvc.lineOperators().subscribe({
      next: (l) => {
        this.operators.set(l);
        if (l.length > 0 && !this.inForm.lineOperatorId) this.inForm.lineOperatorId = l[0].id;
      }
    });

    this.yardSvc.listDepots().subscribe({
      next: (depots) => {
        if (depots.length > 0) {
          this.yardSvc.getYardMap(depots[0].id).subscribe({
            next: (map) => {
              this.blocks.set(map.blocks);
              if (map.blocks.length > 0) {
                const physBlock = map.blocks.find(b => !b.isVirtual) ?? map.blocks[0];
                this.inForm.blockId = physBlock.id;
                this.moveForm.newBlockId = physBlock.id;
              }
            }
          });
        }
      }
    });

    this.orderSvc.list().subscribe({
      next: (dos) => {
        this.deliveryOrders.set(dos);
        if (dos.length > 0) {
          this.outForm.deliveryOrderId = dos[0].id;
        }
      }
    });

    this.cntrSvc.list(1, 100).subscribe({
      next: (p) => this.registeredContainers.set(p.items)
    });
  }

  openDropdown(target: GateOperationTarget): void {
    if (target === 'in') this.showDropdownIn = true;
    if (target === 'move') this.showDropdownMove = true;
    if (target === 'out') this.showDropdownOut = true;
  }

  toggleDropdown(target: GateOperationTarget, event: MouseEvent): void {
    event.stopPropagation();
    if (target === 'in') this.showDropdownIn = !this.showDropdownIn;
    if (target === 'move') this.showDropdownMove = !this.showDropdownMove;
    if (target === 'out') this.showDropdownOut = !this.showDropdownOut;
  }

  getFilteredSuggestions(currentInput: string): { containerNumber: string; owner: string; sizeFeet: number; isoCode: string; condition: string }[] {
    const list = this.allAvailableContainers();
    const q = (currentInput || '').toLowerCase().trim();
    // If empty OR matches an existing full container number, return ALL containers so user can browse and pick!
    if (!q || list.some(c => c.containerNumber.toLowerCase() === q)) {
      return list;
    }
    const filtered = list.filter(c =>
      c.containerNumber.toLowerCase().includes(q) ||
      c.owner.toLowerCase().includes(q) ||
      c.isoCode.toLowerCase().includes(q)
    );
    return filtered.length > 0 ? filtered : list;
  }

  selectDropdownContainer(c: { containerNumber: string; owner: string }, target: GateOperationTarget): void {
    if (target === 'in') {
      this.inForm.containerNumber = c.containerNumber;
      this.showDropdownIn = false;
      const matchedOp = this.operators().find(
        op => op.name.toLowerCase().includes(c.owner.toLowerCase()) ||
              op.code.toLowerCase() === c.owner.toLowerCase()
      );
      if (matchedOp) {
        this.inForm.lineOperatorId = matchedOp.id;
      }
    } else if (target === 'move') {
      this.moveForm.containerNumber = c.containerNumber;
      this.showDropdownMove = false;
    } else if (target === 'out') {
      this.outForm.containerNumber = c.containerNumber;
      this.showDropdownOut = false;
    }
  }

  getDoTypes(doOrder: DeliveryOrder): string {
    if (!doOrder.lines || doOrder.lines.length === 0) return 'No lines';
    return doOrder.lines.map(l => this.typeCodeMap.get(l.containerTypeId) ?? l.containerTypeName ?? l.containerTypeId.substring(0, 4)).join(', ');
  }

  gateIn(): void {
    if (!this.inForm.containerNumber) {
      this.errorIn.set('Please enter or select a container number.');
      return;
    }
    this.busy.set(true);
    this.errorIn.set(null);
    this.lastIn.set(null);
    this.gate.gateIn(this.inForm).subscribe({
      next: (m) => { this.lastIn.set(m.id); this.busy.set(false); },
      error: (e: any) => { this.errorIn.set(e?.error?.detail ?? 'Gate-In failed.'); this.busy.set(false); },
    });
  }

  moveContainer(): void {
    if (!this.moveForm.containerNumber) {
      this.moveError.set('Please enter or select a container number.');
      return;
    }
    this.busy.set(true);
    this.moveError.set(null);
    this.moveSuccess.set(null);
    this.gate.move({
      containerNumber: this.moveForm.containerNumber,
      newBlockId: this.moveForm.newBlockId,
      newBay: Number(this.moveForm.newBay),
      newRow: Number(this.moveForm.newRow),
      newTier: Number(this.moveForm.newTier),
    }).subscribe({
      next: () => {
        this.moveSuccess.set(`Container ${this.moveForm.containerNumber} moved to Bay ${this.moveForm.newBay}, Row ${this.moveForm.newRow}, Tier ${this.moveForm.newTier}!`);
        this.busy.set(false);
      },
      error: (e: any) => {
        this.moveError.set(e?.error?.detail ?? 'Move container failed.');
        this.busy.set(false);
      }
    });
  }

  gateOut(): void {
    if (!this.outForm.containerNumber) {
      this.errorOut.set('Please enter or select a container number.');
      return;
    }
    this.busy.set(true);
    this.errorOut.set(null);
    this.lastOut.set(null);
    this.gate.gateOut(this.outForm).subscribe({
      next: (m) => { this.lastOut.set(m.id); this.busy.set(false); },
      error: (e: any) => { this.errorOut.set(e?.error?.detail ?? 'Gate-Out failed.'); this.busy.set(false); },
    });
  }
}
