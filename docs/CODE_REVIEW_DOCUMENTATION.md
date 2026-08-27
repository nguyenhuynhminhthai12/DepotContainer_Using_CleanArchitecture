# 🏗️ TÀI LIỆU REVIEW DỰ ÁN: TECHSPHEREX CONTAINER DEPOT MANAGEMENT SYSTEM

---

## 1. TỔNG QUAN HỆ THỐNG (EXECUTIVE SUMMARY)

**TechSpherex Container Depot Management System** là hệ thống quản lý bãi Depot Container đạt tiêu chuẩn sản xuất (Production-grade), được xây dựng trên nền tảng **Clean Architecture** kết hợp **Domain-Driven Design (DDD) Lite**, **CQRS**, **Multi-Tenancy** và hệ sinh thái phân tán hiện đại với **.NET 10**, **.NET Aspire**, **PostgreSQL**, **Redis**, **gRPC**, **OpenTelemetry** và **Angular 19**.

Hệ thống không dừng lại ở mức demo CRUD đơn giản mà số hóa và giải quyết trọn vẹn các quy trình vận hành Depot thực tế:

- **Quản lý sơ đồ bãi (Yard Management):** Cấu trúc không gian 4 chiều chuẩn cảng biển/depot: `Block → Bay → Row → Tier`.
- **Quy trình cổng (Gate Operations):** Quản lý Gate In / Gate Out và phát hành phiếu giao nhận container **EIR (Equipment Interchange Receipt)**.
- **Quản lý lệnh giao hàng (Delivery Orders - DO):** Cấp phát vỏ container rỗng hoặc giao container theo lệnh hãng tàu/chủ hàng với quy tắc hạn dùng và số lượng khả dụng.
- **Báo cáo chuyên sâu (Analytics & Reports):** Báo cáo tồn bãi theo độ tuổi (Yard Aging Report 0-10 ngày, ≥10 ngày) và Báo cáo thông lượng ngày (Daily Throughput Gate In/Out).
- **AI Skill Agent:** Trợ lý thông minh truy vấn thông số vận hành bãi bằng ngôn ngữ tự nhiên (`DepotQueryAgentSkill`).

---

## 2. KIẾN TRÚC HỆ THỐNG (CLEAN ARCHITECTURE & CQRS)

Dự án tuân thủ nghiêm ngặt nguyên lý **Inversion of Control (IoC)** và **Dependency Inversion Principle (DIP)**:

```text
┌─────────────────────────────────────────────────────────────┐
│                 Presentation Layer (Api)                    │
│   • Minimal APIs (REST Endpoints)                           │
│   • gRPC Services (High-Performance RPC)                    │
│   • Scalar API Documentation UI                             │
└──────────────────────────────┬──────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────┐
│                    Application Layer                        │
│   • CQRS Pattern (Commands & Queries)                       │
│   • Pipeline Validation (FluentValidation)                  │
│   • Application Interfaces & AI Skills                      │
└──────────────┬──────────────────────────────┬───────────────┘
               │                              │
┌──────────────▼──────────────┐┌──────────────▼───────────────┐
│        Domain Layer         ││    Infrastructure Layer      │
│ • Entities & Aggregates     ││ • PostgreSQL + EF Core       │
│ • Value Objects (ISO 6346)  ││ • Redis + HybridCache        │
│ • Business Rules (IRule)    ││ • JWT Auth & Multi-Tenancy   │
│ • Domain Enums              ││ • Serilog & OpenTelemetry    │
└─────────────────────────────┘└──────────────────────────────┘
```text

### 1. Domain Layer (`src/Domain`)

