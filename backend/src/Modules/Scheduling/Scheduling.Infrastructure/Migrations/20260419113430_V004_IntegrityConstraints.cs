using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Scheduling.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class V004_IntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "scheduling",
                table: "Waitlists",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "Waitlists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "scheduling",
                table: "Providers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "Providers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "scheduling",
                table: "PreferredSlotSwaps",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "PreferredSlotSwaps",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "scheduling",
                table: "NoShowRiskScores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "NoShowRiskScores",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "scheduling",
                table: "IntakeRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "IntakeRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "scheduling",
                table: "AppointmentSlots",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "AppointmentSlots",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "Appointments",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "scheduling",
                table: "Appointments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "scheduling",
                table: "Waitlists");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "scheduling",
                table: "Waitlists");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "scheduling",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "scheduling",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "scheduling",
                table: "PreferredSlotSwaps");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "scheduling",
                table: "PreferredSlotSwaps");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "scheduling",
                table: "NoShowRiskScores");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "scheduling",
                table: "NoShowRiskScores");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "scheduling",
                table: "IntakeRecords");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "scheduling",
                table: "IntakeRecords");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "scheduling",
                table: "AppointmentSlots");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "scheduling",
                table: "AppointmentSlots");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "scheduling",
                table: "Appointments");

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                schema: "scheduling",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
