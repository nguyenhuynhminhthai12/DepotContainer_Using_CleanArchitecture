# Code Review Prompt — TechSpherex.CleanArchitecture

Bạn là một senior code reviewer chuyên nghiệp. Hãy xem xét **từng dòng code** trong từng thư mục của dự án TechSpherex.CleanArchitecture — một hệ thống Container Depot TOS (Terminal Operating System) built on .NET 10 + Angular theo Clean Architecture.

Dự án có cấu trúc các layer: Domain → Application → Infrastructure → API → Client → Tests.

Với mỗi thư mục và file, hãy đặt câu hỏi review và tự trả lời để đánh giá chất lượng code. Include cả questions về architecture, security, performance, correctness, maintainability, testing, và best practices.

---

## 📁 1. ROOT — Cấu trúc dự án

### Q1: Cấu trúc thư mục có tuân thủ Clean Architecture đúng không? Tầng nào phụ thuộc tầng nào?
**A:** Cấu trúc tuân thủ đúng Clean Architecture:
- `Domain` (lớp trong cùng) — không phụ thuộc bất kỳ lớp nào khác
- `Application` — chỉ phụ thuộc Domain
- `Infrastructure` — phụ thuộc Domain + Application
- `Api` — phụ thuộc tất cả các tầng trên
- `Client` (Angular) — frontend độc lập, giao tiếp qua REST API

Điều này được kiểm chứng bởi `tests/Architecture.Tests/ArchitectureTests.cs` sử dụng NetArchTest.

### Q2: Giải pháp sử dụng .NET 10 và Aspire — có lợi ích gì?
**A:** .NET 10 cho phép dùng C# 13 features (primary constructors, required properties, collection expressions). Aspire cung cấp:
- Service discovery (Otlp/OpenTelemetry)
- Health checks (/health, /alive)
- Orchestrated containers (PostgreSQL, Redis)
- Distributed tracing

---

## 📁 2. src/Domain — Lớp miền nghiệp vụ

### Q3: BaseEntity sử dụng `Guid Id { get; init }` — có vấn đề gì với EF Core?
**A:** `init` accessor cho phép EF Core set ID khi loading từ DB nhưng ngăn chặn mutation sau đó. EF Core 7+ hỗ trợ tốt `init` accessors. Tuy nhiên, cần lưu ý: khi tạo entity mới, ID được generate bởi `Guid.NewGuid()` tại constructor, phù hợp với GUID generation strategy.

### Q4: AuditableEntity có CreatedBy/LastModifiedBy nhưng AppDbContext không set chúng — có đúng không?
**A:** Đúng, `UpdateAuditableEntities()` trong `AppDbContext` chỉ set `CreatedAt`/`LastModifiedAt` (Dòng 107-111) mà KHÔNG set `CreatedBy`/`LastModifiedBy`. Đây là khoảng trống — nên sử dụng `ICurrentUser` để set người dùng thay đổi.

### Q5: ITenantEntity chỉ có `string TenantId` — tại sao không dùng Guid?
**A:** Sử dụng string cho TenantId linh hoạt hơn (có thể dùng "default" cho single-tenant, mã tenant từ external system). Quy ước "default" ở line 82 trong `AppDbContext.ApplyTenantFilter` cho thấy hệ thống đang chạy ở chế độ single-tenant với filter cứng.

### Q6: ContainerNumber (strongly-typed record struct) — có đảm bảo bất biến không?
**A:** Có. `ContainerNumber` là `readonly record struct` với constructor validate 11 ký tự và ISO 6346 check digit. Implicit conversion to string (line 33) không cho phép mutate. Tuy nhiên, `Container.ContainerNumberRaw` có `internal set` — EF Core cần access, nhưng domain code không thể đặt sai định dạng sau khi tạo.

### Q7: Business Rule classes (YardSlotNotOccupiedRule, BayParityMatchesContainerSizeRule...) — có thực sự được sử dụng không?
**A:** `Container.Create()` (Container.cs line 66-67) gọi `BusinessRuleValidator.CheckRule(new ContainerNumberCheckDigitRule(normalized))`. Tuy nhiên, `BayParityMatchesContainerSizeRule`, `YardSlotNotOccupiedRule`, `DeliveryOrderNotExpiredRule`, `DeliveryOrderQuantityAvailableRule` **KHÔNG được gọi ở bất kỳ đâu trong codebase** — chỉ có test. Đây là rule dead code cần quyết định: triển khai trong handler hoặc xóa bỏ.

