using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V003_ClinicalAIEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "clinical");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResourceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BeforeState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterState = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalDocuments",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EncryptedFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcessingError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PatientViews360",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicalHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Medications = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LabResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Diagnoses = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastRefreshedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vitals = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientViews360", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataConflicts",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConflictingDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FieldName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SourceValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ConflictingValue = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolutionStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StaffNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataConflicts_ClinicalDocuments_ConflictingDocumentId",
                        column: x => x.ConflictingDocumentId,
                        principalSchema: "clinical",
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DataConflicts_ClinicalDocuments_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalSchema: "clinical",
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentEmbeddings",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    ChunkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingVector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VectorDimension = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentEmbeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentEmbeddings_ClinicalDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "clinical",
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedData",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    SourcePage = table.Column<int>(type: "int", nullable: true),
                    SourceText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedData", x => x.Id);
                    table.CheckConstraint("CK_ExtractedData_ConfidenceScore", "[ConfidenceScore] >= 0.0 AND [ConfidenceScore] <= 1.0");
                    table.ForeignKey(
                        name: "FK_ExtractedData_ClinicalDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "clinical",
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedicalCodes",
                schema: "clinical",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExtractedDataId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CodeType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicalCodes_ClinicalDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "clinical",
                        principalTable: "ClinicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MedicalCodes_ExtractedData_ExtractedDataId",
                        column: x => x.ExtractedDataId,
                        principalSchema: "clinical",
                        principalTable: "ExtractedData",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_ActorId",
                schema: "clinical",
                table: "AuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Resource",
                schema: "clinical",
                table: "AuditLogs",
                columns: new[] { "Resource", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_Timestamp",
                schema: "clinical",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalDocument_PatientId",
                schema: "clinical",
                table: "ClinicalDocuments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DataConflict_Patient_Status",
                schema: "clinical",
                table: "DataConflicts",
                columns: new[] { "PatientId", "ResolutionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_DataConflict_PatientId",
                schema: "clinical",
                table: "DataConflicts",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DataConflicts_ConflictingDocumentId",
                schema: "clinical",
                table: "DataConflicts",
                column: "ConflictingDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DataConflicts_SourceDocumentId",
                schema: "clinical",
                table: "DataConflicts",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentEmbedding_Document_Chunk",
                schema: "clinical",
                table: "DocumentEmbeddings",
                columns: new[] { "DocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedData_Document_Category",
                schema: "clinical",
                table: "ExtractedData",
                columns: new[] { "DocumentId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_ExtractedData_PatientId",
                schema: "clinical",
                table: "ExtractedData",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCode_Patient_Type_Code",
                schema: "clinical",
                table: "MedicalCodes",
                columns: new[] { "PatientId", "CodeType", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCode_PatientId",
                schema: "clinical",
                table: "MedicalCodes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCodes_DocumentId",
                schema: "clinical",
                table: "MedicalCodes",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicalCodes_ExtractedDataId",
                schema: "clinical",
                table: "MedicalCodes",
                column: "ExtractedDataId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientView360_PatientId",
                schema: "clinical",
                table: "PatientViews360",
                column: "PatientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "DataConflicts",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "DocumentEmbeddings",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "MedicalCodes",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "PatientViews360",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "ExtractedData",
                schema: "clinical");

            migrationBuilder.DropTable(
                name: "ClinicalDocuments",
                schema: "clinical");
        }
    }
}
