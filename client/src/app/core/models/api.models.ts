/**
 * Các interface mô hình dữ liệu dùng chung cho API (Shared API Models).
 * Định nghĩa tất cả các DTO (Data Transfer Object) trao đổi giữa frontend và backend:
 * - Container, ContainerType (dữ liệu container)
 * - Block, YardSlot, YardMapDto (cấu trúc Yard)
 * - ContainerMovement, GateInRequest, GateOutRequest (EIR - biên nhận thiết bị)
 * - DeliveryOrder, DeliveryOrderLine (đơn giao hàng)
 * - Customer, LineOperator (dữ liệu chủ)
 * - YardAgingReport, DailyThroughputReport (báo cáo)
 * - LoginRequest, AuthResponse (xác thực)
 * Bản quyền (c) 2026 TechSpherex.
 */
/** Shared API response wrapper (PagedResult<T> on the server). */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Container master data. */
export interface Container {
  id: string;
  containerNumber: string;
  containerTypeId: string;
  isoCode: string;
  sizeFeet: number;
  maxWeightKg: number;
  tareWeightKg: number;
  manufactureDate: string;
  owner: string;
  condition: 'Normal' | 'Damaged' | 'Dented' | 'Twisted' | 'Cracked' | 'Leaking' | 'Other';
  tenantId: string;
}

export interface ContainerType {
  id: string;
  code: string;
  name: string;
  family: string;
  description?: string;
}

/** Yard entities. */
export interface Block {
  id: string;
  depotId: string;
  code: string;
  name: string;
  isVirtual: boolean;
  maxBay?: number;
  maxRow?: number;
  maxTier?: number;
  displayOrder: number;
}

export interface YardSlot {
  id: string;
  blockId: string;
  bay: number;
  row: number;
  tier: number;
  isOccupied: boolean;
  currentContainerId?: string;
}

export interface YardMapDto {
  depotId: string;
  depotName: string;
  blocks: BlockWithSlots[];
}

export interface BlockWithSlots extends Block {
  slots: YardSlot[];
}

/** Gate operations (EIR). */
export interface ContainerMovement {
  id: string;
  containerId: string;
  lineOperatorId: string;
  yardSlotId?: string;
  blockId?: string;
  classification: string;
  conditionAtGateIn: string;
  conditionAtGateOut?: string;
  vehicleInNumber?: string;
  driverInName?: string;
  gateInAt: string;
  vehicleOutNumber?: string;
  driverOutName?: string;
  gateOutAt?: string;
  status: 'InYard' | 'GateOut';
  deliveryOrderId?: string;
}

export interface GateInRequest {
  containerNumber: string;
  blockId: string;
  yardSlotId?: string;
  lineOperatorId: string;
  classification: string;
  vehicleInNumber: string;
  driverInName?: string;
  conditionAtGateIn: string;
}

export interface GateOutRequest {
  containerNumber: string;
  deliveryOrderId: string;
  vehicleOutNumber?: string;
  driverOutName?: string;
  conditionAtGateOut: string;
}

/** Delivery Order. */
export interface DeliveryOrder {
  id: string;
  orderNumber: string;
  customerId: string;
  customerName?: string;
  lineOperatorId: string;
  lineOperatorName?: string;
  expiryDate: string;
  vesselVoyage?: string;
  notes?: string;
  isClosed: boolean;
  lines: DeliveryOrderLine[];
}

export interface DeliveryOrderLine {
  id: string;
  deliveryOrderId: string;
  containerTypeId: string;
  containerTypeName?: string;
  requestedQuantity: number;
  deliveredQuantity: number;
}

export interface CreateDeliveryOrderRequest {
  orderNumber: string;
  customerId: string;
  lineOperatorId: string;
  expiryDate: string;
  vesselVoyage?: string;
  notes?: string;
  lines: {
    containerTypeId: string;
    requestedQuantity: number;
    deliveredQuantity?: number;
  }[];
}

export interface Customer {
  id: string;
  taxCode: string;
  name: string;
}

export interface LineOperator {
  id: string;
  code: string;
  name: string;
  country?: string;
}

/** Reports. */
export interface YardAgingRow {
  lineOperatorId: string;
  lineOperatorCode: string;
  lineOperatorName: string;
  buckets: { withinTenDays: number; tenDaysOrMore: number };
}

export interface YardAgingReport {
  asOf: string;
  rows: YardAgingRow[];
}

export interface DailyThroughputRow {
  date: string;
  lineOperatorId: string;
  lineOperatorCode: string;
  lineOperatorName: string;
  gateIn: number;
  gateOut: number;
}

export interface DailyThroughputReport {
  from: string;
  to: string;
  rows: DailyThroughputRow[];
}

/** Auth. */
export interface LoginRequest { email: string; password: string; }
export interface AuthResponse { accessToken: string; refreshToken: string; expiresIn: number; }