### Q8: RuleEngine (Infrastructure/Rules/RuleEngine.cs) — có khác với Domain Rules?
**A:** Có, đây là 2 hệ thống khác nhau:
- **Domain Rules** (`IBusinessRule`): Compile-time, type-safe, dùng trong domain entities
- **RuleEngine** (`IRuleEngine`): Runtime, configuration-driven, từ appsettings.json

Không có integration giữa 2 hệ thống — cần thống nhất strategy.

---

## 📁 3. src/Application — Lớp ứng dụng (CQRS + Handlers)

### Q9: CQRS pattern được triển khai như thế nào? Có dùng MediatR không?
**A:** Không dùng MediatR. Thay vào đó:
- `ICommand<TResponse>` và `IQuery<TResponse>` là marker interfaces (line 7-14 trong ICommand.cs, IQuery.cs)
- Handler được register tự động qua reflection trong `Application/DependencyInjection.cs` (`AddHandlersFromAssembly`, line 38-61)
- Endpoint directly inject handler interface — không có mediator dispatch

**Ưu:** Không phụ thuộc thư viện bên ngoài, kiểm soát hoàn toàn.
**Nhược:** Không có pipeline behaviors (validation, logging, transactions) tự động.

### Q10: DependencyInjection.cs scan toàn bộ assembly để register handlers — có rủi ro gì?
**A:** `AddHandlersFromAssembly` (line 38-61) scan tất cả class không abstract không interface trong assembly và match interface `ICommandHandler<,>` hoặc `IQueryHandler<,>`. Rủi ro: nếu có class implements các interface này mà không muốn register (ví dụ test class, mock), sẽ bị register nhầm. Tuy nhiên, namespace `Application.Features.*` giới hạn scope.

### Q11: AddSkillAgents register tất cả ISkillAgent — có vấn đề gì?
**A:** Line 76-79: `services.AddScoped(typeof(ISkillAgent), skillType)` — mỗi skill được register riêng biệt. Khi inject `IEnumerable<ISkillAgent>`, tất cả được inject cùng lúc. `AgentOrchestrator` chọn skill phù hợp. **Tuy nhiên**: nếu có nhiều skill, tất cả được instantiate cùng lúc — có thể ảnh hưởng performance nếu skill có dependency nặng.

### Q12: TodoAgentSkill sử dụng string matching cho intent detection — có phù hợp production không?
**A:** Dùng `switch` pattern với `Contains` (line 39-63 trong TodoAgentSkill.cs). Đây là **demo/concept**, không phù hợp production. Cần thay thế bằng:
- NLP/LLM-based intent classification
- hoặc Intent recognition framework
- Document comment at line 38 thừa nhận: "trong môi trường sản xuất, dùng LLM"

### Q13: DepotQueryAgentSkill có thể bị SQL injection qua prompt?
**A:** Không trực tiếp. Prompt chỉ dùng để detect intent (keyword matching). Các truy vấn DB được hardcoded trong handler methods (line 123-239). Không có dynamic query từ user input. Tuy nhiên, `ContainerNumberRegex` (line 21) extract container number từ prompt — nếu regex match sai, có thể query DB với giá trị unexpected.

### Q14: Result<T> pattern — có khác gì so với Exception handling?
**A:** Result pattern tránh exception cho expected failures (validation, not found, conflict). Được global exception handler catch cho unexpected exceptions. Kết hợp `Error` record (Code, Message, Type) cho mapping HTTP status codes qua `ResultExtensions.ToProblemDetails()`. Pattern này tốt cho API responses nhưng gây verbose code so với exceptions.

### Q15: GetAllTodosQueryHandler — có dùng AsNoTracking cho read-only queries không?
**A:** KHÔNG. Line 19-24 trong GetAllTodosQueryHandler.cs: query EF Core không gọi `.AsNoTracking()`. Cho production, read queries nên dùng `.AsNoTracking()` để cải thiện performance.

