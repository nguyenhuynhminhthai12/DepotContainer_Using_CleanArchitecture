/**
 * Các interface mô hình dữ liệu dùng chung cho API (Shared API Models).
 * Định nghĩa tất cả các DTO (Data Transfer Object) trao đổi giữa Frontend và Backend.
 * Cung cấp đầy đủ chú thích JSDoc tiếng Việt để hỗ trợ tooltip khi rê chuột trong IDE.
 * Bản quyền (c) 2026 TechSpherex.
 */

/**
 * Kết quả phân trang dùng chung cho các API danh sách.
 * @template T Kiểu dữ liệu của từng phần tử trong danh sách.
 */
export interface PagedResult<T> {
  /** Danh sách các phần tử của trang hiện tại */
  items: T[];
  /** Tổng số lượng phần tử trên toàn hệ thống */
  totalCount: number;
  /** Chỉ số trang hiện tại (bắt đầu từ 1) */
  page: number;
  /** Số lượng phần tử hiển thị trên một trang */
  pageSize: number;
}

/**
 * Dữ liệu chủ (Master Data) của thùng hàng Container.
 */
export interface Container {
  /** Định danh duy nhất (GUID) của container */
  id: string;
  /** Số hiệu container theo chuẩn ISO (VD: MSCU1234567) */
  containerNumber: string;
  /** Mã loại container (Khóa ngoại trỏ đến ContainerType) */
  containerTypeId: string;
  /** Mã ISO của container (VD: 22G1, 45G1) */
  isoCode: string;
  /** Chiều dài container tính bằng Feet (20, 40 hoặc 45) */
  sizeFeet: number;
  /** Trọng lượng tối đa cho phép (Max Gross Weight) tính bằng Kg */
  maxWeightKg: number;
  /** Trọng lượng vỏ container (Tare Weight) tính bằng Kg */
  tareWeightKg: number;
  /** Ngày sản xuất của container (định dạng ISO chuỗi ngày) */
  manufactureDate: string;
  /** Chủ sở hữu container (Hãng tàu hoặc đơn vị cho thuê) */
  owner: string;
  /** Tình trạng vật lý của container (Bình thường, Hư hỏng, Móp, Thủng...) */
  condition: 'Normal' | 'Damaged' | 'Dented' | 'Twisted' | 'Cracked' | 'Leaking' | 'Other';
  /** Mã định danh Tenant/Chi nhánh sở hữu */
  tenantId: string;
}

/**
 * Danh mục loại container (VD: Dry Standard, High Cube, Reefer).
 */
export interface ContainerType {
  /** Định danh duy nhất (GUID) của loại container */
  id: string;
  /** Mã viết tắt loại container (VD: 20DC, 40HC, 40RF) */
  code: string;
  /** Tên đầy đủ của loại container */
  name: string;
  /** Nhóm/Họ container (VD: DRY, REEFER, TANK) */
  family: string;
  /** Mô tả chi tiết thêm về loại container */
  description?: string;
}

/**
 * Khối phân khu trong bãi container (Block).
 */
export interface Block {
  /** Định danh duy nhất (GUID) của Block */
  id: string;
  /** Mã Depot/Cảng mà Block này trực thuộc */
  depotId: string;
  /** Mã viết tắt của Block (VD: A, B, C, V1) */
  code: string;
  /** Tên hiển thị của Block */
  name: string;
  /** Đánh dấu Block ảo (dùng cho khu vực chờ, trạm kiểm tra) */
  isVirtual: boolean;
  /** Số lượng Bay tối đa trong Block */
  maxBay?: number;
  /** Số lượng Row (Dãy ngang) tối đa trong Block */
  maxRow?: number;
  /** Số lượng Tier (Tầng xếp chồng) tối đa trong Block */
  maxTier?: number;
  /** Thứ tự sắp xếp hiển thị trên sơ đồ bãi */
  displayOrder: number;
}

/**
 * Vị trí ô cụ thể trong bãi chứa container (Yard Slot).
 */
