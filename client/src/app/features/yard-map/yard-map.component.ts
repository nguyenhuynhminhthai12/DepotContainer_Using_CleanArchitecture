import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { YardService } from '../../core/services/yard.service';
import { YardMapDto, BlockWithSlots, YardSlot } from '../../core/models/api.models';

/**
 * Visualises the live yard map: each Block rendered as a grid of Bay × Row × Tier cells.
 * Cells are colour-coded:
 *   • green = occupied (container present)
 *   • white = free
 *   • grey  = virtual block (no slot grid)
 */
@Component({
  selector: 'app-yard-map',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px;">
      <div>
        <h2>Yard Map</h2>
        <p class="muted" style="margin: 0;">Block → Bay → Row → Tier grid with live occupancy from the depot REST API.</p>
      </div>
      <div style="display: flex; gap: 8px;">
        <button (click)="showCreateBlock.set(!showCreateBlock())" style="padding: 8px 14px; background: #2563eb; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
          {{ showCreateBlock() ? '✕ Cancel' : '+ Create Block' }}
        </button>
        <button (click)="showCreateVirtual.set(!showCreateVirtual())" style="padding: 8px 14px; background: #4b5563; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
          {{ showCreateVirtual() ? '✕ Cancel' : '+ Virtual Block' }}
        </button>
      </div>
    </div>

    <!-- Create Physical Block Card -->
    <div *ngIf="showCreateBlock()" class="card" style="margin-bottom: 20px; border-left: 4px solid #2563eb;">
      <h3>Create New Yard Block (Physical Grid)</h3>
      <form (ngSubmit)="createBlock()" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; margin-top: 12px;">
        <label>Block Code
          <input [(ngModel)]="newBlock.code" name="code" placeholder="e.g. B01" required />
        </label>
        <label>Block Name
          <input [(ngModel)]="newBlock.name" name="name" placeholder="e.g. Block A - North" required />
        </label>
        <label>Max Bay (Length)
          <input type="number" min="1" [(ngModel)]="newBlock.maxBay" name="maxBay" required />
        </label>
        <label>Max Row (Width)
          <input type="number" min="1" [(ngModel)]="newBlock.maxRow" name="maxRow" required />
        </label>
        <label>Max Tier (Height)
          <input type="number" min="1" [(ngModel)]="newBlock.maxTier" name="maxTier" required />
        </label>

        <div style="grid-column: 1 / -1; display: flex; gap: 12px; align-items: center; margin-top: 8px;">
          <button type="submit" [disabled]="submitting()" style="padding: 8px 20px; background: #16a34a; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
            {{ submitting() ? 'Creating…' : 'Create Block' }}
          </button>
          <span *ngIf="blockMsg()" style="color: #16a34a; font-weight: 500;">✓ {{ blockMsg() }}</span>
          <span *ngIf="blockErr()" class="error">{{ blockErr() }}</span>
        </div>
      </form>
    </div>

    <!-- Create Virtual Block Card -->
    <div *ngIf="showCreateVirtual()" class="card" style="margin-bottom: 20px; border-left: 4px solid #4b5563;">
      <h3>Create Virtual Block</h3>
      <form (ngSubmit)="createVirtualBlock()" style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 12px; margin-top: 12px;">
        <label>Block Code
          <input [(ngModel)]="newVirtual.code" name="vcode" placeholder="e.g. TRANSIT" required />
        </label>
        <label>Block Name
          <input [(ngModel)]="newVirtual.name" name="vname" placeholder="e.g. Repair / Transit Area" required />
        </label>

        <div style="grid-column: 1 / -1; display: flex; gap: 12px; align-items: center; margin-top: 8px;">
          <button type="submit" [disabled]="submitting()" style="padding: 8px 20px; background: #16a34a; color: #fff; border: none; border-radius: 6px; cursor: pointer; font-weight: 500;">
            {{ submitting() ? 'Creating…' : 'Create Virtual Block' }}
          </button>
          <span *ngIf="blockMsg()" style="color: #16a34a; font-weight: 500;">✓ {{ blockMsg() }}</span>
          <span *ngIf="blockErr()" class="error">{{ blockErr() }}</span>
        </div>
      </form>
    </div>

    <!-- Depot Selector & Controls -->
    <div style="display: flex; gap: 12px; align-items: center; margin-bottom: 16px;" *ngIf="depots().length > 0">
      <label style="display: flex; gap: 8px; align-items: center; font-size: 13px;">
        <strong>Select Depot:</strong>
        <select [ngModel]="selectedDepotId()" (ngModelChange)="onDepotChange($event)" style="padding: 6px 12px; border-radius: 4px; border: 1px solid var(--color-border);">
          <option *ngFor="let d of depots()" [value]="d.id">{{ d.name }} ({{ d.code }})</option>
        </select>
      </label>
      <button (click)="refresh()" style="padding: 6px 12px; border-radius: 4px; border: 1px solid var(--color-border); cursor: pointer;">🔄 Refresh</button>
    </div>

    <div *ngIf="loading()" class="muted">Loading yard map…</div>
    <div *ngIf="error()" class="error">{{ error() }}</div>

    <div *ngIf="data() as map">
      <h3>{{ map.depotName }} <span class="muted">({{ map.depotId }})</span></h3>
      <section *ngFor="let block of map.blocks" class="card">
        <header style="display: flex; justify-content: space-between; align-items: center;">
          <div>
            <strong>Block {{ block.code }}</strong> — {{ block.name }}
            <span class="badge" *ngIf="block.isVirtual">Virtual Block</span>
            <span class="muted" *ngIf="!block.isVirtual">
              • {{ block.maxBay }} bays × {{ block.maxRow }} rows × {{ block.maxTier }} tiers ({{ block.slots.length }} slots)
            </span>
          </div>
          <button *ngIf="!block.isVirtual" (click)="resizeBlockPrompt(block)" style="padding: 4px 10px; border-radius: 4px; border: 1px solid var(--color-border); cursor: pointer; font-size: 12px;">
            📐 Resize Grid
          </button>
        </header>

        <ng-container *ngIf="!block.isVirtual">
          <div class="grid" [style.grid-template-columns]="'repeat(' + block.maxRow + ', 32px)'">
            <div *ngFor="let slot of block.slots"
                 class="cell"
                 [class.occupied]="slot.isOccupied"
                 [title]="'Bay ' + slot.bay + ' / Row ' + slot.row + ' / Tier ' + slot.tier +
                          (slot.isOccupied ? ' — OCCUPIED (' + slot.currentContainerId + ')' : ' — Free')">
              B{{ slot.bay }}R{{ slot.row }}T{{ slot.tier }}
            </div>
          </div>
        </ng-container>
        <p *ngIf="block.isVirtual" class="muted" style="margin-top: 8px;">Virtual Block — no physical slot grid.</p>
      </section>
    </div>
  `,
  styles: [`
    section.card { margin-bottom: 16px; padding: 16px; }
    header { margin-bottom: 8px; }
    .grid { display: grid; gap: 3px; margin-top: 12px; }
    .cell {
      width: 32px; height: 32px;
      display: grid; place-items: center;
      font-size: 8px;
      border: 1px solid var(--color-border);
      background: #fff;
      color: var(--color-muted);
      border-radius: 2px;
      user-select: none;
    }
    .cell.occupied { background: #16a34a; color: #fff; border-color: #15803d; font-weight: bold; }
    label { display: flex; flex-direction: column; gap: 4px; font-size: 12px; font-weight: 500; }
    label input, label select { padding: 8px 10px; border-radius: 4px; border: 1px solid var(--color-border); font-size: 13px; }
    .badge { padding: 2px 8px; border-radius: 12px; font-size: 11px; background: #f3f4f6; color: #4b5563; margin-left: 8px; }
  `],
})
export class YardMapComponent implements OnInit {
  private yard = inject(YardService);
  loading = signal(true);
  submitting = signal(false);
  showCreateBlock = signal(false);
  showCreateVirtual = signal(false);
  error = signal<string | null>(null);
  blockMsg = signal<string | null>(null);
  blockErr = signal<string | null>(null);
  data = signal<YardMapDto | null>(null);
  depots = signal<{ id: string; code: string; name: string }[]>([]);
  selectedDepotId = signal<string>('');

  newBlock = {
    code: `BLK-${Math.floor(100 + Math.random() * 900)}`,
    name: 'Block Area',
    maxBay: 4,
    maxRow: 3,
    maxTier: 2,
  };

  newVirtual = {
    code: `TRANSIT-${Math.floor(10 + Math.random() * 90)}`,
    name: 'Transit / Repair Zone',
  };

  ngOnInit(): void { this.loadDepots(); }

  loadDepots(): void {
    this.loading.set(true);
    this.error.set(null);
    this.yard.listDepots().subscribe({
      next: (list) => {
        this.depots.set(list);
        if (list && list.length > 0) {
          this.selectedDepotId.set(list[0].id);
          this.loadMap(list[0].id);
        } else {
          this.loading.set(false);
          this.error.set('No depots found.');
        }
      },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.detail ?? 'Failed to load depots.');
      }
    });
  }

  loadMap(depotId: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.yard.getYardMap(depotId).subscribe({
      next: (d) => { this.data.set(d); this.loading.set(false); },
      error: (e) => {
        this.loading.set(false);
        this.error.set(e?.error?.detail ?? 'Failed to load yard map.');
      },
    });
  }

  createBlock(): void {
    if (!this.selectedDepotId()) return;
    this.submitting.set(true);
    this.blockErr.set(null);
    this.blockMsg.set(null);

    this.yard.createBlock({
      depotId: this.selectedDepotId(),
      code: this.newBlock.code,
      name: this.newBlock.name,
      maxBay: Number(this.newBlock.maxBay),
      maxRow: Number(this.newBlock.maxRow),
      maxTier: Number(this.newBlock.maxTier),
    }).subscribe({
      next: (b) => {
        this.submitting.set(false);
        this.blockMsg.set(`Block ${b.code} created with ${b.slots.length} slots!`);
        this.newBlock.code = `BLK-${Math.floor(100 + Math.random() * 900)}`;
        this.loadMap(this.selectedDepotId());
      },
      error: (err) => {
        this.submitting.set(false);
        this.blockErr.set(err?.error?.detail ?? 'Failed to create block.');
      }
    });
  }

  createVirtualBlock(): void {
    if (!this.selectedDepotId()) return;
    this.submitting.set(true);
    this.blockErr.set(null);
    this.blockMsg.set(null);

    this.yard.createVirtualBlock({
      depotId: this.selectedDepotId(),
      code: this.newVirtual.code,
      name: this.newVirtual.name,
    }).subscribe({
      next: (b) => {
        this.submitting.set(false);
        this.blockMsg.set(`Virtual Block ${b.code} created successfully!`);
        this.newVirtual.code = `TRANSIT-${Math.floor(10 + Math.random() * 90)}`;
        this.loadMap(this.selectedDepotId());
      },
      error: (err) => {
        this.submitting.set(false);
        this.blockErr.set(err?.error?.detail ?? 'Failed to create virtual block.');
      }
    });
  }

  resizeBlockPrompt(block: BlockWithSlots): void {
    const bayStr = prompt(`Enter new Max Bay (currently ${block.maxBay}):`, block.maxBay?.toString());
    if (!bayStr) return;
    const rowStr = prompt(`Enter new Max Row (currently ${block.maxRow}):`, block.maxRow?.toString());
    if (!rowStr) return;
    const tierStr = prompt(`Enter new Max Tier (currently ${block.maxTier}):`, block.maxTier?.toString());
    if (!tierStr) return;

    this.yard.resizeBlock(block.id, Number(bayStr), Number(rowStr), Number(tierStr)).subscribe({
      next: () => {
        alert(`Block ${block.code} resized successfully!`);
        this.loadMap(this.selectedDepotId());
      },
      error: (err) => alert(err?.error?.detail ?? 'Failed to resize block.')
    });
  }

  onDepotChange(depotId: string): void {
    this.selectedDepotId.set(depotId);
    this.loadMap(depotId);
  }

  refresh(): void {
    if (this.selectedDepotId()) {
      this.loadMap(this.selectedDepotId());
    } else {
      this.loadDepots();
    }
  }
}