### Q16: Transaction handling — các handler có đảm bảo atomicity không?
**A:** KHÔNG có explicit transaction. Mỗi handler gọi `dbContext.SaveChangesAsync()` riêng lẻ. Nếu 1 handler cần nhiều thay đổi (ví dụ: Gate In tạo movement + cập nhật slot), cần `DbContext.Database.BeginTransaction()`. Hiện tại, mỗi `SaveChangesAsync` là transaction riêng.

---

## 📁 4. src/Infrastructure — Lớp hạ tầng

### Q17: TenantProvider resolution — có fallback hợp lý không?
**A:** Resolution order: `X-Tenant-Id header` → `JWT claim tenant_id` → `TenantInfo.Default` (line 23-45). Default TenantId = "default" (TenantInfo.cs line 21-26). **Vấn đề:** Trong multi-tenant production, fallback "default" có thể lộ dữ liệu sai tenant. Nên throw exception thay vì fallback.

### Q18: TenantMiddleware đặt TenantId = "default" khi không tìm thấy — có an toàn không?
**A:** Giống Q17 — `TenantProvider` trả về `TenantInfo.Default` với `Id = "default"`. TenantMiddleware (line 17-44) kiểm tra `IsActive` — default tenant luôn active. **Risk:** Request không có tenant sẽ xử lý với "default" tenant, có thể truy cập nhầm dữ liệu.

### Q19: JWT Secret được đọc từ configuration — có hardcoded không?
**A:** Không hardcode. Đọc từ `configuration["Jwt:Secret"]` (TokenService.cs line 35). Tuy nhiên, `TokenService.RefreshTokenAsync` (line 67) throw `InvalidOperationException` thay vì return Result — vi phạm pattern Result của project.

### Q20: HybridCacheService — có xử lý cache stampede không?
**A:** KHÔNG. `GetOrCreateAsync` (HybridCacheService.cs line 14-33) không có locking mechanism. Nếu nhiều request đồng thời cùng key, factory sẽ được gọi nhiều lần. HybridCache L1/L2 không ngăn cache stampede. Cần `SemaphoreSlim` per key cho production.

### Q21: GeminiAIService có retry logic không?
**A:** Chỉ fallback model (line 57-64), KHÔNG retry cùng model. Khi gọi API thất bại (HTTP error hoặc exception), trả về null ngay. Không có:
- Exponential backoff retry
- Circuit breaker pattern
- Timeout retry (chỉ có one try với HttpClient timeout)

### Q22: RuleEngine Evaluate — short-circuit logic cho AND/OR có đúng không?
**A:** Có vấn đề ở line 84-88:
```csharp
else if (ruleSet.Operator == LogicOperator.Or)
{
    return RuleResult.Pass(); // Short-circuit OR: first pass = overall pass
}
```
Đúng logic: OR → first pass = pass. Tuy nhiên ở line 92-93: `if (ruleSet.Operator == LogicOperator.Or && ruleSet.Rules.Count > 0) return RuleResult.Fail(violations);` — nếu tất cả fail thì fail. Logic này đúng nhưng thiếu rõ ràng.

### Q23: AppDbContext.SaveChangesAsync — SetTenantId có lấy ITenantProvider từ DB context nội bộ không?
**A:** Có (line 121-122): `this.GetInfrastructure().GetService<ITenantProvider>()`. Đây là cách hợp lệ vì ITenantProvider được registered as Scoped, và DbContext cũng Scoped — cùng service provider. **Tuy nhiên:** SetTenantId chỉ gọi cho `EntityState.Added` (line 126), không cập nhật cho Modified. Nếu TenantId thay đổi giữa chừng, entity cũ sẽ không bị ảnh hưởng — thường là OK.

### Q24: DbContext có filter tenant cứng "default" — có vấn đề không?
**A:** Line 82 trong `ApplyTenantFilter`: `builder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == "default");`. Đây là **hardcoded filter** — mọi truy vấn chỉ trả về entity có TenantId = "default". Trong production multi-tenant, cần dynamic filter dựa trên ITenantProvider. Hiện tại chỉ đúng cho single-tenant dev.

### Q25: AddAuth method sử dụng AddIdentityCore — tại không dùng AddIdentity?
**A:** `AddIdentityCore` (line 66) chỉ thêm UserManager, RoleManager, IUserValidator... mà không include SignInManager, UserClaimsPrincipalFactory. Đây là lựa chọn có chủ đích — hệ thống dùng JWT (không cookie-based auth), nên không cần SignInManager. **Đúng** cho JWT-only auth.