export interface YardSlot {
  /** Định danh duy nhất (GUID) của vị trí ô */
  id: string;
  /** Mã Block chứa vị trí ô này */
  blockId: string;
  /** Số Bay (Dãy theo chiều dọc bãi) */
  bay: number;
  /** Số Row (Dãy theo chiều ngang bãi) */
  row: number;
  /** Số Tier (Tầng xếp chồng từ 1 lên cao) */
  tier: number;
  /** Trạng thái ô: true nếu đã có container hạ bãi, false nếu đang trống */
  isOccupied: boolean;
  /** Mã Container đang chiếm giữ vị trí này (nếu có) */
  currentContainerId?: string;
}

/**
 * Bản đồ trực quan hóa toàn bộ bãi chứa container.
 */
export interface YardMapDto {
  /** Mã định danh Depot */
  depotId: string;
  /** Tên Depot */
  depotName: string;
  /** Danh sách các Block kèm các ô Slot bên trong */
  blocks: BlockWithSlots[];
}

/**
 * Thông tin Block mở rộng kèm danh sách các ô Slot tương ứng.
 */
export interface BlockWithSlots extends Block {
  /** Danh sách các ô Slot thuộc Block */
  slots: YardSlot[];
}

/**
 * Bản ghi giao dịch di chuyển / Biên bản bàn giao thiết bị (EIR - Container Movement).
 */
export interface ContainerMovement {
  /** Mã định danh duy nhất của lượt di chuyển (EIR Id) */
  id: string;
  /** Mã Container được di chuyển */
  containerId: string;
  /** Mã hãng tàu / Hãng khai thác */
  lineOperatorId: string;
  /** Mã ô bãi đã hạ container (nếu có) */
  yardSlotId?: string;
  /** Mã Block chứa container */
  blockId?: string;
  /** Phân loại container (A: Đạt chuẩn, B: Cần sửa nhẹ, C: Hư hỏng nặng) */
  classification: string;
  /** Tình trạng ngoại quan lúc vào cổng (Gate-In) */
  conditionAtGateIn: string;
  /** Tình trạng ngoại quan lúc ra cổng (Gate-Out) */
  conditionAtGateOut?: string;
  /** Biển số xe đầu kéo/rơ-moóc lúc vào cổng */
  vehicleInNumber?: string;
  /** Tên tài xế lúc vào cổng */
  driverInName?: string;
  /** Thời điểm xe vào cổng (ISO DateTime) */
  gateInAt: string;
  /** Biển số xe lúc ra cổng */
  vehicleOutNumber?: string;
  /** Tên tài xế lúc ra cổng */
  driverOutName?: string;
  /** Thời điểm xe ra cổng (ISO DateTime) */
  gateOutAt?: string;
  /** Trạng thái lượt di chuyển: 'InYard' (Đang trong bãi) hoặc 'GateOut' (Đã xuất bãi) */
  status: 'InYard' | 'GateOut';
  /** Mã đơn giao hàng (Delivery Order) liên kết lúc xuất bãi */
  deliveryOrderId?: string;
}

/**
 * Dữ liệu yêu cầu thực hiện thủ tục Nhập cổng (Gate-In Request).
 */
export interface GateInRequest {
  /** Số hiệu container (VD: MSCU1234567) */
  containerNumber: string;
  /** Mã Block chỉ định hạ bãi */
  blockId: string;
  /** Mã vị trí ô chỉ định hạ bãi (tùy chọn) */
  yardSlotId?: string;
  /** Mã hãng tàu sở hữu */
  lineOperatorId: string;
  /** Phân loại chất lượng container (A, B, C) */
  classification: string;
  /** Biển số xe chở container vào cổng */
  vehicleInNumber: string;
  /** Họ tên tài xế chở hàng vào */
  driverInName?: string;
  /** Tình trạng container ghi nhận tại cổng (Normal, Damaged...) */
  conditionAtGateIn: string;
}

/**
 * Dữ liệu yêu cầu thực hiện thủ tục Xuất cổng (Gate-Out Request).
 */
export interface GateOutRequest {
  /** Số hiệu container cần xuất khỏi bãi */
  containerNumber: string;
  /** Mã Lệnh giao nhận / Đơn giao hàng (Delivery Order Id) */
  deliveryOrderId: string;
  /** Biển số xe chở container ra cổng */
  vehicleOutNumber?: string;
  /** Họ tên tài xế lấy container */
  driverOutName?: string;
  /** Tình trạng container ghi nhận tại cổng khi xuất bãi */
  conditionAtGateOut: string;
}

