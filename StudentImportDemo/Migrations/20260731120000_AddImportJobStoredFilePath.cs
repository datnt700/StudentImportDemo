using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StudentImportDemo.Data;

#nullable disable

namespace StudentImportDemo.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260731120000_AddImportJobStoredFilePath")]
    public partial class AddImportJobStoredFilePath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "ImportJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoredFilePath",
                table: "ImportJobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "ImportJobs");

            migrationBuilder.DropColumn(
                name: "StoredFilePath",
                table: "ImportJobs");
        }
    }
}