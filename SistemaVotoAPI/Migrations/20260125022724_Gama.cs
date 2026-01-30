using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaVotoAPI.Migrations
{
    /// <inheritdoc />
    public partial class Gama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Votantes_Elecciones_EleccionId",
                table: "Votantes");

            migrationBuilder.DropForeignKey(
                name: "FK_Votantes_Listas_ListaId",
                table: "Votantes");

            migrationBuilder.DropIndex(
                name: "IX_Votantes_EleccionId",
                table: "Votantes");

            migrationBuilder.DropIndex(
                name: "IX_Votantes_ListaId",
                table: "Votantes");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "Votantes");

            migrationBuilder.DropColumn(
                name: "EleccionId",
                table: "Votantes");

            migrationBuilder.DropColumn(
                name: "ListaId",
                table: "Votantes");

            migrationBuilder.DropColumn(
                name: "RolPostulante",
                table: "Votantes");

            migrationBuilder.CreateTable(
                name: "Candidatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Cedula = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ListaId = table.Column<int>(type: "integer", nullable: false),
                    EleccionId = table.Column<int>(type: "integer", nullable: false),
                    RolPostulante = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidatos_Elecciones_EleccionId",
                        column: x => x.EleccionId,
                        principalTable: "Elecciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Candidatos_Listas_ListaId",
                        column: x => x.ListaId,
                        principalTable: "Listas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Candidatos_Votantes_Cedula",
                        column: x => x.Cedula,
                        principalTable: "Votantes",
                        principalColumn: "Cedula",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_Cedula_EleccionId",
                table: "Candidatos",
                columns: new[] { "Cedula", "EleccionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_EleccionId",
                table: "Candidatos",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidatos_ListaId",
                table: "Candidatos",
                column: "ListaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Candidatos");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "Votantes",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EleccionId",
                table: "Votantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ListaId",
                table: "Votantes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RolPostulante",
                table: "Votantes",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votantes_EleccionId",
                table: "Votantes",
                column: "EleccionId");

            migrationBuilder.CreateIndex(
                name: "IX_Votantes_ListaId",
                table: "Votantes",
                column: "ListaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Votantes_Elecciones_EleccionId",
                table: "Votantes",
                column: "EleccionId",
                principalTable: "Elecciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Votantes_Listas_ListaId",
                table: "Votantes",
                column: "ListaId",
                principalTable: "Listas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