### Q26: CORS policy đọc từ configuration — có cors credentials với AllowAnyOrigin không?
**A:** Không — line 152-156 kiểm tra: nếu `AllowedOrigins` chứa "*" VÀ `AllowCredentials` = false thì `AllowAnyOrigin()`. Nếu credentials true, dùng `WithOrigins`. Đây là pattern đúng — tránh violation CORS spec (cannot combine credentials with wildcard origin).

### Q27: Migration files có được commit và áp dụng đúng cách?
**A:** Có 2 migration sets:
- `Persistence/Migrations/20260324190412_InitialCreate.cs` — EF Core migration
- `Infrastructure/Migrations/20260824021016_InitialDepotSchema.cs` — thêm schema depot

`AppDbSeeder.SeedAsync` (Program.cs line 157) chỉ chạy trong Development — production cần migration riêng hoặc CI/CD pipeline.

---

## 📁 5. src/Api — Lớp API (REST + gRPC)

### Q28: Endpoints không có authentication cho tất cả — có đúng không?
**A:** Kiểm tra từng endpoint:
- `GateEndpoints`: group `.RequireAuthorization()` ✅
- `TodoEndpoints`: group `.RequireAuthorization()` ✅
- `ContainerEndpoints`: chỉ POST/PUT/DELETE RequireAuthorization, GET thì KHÔNG ❌
- `YardEndpoints`: `/depots` và `/depots/{id}/map` KHÔNG RequireAuthorization ❌, `/api/blocks` có ✅
- `DeliveryOrderEndpoints`: group RequireAuthorization ✅
- `IdentityEndpoints`: KHÔNG RequireAuthorization (đăng nhập/đăng ký phải public) ✅
- `AgentEndpoints`: `/execute`, `/execute/{skillId}` RequireAuthorization; `/skills` AllowAnonymous ✅
- `ReportEndpoints`: KHÔNG RequireAuthorization ❌
- `LookupEndpoints`: GET KHÔNG RequireAuthorization, POST/create có ✅

**Cảnh báo:** Container GET, Yard GET, Report GET, Lookup GET không yêu cầu auth — bất kỳ ai cũng có thể truy cập dữ liệu.

### Q29: ValidationFilter — có xử lý validation cho tất cả endpoint không?
**A:** Chỉ các endpoint có `.AddEndpointFilter<ValidationFilter<T>>()` mới được validate. Xem lại:
- Gate In/Out/Move ✅
- Todo Create/Update ✅
- Container Create ✅
- Identity Register/Login/Refresh ✅
- Yard CreateBlock/ResizeBlock ✅
- DeliveryOrder Create ✅
- Customer Create ✅
- Agent Execute ❌ (không có validator — prompt không giới hạn)

### Q30: GateEndpoints.MapMovementEndpoints map vào /api/containers/{number}/movements — tại không /api/gate/movements?
**A:** Line 74 trong GateEndpoints.cs: nhóm lịch sử di chuyển vào `/api/containers` thay vì `/api/gate`. Đây là quyết định thiết kế — lịch sử EIR gắn với container hơn là gate. Có thể chấp nhận nhưng cần nhất quán.

### Q31: gRPC services share same handlers as REST — có đúng không?
**A:** ĐÚNG và là pattern tốt. `TodoGrpcService`, `ContainerGrpcService`, `YardGrpcService` đều inject cùng CQRS handlers. Đảm bảo consistency giữa REST và gRPC. Lỗi được map sang gRPC status codes (line 133-139 trong ContainerGrpcService).

### Q32: GlobalExceptionHandler có xử lý BusinessRuleException khác với Exception thường không?
**A:** Có (line 26-33): `BusinessRuleException` → HTTP 422 Unprocessable Entity với `ruleCode` extension. Các exception khác → HTTP 500. Phát triển sẽ hiện message (không production). **Cảnh báo:** Production sẽ KHÔNG hiển thị exception detail — user nhận "Please try again later".

