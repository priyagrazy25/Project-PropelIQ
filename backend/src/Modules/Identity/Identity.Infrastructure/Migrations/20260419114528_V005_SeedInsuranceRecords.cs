using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V005_SeedInsuranceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsurancePlans",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuranceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ValidMemberIdPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsurancePlans", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "InsurancePlans",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "InsuranceName", "IsActive", "IsDeleted", "UpdatedAt", "ValidMemberIdPattern" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000001"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "Blue Cross Blue Shield", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^[A-Z]{3}\\d{9}$" },
                    { new Guid("a1b2c3d4-0002-0000-0000-000000000002"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "Aetna", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^\\d{8,12}$" },
                    { new Guid("a1b2c3d4-0003-0000-0000-000000000003"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "UnitedHealthcare", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^U\\d{9}$" },
                    { new Guid("a1b2c3d4-0004-0000-0000-000000000004"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cigna", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^\\d{10}$" },
                    { new Guid("a1b2c3d4-0005-0000-0000-000000000005"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "Humana", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^H\\d{8}$" },
                    { new Guid("a1b2c3d4-0006-0000-0000-000000000006"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "Kaiser Permanente", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^\\d{8,10}$" },
                    { new Guid("a1b2c3d4-0007-0000-0000-000000000007"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "Anthem", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^[A-Z]{2}\\d{9}$" },
                    { new Guid("a1b2c3d4-0008-0000-0000-000000000008"), new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), null, "Molina Healthcare", true, false, new DateTime(2026, 4, 19, 0, 0, 0, 0, DateTimeKind.Utc), "^\\d{9,12}$" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsurancePlan_InsuranceName",
                schema: "identity",
                table: "InsurancePlans",
                column: "InsuranceName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsurancePlans",
                schema: "identity");
        }
    }
}
