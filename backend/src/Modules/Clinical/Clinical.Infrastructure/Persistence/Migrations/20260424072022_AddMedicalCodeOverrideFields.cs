using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinical.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalCodeOverrideFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                schema: "clinical",
                table: "MedicalCodes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsOverridden",
                schema: "clinical",
                table: "MedicalCodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OriginalAiCode",
                schema: "clinical",
                table: "MedicalCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OriginalAiConfidence",
                schema: "clinical",
                table: "MedicalCodes",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalAiDescription",
                schema: "clinical",
                table: "MedicalCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideNotes",
                schema: "clinical",
                table: "MedicalCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverrideReason",
                schema: "clinical",
                table: "MedicalCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "clinical",
                table: "MedicalCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CptCodes",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RVU = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CptCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    SourcePages = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    OcrConfidence = table.Column<float>(type: "real", nullable: false),
                    QualityFlags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.CheckConstraint("CK_DocumentChunks_OcrConfidence", "[OcrConfidence] >= 0.0 AND [OcrConfidence] <= 1.0");
                    table.ForeignKey(
                        name: "FK_DocumentChunks_ClinicalDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "clinical",
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Icd10Codes",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsBillable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Icd10Codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceVerifications",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InsuranceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MemberId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StatusMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceVerifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId",
                schema: "clinical",
                table: "DocumentChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId_ChunkIndex",
                schema: "clinical",
                table: "DocumentChunks",
                columns: new[] { "DocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_PatientId",
                schema: "clinical",
                table: "DocumentChunks",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Icd10Code_Code",
                schema: "clinical",
                table: "Icd10Codes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Icd10Code_Keywords",
                schema: "clinical",
                table: "Icd10Codes",
                column: "Keywords");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceVerification_AppointmentId",
                schema: "clinical",
                table: "InsuranceVerifications",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CptCodes",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "DocumentChunks",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "Icd10Codes",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "InsuranceVerifications",
                schema: "clinical");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "IsOverridden",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "OriginalAiCode",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "OriginalAiConfidence",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "OriginalAiDescription",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "OverrideNotes",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "OverrideReason",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "clinical",
                table: "MedicalCodes");
        }
    }
}