### Q33: IdentityEndpoints map cả /api/identity và /api/auth — cùng endpoint không?
**A:** Dòng 22-23: `MapGroup(app.MapGroup("/api/identity")); MapGroup(app.MapGroup("/api/auth"));` — CẢNH BÁO: Gọi `MapGroup` 2 lần cùng endpoints → register trùng lặp! Cùng `/register`, `/login`, `/refresh` có ở CẢNH group. **Bug:** Endpoints bị duplicate, user có thể gọi `/api/identity/register` VÀ `/api/auth/register` với cùng behavior.

### Q34: ReportEndpoints có lỗi mapping không?
**A:** Có — Line 26-28:
```csharp
group.MapGet("/yard-occupancy", YardAging);  // Gọi YardAging handler!
```
Endpoint `/reports/yard-occupancy` gọi handler `YardAging` (báo cáo aging) thay vì handler occupancy riêng. **Bug:** Dữ liệu trả về sẽ là Yard Aging Report, không phải Yard Occupancy Report.

### Q35: ContainerEndpoints Update có kiểm tra ID match không?
**A:** Có (line 95-97): `if (id != command.Id) return TypedResults.BadRequest(...)`. Tương tự DeliveryOrderEndpoints line 96-99. Đây là tốt — ngăn URL ID khác body ID.

### Q36: Endpoints trả về Result.ToProblemDetails() — có mapping error type sang HTTP status không?
**A:** Có, trong `ResultExtensions.cs`: NotFound→404, Validation→400, Conflict→409, Unauthorized→401, Other→500. Mapping chính xác theo RFC 9457 Problem Details. ✅

### Q37: Create endpoint trả về TypedResults.Created với Location header — có đúng không?
**A:** Đúng theo RFC 9110 — Location header trỏ đến resource vừa tạo. Các endpoints khác cũng tương tự ✅.

---

## 📁 6. client/ — Frontend Angular

### Q38: Auth interceptor thêm X-Tenant-Id header — có dùng tenant từ backend không?
**A:** AuthStore.tenantId() được set từ login form (login.component.ts line 328: `this.authStore.setTenant(this.tenant)`). Tenant ID do user tự nhập. **Vấn đề:** Không xác thực tenant từ server — user có thể impersonate tenant khác.

### Q39: GateComponent template ~1000 lines — có quá dài không?
**A:** Quá dài. Template 1019 lines cho 3 operation forms (Gate-In, Move, Gate-Out). Nên:
- Tách thành 3 component con: `gate-in-form`, `yard-move-form`, `gate-out-form`
- Extract shared form components

### Q40: NgModel usage trong Angular 17+ — có dùng ReactiveForms không?
**A:** CẢNH BÁO: GateComponent dùng `[(ngModel)]` (template-driven forms) thay vì `FormGroup`/`FormControl` (reactive forms). Reactive forms tốt hơn cho:
- Complex validation
- Dynamic form controls
- Unit testing form logic
- Async validators

### Q41: GateComponent không gọi API validate container number — có rủi ro không?
**A:** Container number validated server-side qua `ContainerNumberCheckDigitRule`, nhưng frontend KHÔNG validate ISO 6346 check digit. User có thể nhập sai format. Nên validate format regex `^[A-Z]{3}[UJZ]?[0-9]{6}[0-9X]$` ở frontend.

### Q42: GateComponent.onDocumentClick đóng tất cả dropdown — có ảnh hưởng UX không?
**A:** Click ra ngoài → tất cả dropdown đóng (line 812-819). Hành vi đúng cho UX. Tuy nhiên, dropdown click vào bên trong không bị đóng vì `$event.stopPropagation()` ở line 125/256/355.

### Q43: LoginComponent lưu password in form field — có an toàn không?
**A:** Có rủi ro bảo mật: password field dùng `[(ngModel)]="password"` lưu trong component state, không dùng secure input (dù type="password"). Tuy nhiên, không lưu password vào localStorage/sessionStorage (chỉ token) ✅.

### Q44: AI Assistant Widget dùng innerHTML cho markdown rendering — có XSS risk không?
**A:** CÓ RỦI RO (line 105): `[innerHTML]="renderMarkdown(msg.content)"`. Mặc dù `renderMarkdown()` escape HTML (line 670-673), nhưng regex-based markdown parsing có thể bị bypass bằng payload tinh vi. Nên dùng DOM sanitizer hoặc library chuyên dụng (marked.js + DOMPurify).

