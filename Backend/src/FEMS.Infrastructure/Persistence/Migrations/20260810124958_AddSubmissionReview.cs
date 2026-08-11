using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FEMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "FormSubmissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewStatus",
                table: "FormSubmissions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAt",
                table: "FormSubmissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByUserId",
                table: "FormSubmissions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserId",
                table: "FormSubmissions");
        }
    }
}