- **Đặc tính:** Lõi trung tâm, không phụ thuộc vào bất kỳ thư viện bên ngoài hay database nào.
- **Entities chính:**
  - `Depot`: Đại diện cho bãi/trạm container.
  - `Block`: Khu vực chứa container trong bãi, hỗ trợ Block vật lý hoặc Block ảo (Virtual Block).
  - `YardSlot`: Vị trí cụ thể 4D `(BlockId, Bay, Row, Tier)`.
  - `Container`: Thông tin vỏ container, kích thước (20ft, 40ft, 45ft), trọng tải, loại cont.
  - `ContainerMovement`: Lịch sử di chuyển, phiếu EIR (Gate In/Gate Out, xe kéo, tài xế, tình trạng hư hỏng).
  - `DeliveryOrder` & `DeliveryOrderLine`: Lệnh giao vỏ cont cho khách hàng.
  - `LineOperator` & `Customer`: Hãng tàu (Maersk, MSC, CMA-CGM...) và Khách hàng/chủ hàng.
- **Business Rules (`Domain/Common/Rules`):**
  - `ContainerNumberCheckDigitRule`: Kiểm tra tính hợp lệ của số container theo công thức **ISO 6346 Modulo-11**.
  - `BayParityMatchesContainerSizeRule`: Bay lẻ dành cho Cont 20ft, Bay chẵn dành cho Cont 40ft.
  - `YardSlotStackingWeightRule`: Kiểm tra quy tắc an toàn xếp chồng (trọng lượng tầng trên ≤ tầng dưới).

### 2. Application Layer (`src/Application`)

- **Đặc tính:** Điều phối luồng nghiệp vụ, hiện thực hóa các Use Cases theo mô hình **CQRS**.
- **Cấu trúc Modules:**
  - `Yard`: Các lệnh và truy vấn sơ đồ bãi (`GetYardMapQuery`, `UpdateSlotCommand`, `CreateBlockCommand`).
  - `Gate`: Quy trình cổng (`GateInCommand`, `GateOutCommand`, `GetEirQuery`).
  - `Containers`: Quản lý danh sách, tìm kiếm và chi tiết container.
  - `DeliveryOrders`: Tạo lệnh giao nhận, cấp phát cont theo hạn sử dụng và số lượng.
  - `Reports`: Tổng hợp báo cáo Aging và Throughput.
  - `Agents`: `DepotQueryAgentSkill` xử lý truy vấn AI tự động.
- **Validation:** Sử dụng `FluentValidation` kiểm tra tính hợp lệ của request trước khi vào handler xử lý.

### 3. Infrastructure Layer (`src/Infrastructure`)

- **Đặc tính:** Triển khai các interface từ Domain/Application.
- **Thành phần:**
  - **Database & EF Core:** `ApplicationDbContext` cấu hình Fluent API, Global Query Filter cho Multi-Tenant (`WHERE TenantId = @TenantId`), đánh Index hiệu năng cao trên `(BlockId, Bay, Row, Tier)`.
  - **Caching:** Tích hợp `HybridCache` kết hợp L1 In-Memory + L2 Redis, chống Cache Stampede và tối ưu tốc độ đọc sơ đồ bãi.
  - **Multi-Tenancy:** Tự động trích xuất `TenantId` từ Header `X-Tenant-Id` hoặc JWT Claim.
  - **Authentication:** Cấu hình JWT Bearer phân quyền Role-based / Policy-based.

### 4. Presentation & Api Layer (`src/Api`)

- **Dual-Protocol:**
  - **Minimal APIs:** Cung cấp RESTful Endpoints gọn nhẹ, phân nhóm qua `MapGroup()`: `/api/yard`, `/api/gate`, `/api/containers`, `/api/delivery-orders`, `/api/reports`.
  - **gRPC Services:** Cung cấp kênh RPC nhị phân tốc độ cao (`ContainerService`, `YardService`), trực tiếp tái sử dụng CQRS Handlers của Application Layer.
- **Tài liệu API:** Sử dụng giao diện hiện đại **Scalar** tại `/scalar/v1`.

### 5. AppHost & Service Defaults (`src/AppHost`, `src/ServiceDefaults`)

- **.NET Aspire:** Điều phối toàn bộ vòng đời ứng dụng (Postgres, Redis, PgAdmin, RedisInsight, API) trên môi trường phát triển chỉ với 1 cú click.
- **Observability:** Tích hợp OpenTelemetry thu thập Traces, Metrics và Logs về Grafana / Prometheus / OpenTelemetry Protocol Exporter.

