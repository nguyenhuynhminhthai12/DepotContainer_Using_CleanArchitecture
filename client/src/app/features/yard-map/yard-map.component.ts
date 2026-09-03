import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { YardService } from '../../core/services/yard.service';
import { YardMapDto, BlockWithSlots, YardSlot } from '../../core/models/api.models';

@Component({
  selector: 'app-yard-map',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="page-header">
      <div>
        <h2>Interactive Yard Map & Slot Matrix</h2>
        <p class="muted">Real-time Bay × Row × Tier container grid visualizer with live slot occupancy and telemetry.</p>
      </div>
      <div class="header-actions">
        <button (click)="showCreateBlock.set(!showCreateBlock())" [class.secondary]="showCreateBlock()">
          {{ showCreateBlock() ? '✕ Cancel' : '+ Physical Block' }}
        </button>
        <button (click)="showCreateVirtual.set(!showCreateVirtual())" class="secondary">
          {{ showCreateVirtual() ? '✕ Cancel' : '+ Virtual Block' }}
        </button>
      </div>
    </div>

    <!-- KPI Summary Cards -->
    <div class="kpi-grid" *ngIf="data() as map">
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #eff6ff; color: #2563eb;">🏗️</div>
        <div class="kpi-info">
          <span class="kpi-label">Active Depot</span>
          <span class="kpi-value" style="font-size: 16px;">{{ map.depotName }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #ecfdf5; color: #059669;">📥</div>
        <div class="kpi-info">
          <span class="kpi-label">Occupied Slots</span>
          <span class="kpi-value">{{ totalOccupiedSlots() }} / {{ totalPhysicalSlots() }}</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fdf4ff; color: #9333ea;">📊</div>
        <div class="kpi-info">
          <span class="kpi-label">Yard Occupancy</span>
          <span class="kpi-value">{{ occupancyRate() }}%</span>
        </div>
      </div>
      <div class="kpi-card">
        <div class="kpi-icon" style="background: #fffbeb; color: #d97706;">🧱</div>
        <div class="kpi-info">
          <span class="kpi-label">Total Blocks</span>
          <span class="kpi-value">{{ map.blocks.length }}</span>
        </div>
      </div>
    </div>

    <!-- Create Physical Block Form -->
    <div *ngIf="showCreateBlock()" class="card create-card">
      <div class="card-title-bar">
        <h3>✨ Create Physical Yard Block</h3>
        <span class="badge">Bay × Row × Tier Matrix</span>
      </div>

      <form (ngSubmit)="createBlock()" class="create-form">
        <label>Block Code <span style="color: #ef4444;">*</span>
          <input [(ngModel)]="newBlock.code" name="code" placeholder="e.g. BLK-A" required />
        </label>
        <label>Block Name <span style="color: #ef4444;">*</span>
          <input [(ngModel)]="newBlock.name" name="name" placeholder="e.g. North Stacking Area" required />
        </label>
        <label>Max Bay (Length) <span style="color: #ef4444;">*</span>
          <input type="number" min="1" [(ngModel)]="newBlock.maxBay" name="maxBay" required />
        </label>
        <label>Max Row (Width) <span style="color: #ef4444;">*</span>
          <input type="number" min="1" [(ngModel)]="newBlock.maxRow" name="maxRow" required />
        </label>
        <label>Max Tier (Height) <span style="color: #ef4444;">*</span>
          <input type="number" min="1" [(ngModel)]="newBlock.maxTier" name="maxTier" required />
        </label>

        <div class="form-actions">
          <button type="submit" [disabled]="submitting()" class="btn-success">
            {{ submitting() ? 'Creating…' : '✓ Create Block' }}
          </button>
          <span *ngIf="blockMsg()" class="success-msg">✓ {{ blockMsg() }}</span>
          <span *ngIf="blockErr()" class="error-msg">⚠️ {{ blockErr() }}</span>
        </div>
      </form>
    </div>

    <!-- Create Virtual Block Form -->
    <div *ngIf="showCreateVirtual()" class="card create-card" style="border-left-color: #64748b;">
      <div class="card-title-bar">
        <h3>✨ Create Virtual Yard Block</h3>
        <span class="badge badge-muted">No Physical Slot Matrix</span>
      </div>

      <form (ngSubmit)="createVirtualBlock()" class="create-form">
        <label>Block Code <span style="color: #ef4444;">*</span>
          <input [(ngModel)]="newVirtual.code" name="vcode" placeholder="e.g. TRANSIT" required />
        </label>
        <label>Block Name <span style="color: #ef4444;">*</span>
          <input [(ngModel)]="newVirtual.name" name="vname" placeholder="e.g. Transit & Repair Zone" required />
        </label>

        <div class="form-actions">
          <button type="submit" [disabled]="submitting()" class="btn-success">
            {{ submitting() ? 'Creating…' : '✓ Create Virtual Block' }}
          </button>
          <span *ngIf="blockMsg()" class="success-msg">✓ {{ blockMsg() }}</span>
          <span *ngIf="blockErr()" class="error-msg">⚠️ {{ blockErr() }}</span>
        </div>
      </form>
    </div>

    <!-- Depot Selector & Legend Bar -->
    <div class="controls-bar card">
      <div class="depot-select-wrap" *ngIf="depots().length > 0">
        <label style="flex-direction: row; align-items: center; gap: 8px;">
          <span>Depot:</span>
          <select [ngModel]="selectedDepotId()" (ngModelChange)="onDepotChange($event)" class="depot-select">
            <option *ngFor="let d of depots()" [value]="d.id">{{ d.name }} ({{ d.code }})</option>
          </select>
        </label>
        <button (click)="refresh()" class="secondary btn-sm">🔄 Refresh Map</button>
      </div>

      <div class="legend-wrap">
        <span class="legend-item"><span class="legend-box occupied-20"></span> Occupied</span>
        <span class="legend-item"><span class="legend-box free"></span> Free Slot</span>
        <span class="legend-item"><span class="legend-box virtual"></span> Virtual Area</span>
      </div>
    </div>

    <div *ngIf="loading()" class="muted" style="padding: 20px; text-align: center;">Loading yard map telemetry…</div>
    <div *ngIf="error()" class="error-msg" style="padding: 12px; margin-bottom: 16px;">⚠️ {{ error() }}</div>

    <!-- Block Matrix Visualizer -->
    <div *ngIf="data() as map" class="blocks-grid">
      <section *ngFor="let block of map.blocks" class="card block-card">
        <header class="block-header">
          <div>
            <div class="block-title">
              <strong>Block {{ block.code }}</strong>
              <span class="muted">— {{ block.name }}</span>
              <span class="badge badge-muted" *ngIf="block.isVirtual">Virtual</span>
            </div>
            <div class="block-meta" *ngIf="!block.isVirtual">
              <span>{{ block.maxBay }} Bays × {{ block.maxRow }} Rows × {{ block.maxTier }} Tiers</span>
              <span>•</span>
              <b>{{ getBlockOccupancy(block) }}% Full</b> ({{ getBlockOccupiedCount(block) }}/{{ block.slots.length }} slots)
            </div>
          </div>
          <div class="block-actions-wrap">
            <button (click)="renameBlockPrompt(block)" class="secondary btn-sm" title="Rename Block">
              ✏️ Rename
            </button>
            <button *ngIf="!block.isVirtual" (click)="resizeBlockPrompt(block)" class="secondary btn-sm" title="Resize Grid">
              📐 Resize
            </button>
            <button (click)="deleteBlock(block)" class="btn-delete-block btn-sm" title="Delete Block">
              🗑️ Delete
            </button>
          </div>
        </header>

        <div *ngIf="!block.isVirtual" class="block-occupancy-bar">
          <div class="progress-bar-wrap">
            <div class="progress-bar-fill" [style.width.%]="getBlockOccupancy(block)"></div>
          </div>
        </div>

        <!-- 2D Slot Matrix -->
        <div *ngIf="!block.isVirtual" class="grid-scroll">
          <div class="slot-matrix" [style.grid-template-columns]="'repeat(' + block.maxRow + ', 42px)'">
            <div
              *ngFor="let slot of block.slots"
              class="slot-cell"
              [class.occupied]="slot.isOccupied"
              [class.selected]="selectedSlot?.id === slot.id"
              (click)="selectSlot(slot, block)"
              [title]="'Bay ' + slot.bay + ' / Row ' + slot.row + ' / Tier ' + slot.tier + (slot.isOccupied ? ' — OCCUPIED' : ' — Free')"
            >
              <div class="slot-loc">B{{ slot.bay }}R{{ slot.row }}</div>
              <div class="slot-tier">T{{ slot.tier }}</div>
            </div>
          </div>
        </div>

        <p *ngIf="block.isVirtual" class="virtual-note">
          📍 Virtual Block — Used for staging, gate buffer, and repair lanes without fixed Bay/Row/Tier grid.
        </p>
      </section>
    </div>

    <!-- Slot Inspector Modal / Drawer -->
    <div class="slot-inspector card" *ngIf="selectedSlot as s">
      <div class="inspector-header">
        <div>
          <h4>🔍 Slot Inspector: Bay {{ s.bay }}, Row {{ s.row }}, Tier {{ s.tier }}</h4>
          <span class="muted small">Block: <b>{{ selectedBlock?.code }}</b></span>
        </div>
        <button class="clear-btn" (click)="selectedSlot = null">✕</button>
      </div>
      <div class="inspector-body">
        <div class="inspector-item">
          <span class="muted">Status:</span>
          <span class="badge" [class.badge-success]="s.isOccupied" [class.badge-muted]="!s.isOccupied">
            {{ s.isOccupied ? '● Occupied' : '○ Free' }}
          </span>
        </div>
        <div class="inspector-item" *ngIf="s.currentContainerId">
          <span class="muted">Container ID:</span>
          <code class="mono">{{ s.currentContainerId }}</code>
        </div>
        <div class="inspector-actions">
          <button class="btn-sm" (click)="goToGate()">🚪 Open in Gate Operations</button>
          <button class="secondary btn-sm" (click)="selectedSlot = null">Close</button>
        </div>
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
    .card-title-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }
    .card-title-bar h3 { margin: 0; }
    .create-form {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 12px;
    }
    .form-actions {
      grid-column: 1 / -1;
      display: flex;
      gap: 12px;
      align-items: center;
      margin-top: 8px;
    }
    .success-msg { color: #16a34a; font-weight: 600; }
    .error-msg { color: #dc2626; font-weight: 600; }

    .controls-bar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 18px;
      margin-bottom: 20px;
      flex-wrap: wrap;
      gap: 12px;
    }
    .depot-select-wrap {
      display: flex;
      align-items: center;
      gap: 10px;
    }
    .depot-select {
      width: auto;
      min-width: 200px;
      font-weight: 600;
    }
    .btn-sm {
      padding: 5px 10px;
      font-size: 12px;
    }
    .block-actions-wrap {
      display: flex;
      gap: 6px;
      align-items: center;
    }
    .btn-delete-block {
      background: none;
      color: #dc2626;
      border: 1px solid #fecaca;
      border-radius: 4px;
      cursor: pointer;
    }
    .btn-delete-block:hover {
      background: #fef2f2;
      border-color: #f87171;
    }

    .legend-wrap {
      display: flex;
      gap: 16px;
      align-items: center;
      font-size: 12px;
      color: #64748b;
    }
    .legend-item { display: flex; align-items: center; gap: 6px; }
    .legend-box {
      width: 14px;
      height: 14px;
      border-radius: 3px;
      border: 1px solid #cbd5e1;
    }
    .occupied-20 { background: #16a34a; border-color: #15803d; }
    .free { background: #ffffff; }
    .virtual { background: #f1f5f9; }

    .blocks-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(380px, 1fr));
      gap: 20px;
    }

    .block-card {
      display: flex;
      flex-direction: column;
      padding: 18px;
    }
    .block-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 8px;
    }
    .block-title { font-size: 15px; }
    .block-meta {
      font-size: 11px;
      color: #64748b;
      margin-top: 2px;
      display: flex;
      gap: 6px;
    }
    .block-occupancy-bar { margin-bottom: 12px; }

    .grid-scroll {
      overflow-x: auto;
      padding: 4px;
    }
    .slot-matrix {
      display: grid;
      gap: 5px;
      justify-content: start;
    }
    .slot-cell {
      width: 42px;
      height: 42px;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      border: 1px solid #cbd5e1;
      border-radius: 4px;
      background: #ffffff;
      color: #64748b;
      cursor: pointer;
      user-select: none;
      transition: all 0.15s ease;
    }
    .slot-cell:hover {
      transform: scale(1.08);
      border-color: #2563eb;
      box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
      z-index: 2;
    }
    .slot-cell.occupied {
      background: #16a34a;
      color: #ffffff;
      border-color: #15803d;
      font-weight: bold;
    }
    .slot-cell.selected {
      outline: 2px solid #2563eb;
      box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.25);
    }
    .slot-loc { font-size: 9px; line-height: 1; }
    .slot-tier { font-size: 8px; opacity: 0.8; line-height: 1; margin-top: 2px; }

    .virtual-note {
      font-size: 12px;
      color: #64748b;
      background: #f8fafc;
      padding: 12px;
      border-radius: 6px;
      border: 1px dashed #cbd5e1;
      margin: 8px 0 0 0;
    }

    .slot-inspector {
      position: fixed;
      bottom: 24px;
      right: 24px;
      width: 320px;
      background: #ffffff;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
      border: 1px solid #cbd5e1;
      border-radius: 12px;
      z-index: 200;
      animation: slideUp 0.2s ease;
      padding: 16px;
    }
    .inspector-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 12px;
      border-bottom: 1px solid #f1f5f9;
      padding-bottom: 8px;
    }
    .inspector-header h4 { margin: 0; font-size: 14px; }
    .clear-btn { background: none; border: none; color: #64748b; cursor: pointer; }
    .inspector-body { display: flex; flex-direction: column; gap: 8px; }
    .inspector-item { display: flex; justify-content: space-between; font-size: 12px; }
    .inspector-actions { display: flex; gap: 8px; margin-top: 8px; }
    .mono { font-family: var(--font-mono); font-size: 11px; }
  `],
})
export class YardMapComponent implements OnInit {
  private readonly yard = inject(YardService);
  private readonly router = inject(Router);

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

  selectedSlot: YardSlot | null = null;
  selectedBlock: BlockWithSlots | null = null;

  totalPhysicalSlots = computed(() => {
    const blocks = this.data()?.blocks ?? [];
    return blocks.filter(b => !b.isVirtual).reduce((sum, b) => sum + b.slots.length, 0);
  });

  totalOccupiedSlots = computed(() => {
    const blocks = this.data()?.blocks ?? [];
    return blocks.filter(b => !b.isVirtual).reduce((sum, b) => sum + b.slots.filter(s => s.isOccupied).length, 0);
  });

  occupancyRate = computed(() => {
    const total = this.totalPhysicalSlots();
    if (!total) return 0;
    return Math.round((this.totalOccupiedSlots() / total) * 100);
  });

  newBlock = {
    code: `BLK-${Date.now() % 900 + 100}`,
    name: 'Block Area',
    maxBay: 4,
    maxRow: 3,
    maxTier: 2,
  };

  newVirtual = {
    code: `TRANSIT-${Date.now() % 90 + 10}`,
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

  getBlockOccupiedCount(b: BlockWithSlots): number {
    return b.slots.filter(s => s.isOccupied).length;
  }

  getBlockOccupancy(b: BlockWithSlots): number {
    if (!b.slots || b.slots.length === 0) return 0;
    return Math.round((this.getBlockOccupiedCount(b) / b.slots.length) * 100);
  }

  selectSlot(s: YardSlot, b: BlockWithSlots): void {
    this.selectedSlot = s;
    this.selectedBlock = b;
  }

  goToGate(): void {
    this.router.navigate(['/gate']);
  }

  createBlock(): void {
    if (!this.selectedDepotId()) {
      this.blockErr.set('Please select a depot before creating a block.');
      return;
    }
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
        const slotsCount = (b.maxBay ?? 0) * (b.maxRow ?? 0) * (b.maxTier ?? 0);
        this.blockMsg.set(`Block ${b.code} created successfully with ${slotsCount} slots!`);
        this.newBlock.code = `BLK-${Date.now() % 900 + 100}`;
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
        this.newVirtual.code = `TRANSIT-${Date.now() % 90 + 10}`;
        this.loadMap(this.selectedDepotId());
      },
      error: (err) => {
        this.submitting.set(false);
        this.blockErr.set(err?.error?.detail ?? 'Failed to create virtual block.');
      }
    });
  }

  renameBlockPrompt(block: BlockWithSlots): void {
    const newCode = prompt(`Enter new Block Code (currently ${block.code}):`, block.code);
    if (!newCode) return;
    const newName = prompt(`Enter new Block Name (currently ${block.name}):`, block.name);
    if (!newName) return;

    this.yard.updateBlock(block.id, { code: newCode, name: newName }).subscribe({
      next: () => {
        this.loadMap(this.selectedDepotId());
      },
      error: (err) => alert(err?.error?.detail ?? 'Failed to update block.')
    });
  }

  deleteBlock(block: BlockWithSlots): void {
    if (!confirm(`Are you sure you want to permanently delete Block ${block.code}?`)) return;

    this.yard.deleteBlock(block.id).subscribe({
      next: () => {
        this.loadMap(this.selectedDepotId());
      },
      error: (err) => {
        alert(err?.error?.detail ?? err?.error?.title ?? 'Failed to delete block. It may contain occupied container slots.');
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