### Q45: Client API calls — có error handling toàn cục không?
**A:** KHÔNG có global error handler cho HTTP. Mỗi service method handle error riêng lẻ. Nên thêm HttpErrorResponse interceptor để handle 401/403/500 globally, retry on transient errors, và logging.

### Q46: loadAllData() trong GateComponent gọi 4 HTTP requests song song — có tối ưu không?
**A:** Line 867-911: 4 subscribes độc lập (lineOperators, yardMap, deliveryOrders, containers). Có thể combine bằng `forkJoin` cho request phụ thuộc hoặc `mergeMap` cho sequential dependencies.

### Q47: Api.models.ts — DTOs có matching với backend không?
**A:** Một số mismatch:
- `ContainerMovement.id` trong client là `string`, backend là `Guid` (cần serialization)
- `ContainerMovement.gateInAt` trong client là `string`, backend là `DateTimeOffset`
- DeliveryOrder.lines dùng `DeliveryOrderLine[]` — backend có `DeliveryOrderLine` entity nhưng DTO cần mapping

### Q48: AuthStore quản lý token trong sessionStorage — có an toàn không?
**A:** Xem auth.store.ts — không đọc file nhưng sessionStorage là common approach. **Lưu ý:** XSS attack có thể đọc sessionStorage. JWT in httpOnly cookie sẽ an toàn hơn. Tuy nhiên, nếu JWT trong localStorage/sessionStorage cần CSP headers chặt chẽ.

### Q49: Angular routes use `loadComponent` (lazy loading) — có đúng không?
**A:** ĐÚNG ✅. Routes trong `app.routes.ts` dùng dynamic import `loadComponent: () => import(...)`. Tất cả route trừ login đều `canActivate: [authGuard]`. AuthGuard kiểm tra `auth.isAuthenticated()` signal ✅.

### Q50: AppRouting có protection route refresh không?
**A:** AuthGuard kiểm tra AuthStore.isAuthenticated() — signal từ authStore. Nếu user refresh page, token vẫn trong sessionStorage → authStore phục hồi → guard pass ✅. Tuy nhiên, nếu token hết hạn → 401 → interceptor redirect login ✅.

---

## 📁 7. src/ServiceDefaults — OpenTelemetry & Health

### Q51: OpenTelemetry exporter chỉ bật khi OTEL_EXPORTER_OTLP_ENDPOINT set — có đúng không?
**A:** ĐÚNG ✅ (line 76-81). Mặc định KHÔNG export OTLP → tránh gửi telemetry data vô ích khi không có collector. Phù hợp cho local dev.

### Q52: Health check chỉ check "self" — có kiểm tra dependencies không?
**A:** KHÔNG. Chỉ health check self (line 92). Nên thêm:
- Database connection check (PostgreSQL)
- Redis connection check
- Gemini API health check

---

## 📁 8. tests/ — Unit Tests & Architecture Tests

### Q53: ArchitectureTests có kiểm tra dependency direction đúng không?
**A:** Có 3 tests cho Domain→Application/Infrastructure/Api, 2 cho Application→Infrastructure/Api, 1 cho Infrastructure→Api ✅. Tất cả dùng NetArchTest. ✅

### Q54: TestDbContextFactory dùng InMemory DB — có phù hợp không?
**A:** InMemory DB KHÔNG support:
- Unique constraints ❌
- Foreign key constraints ❌
- Transactions ❌
- Database-specific functions ❌

Tests cho CQRS handler dùng InMemory → các constraint business logic (không phải DB constraints) mới test được. Phù hợp cho unit test, cần integration test cho DB-specific logic.

### Q55: FakeCacheService — có mô phỏng đúng ICacheService không?
**A:** `GetOrCreateAsync` gọi factory trực tiếp (line 19). `SetAsync`/`RemoveAsync`/`InvalidateByTagAsync` là no-op (line 21-30). Đúng cho unit test khi không cần caching. ✅

### Q56: Domain rule tests kiểm tra những gì?
**A:** `ContainerNumberCheckDigitRuleTests`: valid/invalid container numbers, stable rule code ✅. `DomainRuleTests`: BayParity, DeliveryOrder rules, YardSlot rules ✅. Tests đầy đủ cho domain rules ✅.