### 6. Frontend Client (`client/`)

- **Framework:** **Angular 19** (Standalone Components, Signals, RxJS).
- **Tính năng UI:**
  - Giao diện **Yard Map 3D/2D grid** thể hiện trạng thái từng vị trí slot (trống, có cont, phân loại hãng tàu theo màu sắc).
  - Màn hình vận hành Gate In / Gate Out nhập liệu nhanh.
  - Quản lý lệnh giao hàng DO và danh sách Cont.
  - Dashboard biểu đồ thống kê Chart.js cho Aging và Throughput.
  - HTTP Interceptor tự động gắn JWT Token và `X-Tenant-Id`.

---

## 3. BẢNG TỔNG HỢP CÔNG NGHỆ (TECH STACK REFERENCE)

| Phân loại | Công nghệ / Thư viện | Phiên bản | Mục đích & Vị trí áp dụng |

|---|---|---|---|
| **Backend Runtime** | .NET (C#) | 10.0 | Nền tảng thực thi toàn bộ backend |
| **Orchestration** | .NET Aspire | 13.5 | Điều phối container và microservices (`src/AppHost`) |
| **Database** | PostgreSQL | 16+ / Npgsql 10.0 | Hệ cơ sở dữ liệu quan hệ lưu trữ dữ liệu chính |
| **ORM** | Entity Framework Core | 10.0.11 | Data access, Migrations, Global Query Filter |
| **Caching** | Redis + HybridCache | 10.4 | Cache phân tán 2 tầng L1 Memory / L2 Redis |
| **Giao tiếp RPC** | gRPC + Protobuf | 2.76 / 3.31 | Giao tiếp API nhị phân tốc độ cao (`src/Api/Protos`) |
| **Validation** | FluentValidation | 12.1 | Kiểm tra dữ liệu đầu vào theo nghiệp vụ |
| **API Docs** | Scalar API Reference | 2.13 | UI tương tác và kiểm thử API hiện đại |
| **Logging & Tracing**| Serilog, OpenTelemetry | 10.0 / 1.15 | Giám sát hệ thống, log tập trung ELK / Grafana |
| **Frontend** | Angular | 19.0 | Ứng dụng Web SPA quản lý bãi Depot (`client/`) |
| **Charts** | Chart.js / ng2-charts | 4.4 / 6.0 | Biểu đồ báo cáo thống kê Aging & Throughput |
| **Containers** | Docker & Compose | — | Container hóa toàn bộ hệ sinh thái |
| **Kiểm thử** | xUnit v3, NSubstitute | 3.2 / 5.3 | Unit Test & Architecture Rule Test (`tests/`) |

---

## 4. CÁC QUY TẮC NGHIỆP VỤ CỐT LÕI (CORE BUSINESS RULES)

1. **Chuẩn hóa số Container (ISO 6346 Modulo-11):**
   - Định dạng: 4 chữ cái (Mã chủ sở hữu + loại thiết bị `U/J/Z`) + 6 số seri + 1 số kiểm tra (Check Digit).
   - Kiểm tra tính hợp lệ bằng thuật toán nhân trọng số lũy thừa của 2 chia lấy dư 11.
2. **Quy tắc Bay chẵn / Bay lẻ (Bay Parity Rule):**
   - Container 20 feet bắt buộc xếp tại các Bay số **lẻ** (Bay 01, 03, 05...).
   - Container 40 feet chiếm 2 Bay 20ft liền kề và được định danh bằng Bay số **chẵn** ở giữa (Bay 02 = Bay 01 + 03).
3. **Quy tắc tải trọng và chiều cao tầng (Weight & Tier Safety Rule):**
   - Kiểm tra giới hạn số tầng tối đa (`MaxTier`) của Block.
   - Container nặng không được xếp chồng lên trên container nhẹ hơn hoặc container rỗng để đảm bảo an toàn sụt lún/đổ ngã.
4. **Quy tắc cấp vỏ theo lệnh giao hàng (Delivery Order Rule):**
   - Lệnh giao hàng phải còn hạn sử dụng (`ExpiryDate >= Today`).
   - Số lượng container xuất bãi không được vượt quá số lượng đăng ký trên DO (`DeliveredQty < RequestedQty`).

---

## 5. BỘ CÂU HỎI & TRẢ LỜI NHANH (Q&A CHEAT SHEET DÀNH CHO REVIEW)

### Q1: Tại sao dự án lại áp dụng Clean Architecture thay vì kiến trúc 3-Tier truyền thống?

> **Trả lời:** Clean Architecture cô lập hoàn toàn Domain và Nghiệp vụ cốt lõi (Core Business) khỏi sự phụ thuộc vào Database, Framework hay UI. Nếu sau này cần thay đổi từ PostgreSQL sang SQL Server, hay mở rộng thêm gRPC, GraphQL bên cạnh REST API, ta chỉ cần thay đổi tầng ngoài (Infrastructure/Api) mà không làm ảnh hưởng đến logic nghiệp vụ đã được kiểm thử ở Domain và Application.

### Q2: Cơ chế Multi-Tenancy hoạt động như thế nào và đảm bảo an toàn dữ liệu ra sao?

> **Trả lời:** Mọi Entity dữ liệu nghiệp vụ đều kế thừa `ITenantEntity` chứa thuộc tính `TenantId`. Khi có request đến, hệ thống tự động nhận diện Tenant qua Header `X-Tenant-Id` hoặc Claim từ JWT token. `ApplicationDbContext` của EF Core sử dụng cơ chế `HasQueryFilter` để tự động chèn mệnh đề `WHERE TenantId = @CurrentTenant` vào tất cả các câu lệnh SELECT, UPDATE, DELETE, đảm bảo không một Tenant nào có thể xem hoặc chỉnh sửa dữ liệu của Tenant khác.

### Q3: Ưu điểm của HybridCache so với IDistributedCache truyền thống là gì?

> **Trả lời:** `HybridCache` (ra mắt từ .NET 9) kết hợp bộ nhớ đệm 2 cấp: L1 là In-Memory Cache nằm ngay trong tiến trình ứng dụng (truy xuất siêu nhanh ở mức microsecond), và L2 là Redis Cache (chia sẻ dữ liệu giữa các node API). Đặc biệt, `HybridCache` tích hợp sẵn cơ chế chống hiện tượng **Cache Stampede** (khi 1 key hot hết hạn, chỉ có 1 luồng truy vấn database để cập nhật lại cache, các luồng khác sẽ chờ thay vì cùng lúc đổ dồn về database).

### Q4: Làm thế nào để API REST và gRPC chia sẻ chung logic mà không bị duplicate code?

> **Trả lời:** Cả Minimal API Endpoints và gRPC Services đều chỉ đóng vai trò là "cổng giao tiếp" (Adapters). Khi nhận request, cả hai đều chuyển tiếp dữ liệu về các CQRS Handlers nằm ở tầng `Application`. Do đó, toàn bộ logic tính toán, kiểm tra rule và truy xuất database chỉ được viết một lần duy nhất tại Application Layer.

### Q5: Dự án đã đảm bảo chất lượng mã nguồn (Code Quality) và kiểm thử như thế nào?

> **Trả lời:** Dự án tích hợp:

> 1. **Architecture Tests (`NetArchTest`):** Tự động kiểm tra xem các tầng có vi phạm nguyên tắc phụ thuộc của Clean Architecture hay không (ví dụ: Domain không được reference Infrastructure).
> 2. **Unit Tests (xUnit + NSubstitute + FluentAssertions):** Kiểm thử chi tiết các quy tắc tính toán (ISO 6346, Bay parity, DO validation).
> 3. **Static Analysis:** Cấu hình EditorConfig, SonarQube phân tích code smells và lỗ hổng bảo mật.
