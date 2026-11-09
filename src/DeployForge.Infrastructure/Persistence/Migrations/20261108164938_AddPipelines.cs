using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeployForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pipelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pipelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pipelines_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PipelineSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PipelineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Command = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipelineSteps_Pipelines_PipelineId",
                        column: x => x.PipelineId,
                        principalTable: "Pipelines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pipelines_ProjectId_Name",
                table: "Pipelines",
                columns: new[] { "ProjectId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PipelineSteps_PipelineId_Order",
                table: "PipelineSteps",
                columns: new[] { "PipelineId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PipelineSteps");

            migrationBuilder.DropTable(
                name: "Pipelines");
        }
    }
}
