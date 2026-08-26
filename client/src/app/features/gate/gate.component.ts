import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GateService } from '../../core/services/gate.service';
import { Block, DeliveryOrder, LineOperator } from '../../core/models/api.models';
import { DeliveryOrderService } from '../../core/services/delivery-order.service';
import { ContainerService } from '../../core/services/container.service';
import { YardService } from '../../core/services/yard.service';

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
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
      <div>
        <h2>Gate Operations</h2>
        <p class="muted" style="margin: 0;">Gate-In starts an EIR; Gate-Out requires an active Delivery Order.</p>
      </div>
      <button (click)="loadAllData()" style="padding: 6px 14px; border-radius: 6px; border: 1px solid var(--color-border); cursor: pointer;">
        🔄 Refresh Data
      </button>
    </div>

    <div class="grid-3">
      <!-- Gate In -->
      <section class="card">
        <h3>Gate In</h3>
        <form (ngSubmit)="gateIn()">
          <label>Container Number (ISO 6346)
            <input [(ngModel)]="inForm.containerNumber" name="inNumber" placeholder="e.g. MAEU1000018" required />
          </label>
          <label>Line Operator (Shipping Line)
            <select [(ngModel)]="inForm.lineOperatorId" name="inOp" required>
              <option value="">Select Operator…</option>
              <option *ngFor="let l of operators()" [value]="l.id">{{ l.code }} — {{ l.name }}</option>
            </select>
          </label>
          <label>Target Yard Block
            <select [(ngModel)]="inForm.blockId" name="inBlock" required>
              <option value="">Select Block…</option>
              <option *ngFor="let b of blocks()" [value]="b.id">Block {{ b.code }} ({{ b.name }})</option>
            </select>
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
            <input [(ngModel)]="moveForm.containerNumber" name="moveContainerNumber" placeholder="e.g. MAEU1000018" required />
          </label>
          <label>Target Yard Block
            <select [(ngModel)]="moveForm.newBlockId" name="moveBlockId" required>
              <option value="">Select Block…</option>
              <option *ngFor="let b of blocks()" [value]="b.id">Block {{ b.code }} ({{ b.name }})</option>
            </select>
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
          <p class="muted small" style="margin-top: 4px;">* 20ft requires Odd Bay (1,3,5); 40ft requires Even Bay (2,4,6).</p>
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
            <input [(ngModel)]="outForm.containerNumber" name="outContainerNumber" placeholder="e.g. MAEU1000018" required />
          </label>
          <label>Delivery Order (Release Permit)
            <select [(ngModel)]="outForm.deliveryOrderId" name="outOrder" required>
              <option value="">Select Delivery Order…</option>
              <option *ngFor="let do of deliveryOrders()" [value]="do.id">
                {{ do.orderNumber }} [{{ getDoTypes(do) }}] — {{ do.lineOperatorName }}
              </option>
            </select>
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
export class GateComponent implements OnInit {
  private readonly gate = inject(GateService);
  private readonly orderSvc = inject(DeliveryOrderService);
  private readonly cntrSvc = inject(ContainerService);
  private readonly yardSvc = inject(YardService);

  busy = signal(false);
  operators = signal<LineOperator[]>([]);
  blocks = signal<Block[]>([]);
  deliveryOrders = signal<DeliveryOrder[]>([]);
  typeCodeMap = new Map<string, string>();

  lastIn = signal<string | null>(null);
  lastOut = signal<string | null>(null);
  errorIn = signal<string | null>(null);
  errorOut = signal<string | null>(null);
  moveSuccess = signal<string | null>(null);
  moveError = signal<string | null>(null);

  inForm = {
    containerNumber: 'MAEU1000018',
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
    containerNumber: 'MAEU1000018',
    newBlockId: '',
    newBay: 3,
    newRow: 1,
    newTier: 1,
  };

  outForm = {
    containerNumber: 'MAEU1000018',
    deliveryOrderId: '',
    vehicleOutNumber: 'KH-8888',
    driverOutName: 'Tran Van B',
    conditionAtGateOut: 'Normal',
  };

  ngOnInit(): void {
    this.cntrSvc.listTypes().subscribe({
      next: (types) => {
        types.forEach(t => this.typeCodeMap.set(t.id, t.code));
        this.loadAllData();
      }
    });
  }

  loadAllData(): void {
    // 1. Load Line Operators
    this.orderSvc.lineOperators().subscribe({
      next: (l) => {
        this.operators.set(l);
        if (l.length > 0 && !this.inForm.lineOperatorId) this.inForm.lineOperatorId = l[0].id;
      }
    });

    // 2. Load Blocks from Yard
    this.yardSvc.listDepots().subscribe({
      next: (depots) => {
        if (depots.length > 0) {
          this.yardSvc.getYardMap(depots[0].id).subscribe({
            next: (map) => {
              this.blocks.set(map.blocks);
              if (map.blocks.length > 0) {
                // Default to first non-virtual block if available
                const physBlock = map.blocks.find(b => !b.isVirtual) ?? map.blocks[0];
                this.inForm.blockId = physBlock.id;
                this.moveForm.newBlockId = physBlock.id;
              }
            }
          });
        }
      }
    });

    // 3. Load Active Delivery Orders
    this.orderSvc.list().subscribe({
      next: (dos) => {
        this.deliveryOrders.set(dos);
        if (dos.length > 0) {
          this.outForm.deliveryOrderId = dos[0].id;
        }
      }
    });
  }

  getDoTypes(doOrder: DeliveryOrder): string {
    if (!doOrder.lines || doOrder.lines.length === 0) return 'No lines';
    return doOrder.lines.map(l => this.typeCodeMap.get(l.containerTypeId) ?? l.containerTypeName ?? l.containerTypeId.substring(0, 4)).join(', ');
  }

  gateIn(): void {
    this.busy.set(true);
    this.errorIn.set(null);
    this.lastIn.set(null);
    this.gate.gateIn(this.inForm).subscribe({
      next: (m) => { this.lastIn.set(m.id); this.busy.set(false); },
      error: (e: any) => { this.errorIn.set(e?.error?.detail ?? 'Gate-In failed.'); this.busy.set(false); },
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
      error: (e: any) => {
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
      error: (e: any) => { this.errorOut.set(e?.error?.detail ?? 'Gate-Out failed.'); this.busy.set(false); },
    });
  }
}