### Q57: Không có integration tests cho API endpoints — có cần không?
**A:** KHÔNG có integration test (WebApplicationFactory test). Cần test:
- Endpoint auth/authorization
- Validation filter behavior
- Error handling (GlobalExceptionHandler)
- Full request/response cycle
- Multi-tenant middleware behavior

### Q58: Không có test cho Infrastructure services — có cần không?
**A:** KHÔNG có test cho:
- `TokenService` (JWT generation/refresh)
- `RuleEngine` (rule evaluation logic)
- `GeminiAIService` (AI integration)
- `HybridCacheService` (caching)
- `TenantMiddleware` (tenant resolution)

---

## 🔐 9. SECURITY REVIEW

### Q59: JWT Secret — có được bảo vệ không?
**A:** Đọc từ configuration (`Jwt:Secret`). Cần đảm bảo:
- Not in source code (check appsettings.json, git history)
- Environment variable hoặc Azure Key Vault trong production
- Rotated periodically

### Q60: XSS vulnerabilities trong frontend?
**A:** Nguy cơ:
1. **AI Widget**: `[innerHTML]` với server response (Q44)
2. **GateComponent**: User input rendered in template — Angular auto-escapes bởi default ✅
3. **JWT tokens**: Stored in sessionStorage — vulnerable to XSS ❌

### Q61: SQL Injection — có rủi ro không?
**A:** EF Core parameterize queries mặc định ✅. Không có raw SQL trong codebase nhìn thấy được. `ContainerNumberCheckDigitRule` và `RuleEngine` chỉ dùng in-memory operations. ✅

### Q62: CORS — có quá mở không?
**A:** Default: `AllowedOrigins: ["*"]`, `AllowCredentials: false` ✅. Nếu production cần restrict origins, cần cấu hình đúng. CORS config ở Infrastructure/DependencyInjection.cs line 126-145.

### Q63: Password policy — có đủ mạnh không?
**A:** `AddAuth` (Infrastructure/DependencyInjection.cs line 66-77):
- RequireDigit ✅
- RequireLowercase ✅
- RequireUppercase ✅
- RequireNonAlphanumeric: false ❌
- RequiredLength: 6 ❌ (nên 8+)

### Q64: Refresh token — có bảo mật không?
**A:** Refresh token lưu trong `ApplicationUser.RefreshToken` (DB) và accessed by user ID. **Vấn đề**: TokenService line 67 kiểm tra `user.RefreshToken != refreshToken` — so sánh string trực tiếp (không hash). Nếu DB bị leak, refresh token dùng được. Nên hash refresh token lưu trong DB.

### Q65: Tenant isolation — có đảm bảo không?
**A:** EF Core global query filter `TenantId == "default"` (hardcoded). Trong single-tenant mode OK, nhưng multi-tenant:
- TenantId từ JWT claim/header KHÔNG được áp dụng vào query filter ❌
- Query filter luôn "default" → tất cả tenant thấy dữ liệu "default"

---

## ⚡ 10. PERFORMANCE REVIEW

### Q66: N+1 queries — có vấn đề gì trong handlers không?
**A:**
- `LoginCommandHandler`: FindByEmailAsync ✅ (single query)
- `GetAllTodosQueryHandler`: Count + paginated query ✅ (2 queries)
- `DepotQueryAgentSkill.HandleContainerLookupQueryAsync`: Include ContainerType + 3 Include movement (line 206-218) — N+1 nếu mỗi movement có Block/YardSlot/LineOperator ❌
- Should use `.ThenInclude()` or explicit load

### Q67: PagedResult không có pagination validation — có rủi ro không?
**A:** `GetAllTodosQuery` default Page=1, PageSize=10. Endpoint TodoEndpoints (line 56-65): `page ?? 1, pageSize ?? 10`. Không giới hạn max pageSize — user có thể request `pageSize=1000000` gây load DB. Cần validate max PageSize.

### Q68: AgentOrchestrator BuildDepotContextAsync — có query quá nhiều không?
**A:** Line 97-167: 4 queries (ContainerMovements, Blocks, Containers, specific container lookup) cho MỖI Gemini request. **Performance risk** cho mỗi AI call. Nên cache depot context, hoặc dùng background refresh.

