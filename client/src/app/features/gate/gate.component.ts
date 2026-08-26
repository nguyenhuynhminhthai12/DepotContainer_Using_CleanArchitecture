import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GateService } from '../../core/services/gate.service';
import { LineOperator } from '../../core/models/api.models';
import { DeliveryOrderService } from '../../core/services/delivery-order.service';

/**
 * Gate In / Gate Out form. Operators enter the container number, vehicle plate,
 * driver name and select the Line Operator. Server enforces:
 *   • ISO 6346 Modulo-11 check digit
 *   • Delivery Order validity for Gate Out (not expired + quantity remaining)
 */
@Component({
  selector: 'app-gate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <h2>Gate Operations</h2>
    <p class="muted">Gate-In starts an EIR; Gate-Out requires an active Delivery Order.</p>

    <div class="grid-3">
      <!-- Gate In -->
      <section class="card">
        <h3>Gate In</h3>
        <form (ngSubmit)="gateIn()">
          <label>Container Number
            <input [(ngModel)]="inForm.containerNumber" name="inNumber" placeholder="e.g. MSCU1234566" required />
          </label>
          <label>Line Operator
            <select [(ngModel)]="inForm.lineOperatorId" name="inOp" required>
              <option value="">Select…</option>
              <option *ngFor="let l of operators()" [value]="l.id">{{ l.code }} — {{ l.name }}</option>
            </select>
          </label>
          <label>Block ID
            <input [(ngModel)]="inForm.blockId" name="inBlock" placeholder="Target Block ID" required />
          </label>
          <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px;">
            <label>Bay
              <input type="number" [(ngModel)]="inForm.bay" name="inBay" placeholder="1" />
            </label>
            <label>Row
              <input type="number" [(ngModel)]="inForm.row" name="inRow" placeholder="1" />
            </label>
            <label>Tier
              <input type="number" [(ngModel)]="inForm.tier" name="inTier" placeholder="1" />
            </label>
          </div>
          <label>Vehicle Plate
            <input [(ngModel)]="inForm.vehicleInNumber" name="inVehicle" placeholder="KH-9999" required />
          </label>
          <label>Driver Name
            <input [(ngModel)]="inForm.driverInName" name="inDriver" placeholder="Nguyen Van A" />
          </label>
          <label>Classification
            <select [(ngModel)]="inForm.classification" name="inClass">
              <option value="Export">Export</option>
              <option value="Import">Import</option>
              <option value="Domestic">Domestic</option>
              <option value="A">A</option>
              <option value="B">B</option>
              <option value="C">C</option>
            </select>
          </label>
          <button type="submit" [disabled]="busy()" style="margin-top: 8px; padding: 8px 16px; background: #2563eb; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
            {{ busy() ? 'Submitting…' : 'Register Gate-In' }}
          </button>
          <p class="success" *ngIf="lastIn()">✓ EIR opened: {{ lastIn() }}</p>
          <p class="error" *ngIf="errorIn()">{{ errorIn() }}</p>
        </form>
      </section>

      <!-- Move Container -->
      <section class="card">
        <h3>Move Container</h3>
        <form (ngSubmit)="moveContainer()">
          <label>Container Number
            <input [(ngModel)]="moveForm.containerNumber" name="moveContainerNumber" placeholder="e.g. MSCU1234566" required />
          </label>
          <label>Target Block ID
            <input [(ngModel)]="moveForm.newBlockId" name="moveBlockId" placeholder="New Block ID" required />
          </label>
          <div style="display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 6px;">
            <label>New Bay
              <input type="number" [(ngModel)]="moveForm.newBay" name="moveBay" placeholder="3" required />
            </label>
            <label>New Row
              <input type="number" [(ngModel)]="moveForm.newRow" name="moveRow" placeholder="1" required />
            </label>
            <label>New Tier
              <input type="number" [(ngModel)]="moveForm.newTier" name="moveTier" placeholder="1" required />
            </label>
          </div>
          <p class="muted small" style="margin-top: 4px;">* 20ft requires Odd Bay; 40ft requires Even Bay.</p>
          <button type="submit" [disabled]="busy()" style="margin-top: 8px; padding: 8px 16px; background: #4f46e5; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
            {{ busy() ? 'Moving…' : 'Move Container' }}
          </button>
          <p class="success" *ngIf="moveSuccess()">✓ {{ moveSuccess() }}</p>
          <p class="error" *ngIf="moveError()">{{ moveError() }}</p>
        </form>
      </section>

      <!-- Gate Out -->
      <section class="card">
        <h3>Gate Out</h3>
        <form (ngSubmit)="gateOut()">
          <label>Container Number
            <input [(ngModel)]="outForm.containerNumber" name="outContainerNumber" placeholder="e.g. MSCU1234566" required />
          </label>
          <label>Delivery Order ID
            <input [(ngModel)]="outForm.deliveryOrderId" name="outOrder" placeholder="Active DO Guid" required />
          </label>
          <label>Vehicle Plate
            <input [(ngModel)]="outForm.vehicleOutNumber" name="outVehicle" placeholder="KH-8888" required />
          </label>
          <label>Driver Name
            <input [(ngModel)]="outForm.driverOutName" name="outDriver" placeholder="Tran Van B" />
          </label>
          <label>Condition at Gate Out
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
          <button type="submit" [disabled]="busy()" style="margin-top: 8px; padding: 8px 16px; background: #059669; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
            {{ busy() ? 'Submitting…' : 'Register Gate-Out' }}
          </button>
          <p class="success" *ngIf="lastOut()">✓ EIR closed: {{ lastOut() }}</p>
          <p class="error" *ngIf="errorOut()">{{ errorOut() }}</p>
        </form>
      </section>
    </div>
  `,
  styles: [`
    .grid-3 { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
    form { display: flex; flex-direction: column; gap: 8px; }
    label { display: flex; flex-direction: column; gap: 4px; font-size: 12px; font-weight: 500; }
    label input, label select { padding: 8px 10px; border-radius: 4px; border: 1px solid var(--color-border); font-size: 13px; }
    .small { font-size: 11px; }
  `],
})
export class GateComponent {
  private gate = inject(GateService);
  private orderSvc = inject(DeliveryOrderService);

  busy = signal(false);
  operators = signal<LineOperator[]>([]);
  lastIn = signal<string | null>(null);
  lastOut = signal<string | null>(null);
  errorIn = signal<string | null>(null);
  errorOut = signal<string | null>(null);
  moveSuccess = signal<string | null>(null);
  moveError = signal<string | null>(null);

  inForm = {
    containerNumber: '', lineOperatorId: '', blockId: '',
    bay: 1, row: 1, tier: 1,
    vehicleInNumber: '', driverInName: '',
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
    containerNumber: '', deliveryOrderId: '',
    vehicleOutNumber: '', driverOutName: '',
    conditionAtGateOut: 'Normal',
  };

  constructor() {
    this.orderSvc.lineOperators().subscribe((l) => {
      this.operators.set(l);
      if (l.length > 0 && !this.inForm.lineOperatorId) this.inForm.lineOperatorId = l[0].id;
    });
  }

  gateIn(): void {
    this.busy.set(true);
    this.errorIn.set(null);
    this.lastIn.set(null);
    this.gate.gateIn(this.inForm).subscribe({
      next: (m) => { this.lastIn.set(m.id); this.busy.set(false); },
      error: (e) => { this.errorIn.set(e?.error?.detail ?? 'Gate-In failed.'); this.busy.set(false); },
    });
  }

  moveContainer(): void {
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
      error: (e) => {
        this.moveError.set(e?.error?.detail ?? 'Move container failed.');
        this.busy.set(false);
      }
    });
  }

  gateOut(): void {
    this.busy.set(true);
    this.errorOut.set(null);
    this.lastOut.set(null);
    this.gate.gateOut(this.outForm).subscribe({
      next: (m) => { this.lastOut.set(m.id); this.busy.set(false); },
      error: (e) => { this.errorOut.set(e?.error?.detail ?? 'Gate-Out failed.'); this.busy.set(false); },
    });
  }
}
