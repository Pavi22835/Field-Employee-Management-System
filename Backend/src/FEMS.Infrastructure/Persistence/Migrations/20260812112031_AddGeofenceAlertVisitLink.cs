using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FEMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGeofenceAlertVisitLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FieldVisitId",
                table: "SecurityAlerts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAlerts_FieldVisitId",
                table: "SecurityAlerts",
                column: "FieldVisitId");

            migrationBuilder.AddForeignKey(
                name: "FK_SecurityAlerts_FieldVisits_FieldVisitId",
                table: "SecurityAlerts",
                column: "FieldVisitId",
                principalTable: "FieldVisits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SecurityAlerts_FieldVisits_FieldVisitId",
                table: "SecurityAlerts");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAlerts_FieldVisitId",
                table: "SecurityAlerts");

            migrationBuilder.DropColumn(
                name: "FieldVisitId",
                table: "SecurityAlerts");
        }
    }
}