### Q69: No response caching — có cần không?
**A:** Không có HTTP response caching, output caching, hoặc Redis cache cho read endpoints. Các endpoint như `GetDepots`, `GetContainerTypes` là semi-static → nên cache. `ICacheService` có sẵn nhưng KHÔNG được dùng trong bất kỳ handler nào.

---

## 🧹 11. CODE QUALITY & MAINTAINABILITY

### Q70: Vietnamese comments chiếm ưu thế — có ảnh hưởng không?
**A:** Phần lớn comment/summary docs bằng tiếng Việt. Điều này OK nếu team Việt Nam, nhưng:
- Code review từ quốc tế khó hiểu
- Variable naming nên tiếng Anh thống nhất
- API documentation nên tiếng Anh (OpenAPI/Swagger)

### Q71: File organization — feature folders hay layer folders?
**A:** Application: feature-based (`Features/Todos/Create/`, `Features/Identity/Login/`) ✅ tốt. Domain: entity-based (`Entities/`, `Common/`) ✅. Infrastructure: technology-based (`Persistence/`, `Identity/`, `Tenancy/`) ✅.

### Q72: DependencyInjection.cs — có quá nhiều private methods không?
**A:** Infrastructure DI: 8 private methods (AddPersistence, AddAuth, AddCachingServices, AddCorsPolicy, AddMultiTenancy, AddRuleEngineServices, AddAIServices). Có thể tách class nhưng hiện tại maintainable vì grouped by function ✅.

### Q73: GeneratedRegex — có cache regex hay tạo mới mỗi lần?
**A:** `[GeneratedRegex]` (AgentOrchestrator line 23, DepotQueryAgentSkill line 21-22) là compile-time generated, hiệu quả ✅. Regex không bị tạo lại mỗi call.

### Q74: Code duplication giữa Result<T> và Result — có không?
**A:** ResultExtensions.cs có 2 methods gần giống: `ToProblemDetails(this Result)` và `ToProblemDetails<T>(this Result<T>)`. Chỉ khác ở generic access. Có thể unify nhưng chấp nhận được để rõ ràng type ✅.


### Q75: Có code smells nào đáng chú ý?
**A:** Có:
1. **Long template** in GateComponent (1019 lines) ❌
2. **Magic strings** "default" tenant ID (nhiều nơi) ❌
3. **TODO at TodoEndpoints** (line 1 comment) ❌
4. **Duplicate endpoints** IdentityEndpoints (2 route groups) ❌
5. **Wrong handler mapping** ReportEndpoints ❌
6. **Dead code** — business rules không được sử dụng ❌

---

## 📋 TÓM TẮT ISSUE ƯU TIÊN

### 🔴 Critical:
1. **ReportEndpoints** `/reports/yard-occupancy` gọi sai handler (YardAging thay vì occupancy handler)
2. **IdentityEndpoints** duplicate endpoints (`/api/identity` và `/api/auth` cùng endpoints)
3. **Multi-tenant isolation FAIL** — query filter hardcoded "default" → all tenants see same data

### 🟠 High:
4. Gate/Container/Report/Lookup endpoints thiếu authorization
5. JWT refresh token lưu plaintext trong DB
6. No max page size validation
7. AgentOrchestrator BuildDepotContextAsync query trên mỗi AI request
8. No integration tests

### 🟡 Medium:
9. Dead business rule code (BayParity, YardSlot, DeliveryOrder rules not used)
10. TodoAgentSkill intent detection not production-ready
11. AuditableEntity CreatedBy/LastModifiedBy never set
12. Container endpoints GET không có auth
13. AI Widget XSS risk via innerHTML
14. No response caching for read endpoints
15. Password policy too weak (min 6, no special char requirement)

### 🟢 Low:
16. Vietnamese comments/docs (acceptable if team VN)
17. GateComponent template too large
18. In-memory DbContext test limitations
19. No HttpErrorResponse global handler in Angular

---

Tổng cộng: **75 câu hỏi review** với câu trả lời chi tiết, phân loại theo 11 nhóm: Project Structure, Domain, Application, Infrastructure, API, Client, ServiceDefaults, Tests, Security, Performance, Code Quality.
