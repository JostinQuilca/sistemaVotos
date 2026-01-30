using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaVotoAPI.Migrations
{
    /// <inheritdoc />
    public partial class alfa2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Juntas_Direcciones_DireccionId",
                table: "Juntas");

            migrationBuilder.AlterColumn<string>(
                name: "JefeDeJuntaId",
                table: "Juntas",
                type: "character varying(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)");

            migrationBuilder.AddForeignKey(
                name: "FK_Juntas_Direcciones_DireccionId",
                table: "Juntas",
                column: "DireccionId",
                principalTable: "Direcciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Juntas_Direcciones_DireccionId",
                table: "Juntas");

            migrationBuilder.AlterColumn<string>(
                name: "JefeDeJuntaId",
                table: "Juntas",
                type: "character varying(10)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Juntas_Direcciones_DireccionId",
                table: "Juntas",
                column: "DireccionId",
                principalTable: "Direcciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
