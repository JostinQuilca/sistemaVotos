using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVotoAPI.Migrations
{
    /// <inheritdoc />
    public partial class alfa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EleccionId",
                table: "Votantes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votantes_EleccionId",
                table: "Votantes",
                column: "EleccionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Votantes_Elecciones_EleccionId",
                table: "Votantes",
                column: "EleccionId",
                principalTable: "Elecciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Votantes_Elecciones_EleccionId",
                table: "Votantes");

            migrationBuilder.DropIndex(
                name: "IX_Votantes_EleccionId",
                table: "Votantes");

            migrationBuilder.DropColumn(
                name: "EleccionId",
                table: "Votantes");
        }
    }
}
