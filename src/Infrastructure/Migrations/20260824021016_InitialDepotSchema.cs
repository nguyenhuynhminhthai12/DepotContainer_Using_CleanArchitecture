using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechSpherex.CleanArchitecture.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDepotSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContainerTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Family = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Depots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Depots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineOperators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineOperators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Containers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerNumberRaw = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    ContainerTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SizeFeet = table.Column<int>(type: "integer", nullable: false),
                    MaxWeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TareWeightKg = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ManufactureDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Owner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Containers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Containers_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Blocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    MaxBay = table.Column<int>(type: "integer", nullable: true),
                    MaxRow = table.Column<int>(type: "integer", nullable: true),
                    MaxTier = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Blocks_Depots_DepotId",
                        column: x => x.DepotId,
                        principalTable: "Depots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VesselVoyage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOrders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryOrders_LineOperators_LineOperatorId",
                        column: x => x.LineOperatorId,
                        principalTable: "LineOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YardSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Bay = table.Column<int>(type: "integer", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    IsOccupied = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YardSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YardSlots_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrderLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedQuantity = table.Column<int>(type: "integer", nullable: false),
                    DeliveredQuantity = table.Column<int>(type: "integer", nullable: false),
                    DeliveryOrderId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrderLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryOrderLines_ContainerTypes_ContainerTypeId",
                        column: x => x.ContainerTypeId,
                        principalTable: "ContainerTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryOrderLines_DeliveryOrders_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliveryOrderLines_DeliveryOrders_DeliveryOrderId1",
                        column: x => x.DeliveryOrderId1,
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ContainerMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineOperatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    YardSlotId = table.Column<Guid>(type: "uuid", nullable: true),
                    BlockId = table.Column<Guid>(type: "uuid", nullable: true),
                    Classification = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ConditionAtGateIn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConditionAtGateOut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VehicleInNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DriverInName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GateInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    VehicleOutNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DriverOutName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GateOutAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContainerMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_Containers_ContainerId",
                        column: x => x.ContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_DeliveryOrders_DeliveryOrderId",
                        column: x => x.DeliveryOrderId,
                        principalTable: "DeliveryOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_LineOperators_LineOperatorId",
                        column: x => x.LineOperatorId,
                        principalTable: "LineOperators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ContainerMovements_YardSlots_YardSlotId",
                        column: x => x.YardSlotId,
                        principalTable: "YardSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_DepotId_Code",
                table: "Blocks",
                columns: new[] { "DepotId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_TenantId",
                table: "Blocks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_BlockId",
                table: "ContainerMovements",
                column: "BlockId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_ContainerId",
                table: "ContainerMovements",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_DeliveryOrderId",
                table: "ContainerMovements",
                column: "DeliveryOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_GateInAt",
                table: "ContainerMovements",
                column: "GateInAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_GateOutAt",
                table: "ContainerMovements",
                column: "GateOutAt");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_LineOperatorId",
                table: "ContainerMovements",
                column: "LineOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_Status",
                table: "ContainerMovements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_TenantId",
                table: "ContainerMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_TenantId_ContainerId_GateInAt",
                table: "ContainerMovements",
                columns: new[] { "TenantId", "ContainerId", "GateInAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContainerMovements_YardSlotId",
                table: "ContainerMovements",
                column: "YardSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_Condition",
                table: "Containers",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_ContainerTypeId",
                table: "Containers",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_TenantId",
                table: "Containers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Containers_TenantId_ContainerNumberRaw",
                table: "Containers",
                columns: new[] { "TenantId", "ContainerNumberRaw" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTypes_TenantId",
                table: "ContainerTypes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ContainerTypes_TenantId_Code",
                table: "ContainerTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId",
                table: "Customers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TenantId_TaxCode",
                table: "Customers",
                columns: new[] { "TenantId", "TaxCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderLines_ContainerTypeId",
                table: "DeliveryOrderLines",
                column: "ContainerTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderLines_DeliveryOrderId_ContainerTypeId",
                table: "DeliveryOrderLines",
                columns: new[] { "DeliveryOrderId", "ContainerTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrderLines_DeliveryOrderId1",
                table: "DeliveryOrderLines",
                column: "DeliveryOrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_CustomerId",
                table: "DeliveryOrders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_ExpiryDate",
                table: "DeliveryOrders",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_IsClosed",
                table: "DeliveryOrders",
                column: "IsClosed");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_LineOperatorId",
                table: "DeliveryOrders",
                column: "LineOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_TenantId",
                table: "DeliveryOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrders_TenantId_OrderNumber",
                table: "DeliveryOrders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Depots_Code",
                table: "Depots",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Depots_TenantId",
                table: "Depots",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LineOperators_TenantId",
                table: "LineOperators",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LineOperators_TenantId_Code",
                table: "LineOperators",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardSlots_BlockId_Bay_Row_Tier",
                table: "YardSlots",
                columns: new[] { "BlockId", "Bay", "Row", "Tier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YardSlots_IsOccupied",
                table: "YardSlots",
                column: "IsOccupied");

            migrationBuilder.CreateIndex(
                name: "IX_YardSlots_TenantId",
                table: "YardSlots",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContainerMovements");

            migrationBuilder.DropTable(
                name: "DeliveryOrderLines");

            migrationBuilder.DropTable(
                name: "Containers");

            migrationBuilder.DropTable(
                name: "YardSlots");

            migrationBuilder.DropTable(
                name: "DeliveryOrders");

            migrationBuilder.DropTable(
                name: "ContainerTypes");

            migrationBuilder.DropTable(
                name: "Blocks");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "LineOperators");

            migrationBuilder.DropTable(
                name: "Depots");
        }
    }
}