/**
 * Lệnh giao nhận / Đơn giao hàng (Delivery Order - D/O).
 */
export interface DeliveryOrder {
  /** Mã định danh duy nhất (GUID) của D/O */
  id: string;
  /** Số chứng từ / Mã số đơn D/O */
  orderNumber: string;
  /** Mã khách hàng / Chủ hàng */
  customerId: string;
  /** Tên hiển thị của khách hàng */
  customerName?: string;
  /** Mã hãng tàu cấp lệnh */
  lineOperatorId: string;
  /** Tên hãng tàu cấp lệnh */
  lineOperatorName?: string;
  /** Ngày hết hạn hiệu lực của đơn giao hàng */
  expiryDate: string;
  /** Tên tàu và chuyến hành trình (Vessel/Voyage) */
  vesselVoyage?: string;
  /** Ghi chú bổ sung cho lệnh giao nhận */
  notes?: string;
  /** Trạng thái đơn: true nếu đã đóng/hoàn thành đủ số lượng, false nếu còn hiệu lực */
  isClosed: boolean;
  /** Chi tiết số lượng từng loại container cần giao */
  lines: DeliveryOrderLine[];
}

/**
 * Dòng chi tiết phân bổ loại container trong Đơn giao hàng.
 */
export interface DeliveryOrderLine {
  /** Mã dòng chi tiết */
  id: string;
  /** Mã đơn giao hàng trực thuộc */
  deliveryOrderId: string;
  /** Mã loại container yêu cầu (20DC, 40HC...) */
  containerTypeId: string;
  /** Tên loại container */
  containerTypeName?: string;
  /** Số lượng container yêu cầu xuất */
  requestedQuantity: number;
  /** Số lượng container thực tế đã xuất khỏi bãi */
  deliveredQuantity: number;
}

/**
 * Dữ liệu yêu cầu tạo mới Lệnh giao nhận (Create Delivery Order Request).
 */
export interface CreateDeliveryOrderRequest {
  /** Số lệnh giao nhận mới */
  orderNumber: string;
  /** Mã khách hàng nhận lệnh */
  customerId: string;
  /** Mã hãng tàu phát hành */
  lineOperatorId: string;
  /** Ngày hết hạn hiệu lực */
  expiryDate: string;
  /** Tên tàu / Số chuyến */
  vesselVoyage?: string;
  /** Ghi chú đơn */
  notes?: string;
  /** Danh sách các dòng số lượng loại container yêu cầu */
  lines: {
    containerTypeId: string;
    requestedQuantity: number;
    deliveredQuantity?: number;
  }[];
}

/**
 * Thông tin Khách hàng / Doanh nghiệp vận tải (Customer).
 */
export interface Customer {
  /** Mã định danh khách hàng */
  id: string;
  /** Mã số thuế doanh nghiệp */
  taxCode: string;
  /** Tên đầy đủ của khách hàng/doanh nghiệp */
  name: string;
}

/**
 * Thông tin Hãng tàu / Đơn vị khai thác (Line Operator).
 */
export interface LineOperator {
  /** Mã định danh hãng tàu */
  id: string;
  /** Mã viết tắt chuẩn quốc tế (VD: MSC, ONE, MAEU, CMA) */
  code: string;
  /** Tên đầy đủ của hãng tàu */
  name: string;
  /** Quốc gia xuất xứ */
  country?: string;
}

/**
 * Dòng dữ liệu báo cáo tuổi container tồn bãi (Yard Aging Row).
 */
export interface YardAgingRow {
  /** Mã hãng tàu */
  lineOperatorId: string;
  /** Mã viết tắt hãng tàu */
  lineOperatorCode: string;
  /** Tên hãng tàu */
  lineOperatorName: string;
  /** Phân nhóm số lượng container theo ngày lưu bãi */
  buckets: { 
    /** Số lượng container lưu bãi dưới 10 ngày */
    withinTenDays: number; 
    /** Số lượng container lưu bãi từ 10 ngày trở lên */
    tenDaysOrMore: number 
  };
}

