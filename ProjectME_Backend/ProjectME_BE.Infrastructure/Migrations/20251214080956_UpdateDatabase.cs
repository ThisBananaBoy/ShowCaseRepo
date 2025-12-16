using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectME_BE.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId1",
                table: "deadlines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_deadlines_ProjectId1",
                table: "deadlines",
                column: "ProjectId1");

            migrationBuilder.AddForeignKey(
                name: "FK_deadlines_projects_ProjectId1",
                table: "deadlines",
                column: "ProjectId1",
                principalTable: "projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deadlines_projects_ProjectId1",
                table: "deadlines");

            migrationBuilder.DropIndex(
                name: "IX_deadlines_ProjectId1",
                table: "deadlines");

            migrationBuilder.DropColumn(
                name: "ProjectId1",
                table: "deadlines");
        }
    }
}
