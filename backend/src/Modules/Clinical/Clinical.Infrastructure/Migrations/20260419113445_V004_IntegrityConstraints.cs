using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinical.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V004_IntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "clinical",
                table: "PatientViews360",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "clinical",
                table: "PatientViews360",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "clinical",
                table: "MedicalCodes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "clinical",
                table: "MedicalCodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "clinical",
                table: "ExtractedData",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "clinical",
                table: "ExtractedData",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "clinical",
                table: "DocumentEmbeddings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "clinical",
                table: "DocumentEmbeddings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "clinical",
                table: "DataConflicts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "clinical",
                table: "DataConflicts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "clinical",
                table: "ClinicalDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "clinical",
                table: "ClinicalDocuments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "clinical",
                table: "PatientViews360");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "clinical",
                table: "PatientViews360");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "clinical",
                table: "MedicalCodes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "clinical",
                table: "ExtractedData");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "clinical",
                table: "ExtractedData");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "clinical",
                table: "DocumentEmbeddings");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "clinical",
                table: "DocumentEmbeddings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "clinical",
                table: "DataConflicts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "clinical",
                table: "DataConflicts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "clinical",
                table: "ClinicalDocuments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "clinical",
                table: "ClinicalDocuments");
        }
    }
}