/**
 * Báo cáo tuổi container lưu bãi theo từng hãng tàu (Yard Aging Report).
 */
export interface YardAgingReport {
  /** Thời điểm tổng hợp báo cáo */
  asOf: string;
  /** Danh sách các dòng dữ liệu hãng tàu */
  rows: YardAgingRow[];
}

/**
 * Dòng dữ liệu báo cáo sản lượng xuất/nhập cổng theo ngày (Daily Throughput Row).
 */
export interface DailyThroughputRow {
  /** Ngày thống kê */
  date: string;
  /** Mã hãng tàu */
  lineOperatorId: string;
  /** Mã viết tắt hãng tàu */
  lineOperatorCode: string;
  /** Tên hãng tàu */
  lineOperatorName: string;
  /** Tổng số lượt xe/container Gate-In trong ngày */
  gateIn: number;
  /** Tổng số lượt xe/container Gate-Out trong ngày */
  gateOut: number;
}

/**
 * Báo cáo tổng hợp sản lượng cổng theo khoảng thời gian (Daily Throughput Report).
 */
export interface DailyThroughputReport {
  /** Ngày bắt đầu thống kê */
  from: string;
  /** Ngày kết thúc thống kê */
  to: string;
  /** Danh sách chi tiết sản lượng từng ngày */
  rows: DailyThroughputRow[];
}

/**
 * Dữ liệu yêu cầu đăng nhập hệ thống (Login Request).
 */
export interface LoginRequest { 
  /** Địa chỉ email tài khoản */
  email: string; 
  /** Mật khẩu truy cập */
  password: string; 
}

/**
 * Kết quả xác thực trả về từ máy chủ (Authentication Response).
 */
export interface AuthResponse { 
  /** Chuỗi JWT Access Token dùng để xác thực các request API */
  accessToken: string; 
  /** Chuỗi Refresh Token dùng để cấp lại Access Token khi hết hạn */
  refreshToken: string; 
  /** Thời gian sống của Access Token tính bằng giây */
  expiresIn: number; 
}

/**
 * DTO tin nhắn trong cuộc trò chuyện với Trợ lý AI (AI Agent Message).
 */
export interface AgentMessageDto {
  /** Vai trò của người gửi: 'user' (người dùng), 'assistant' (trợ lý AI), 'system' (hệ thống) */
  role: 'user' | 'assistant' | 'system';
  /** Nội dung văn bản của tin nhắn */
  content: string;
  /** Thời gian gửi tin nhắn */
  timestamp?: string | Date;
}

/**
 * Yêu cầu gửi câu lệnh / lời nhắc cho Trợ lý AI thực thi (Agent Execute Request).
 */
export interface AgentExecuteRequest {
  /** Câu lệnh tự nhiên của người dùng (VD: 'Tìm các slot trống ở Block A') */
  prompt: string;
  /** Các tham số tùy chọn truyền thêm cho AI */
  parameters?: Record<string, unknown>;
  /** Lịch sử các câu đối đáp trước đó để duy trì ngữ cảnh */
  history?: AgentMessageDto[];
}

/**
 * Kết quả phản hồi từ Trợ lý AI sau khi thực thi câu lệnh (Agent Execute Response).
 */
export interface AgentExecuteResponse {
  /** Trạng thái thực thi: 'success', 'failed', 'clarification_needed' */
  status: string;
  /** Câu trả lời bằng ngôn ngữ tự nhiên gửi về cho người dùng */
  message: string;
  /** Dữ liệu có cấu trúc (bảng biểu, danh sách) trả về kèm theo */
  data?: unknown;
  /** Thông tin bổ sung (tên kỹ năng đã dùng, độ trễ xử lý) */
  metadata?: Record<string, unknown>;
}

/**
 * Thông tin kỹ năng mà Trợ lý AI có thể thực hiện (AI Skill Metadata).
 */
export interface SkillInfo {
  /** Định danh kỹ năng */
  skillId: string;
  /** Tên kỹ năng */
  name: string;
  /** Mô tả chức năng của kỹ năng */
  description: string;
  /** Danh sách câu lệnh mẫu gợi ý cho người dùng */
  examplePrompts: string[];
}
