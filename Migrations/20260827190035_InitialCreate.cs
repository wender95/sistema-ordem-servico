using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaOrdemServico.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FluxosOS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroOS = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IdentificadorFluxo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NomeCliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SetorAtual = table.Column<int>(type: "integer", nullable: false),
                    SetorAnterior = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataEncerramento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FluxosOS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeUsuario = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    SenhaHash = table.Column<string>(type: "text", nullable: false),
                    SenhaSalt = table.Column<string>(type: "text", nullable: false),
                    Perfil = table.Column<int>(type: "integer", nullable: false),
                    Setor = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventosMovimentacao",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FluxoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Setor = table.Column<int>(type: "integer", nullable: false),
                    TipoEvento = table.Column<int>(type: "integer", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MotivoJustificativa = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosMovimentacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventosMovimentacao_FluxosOS_FluxoId",
                        column: x => x.FluxoId,
                        principalTable: "FluxosOS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventosMovimentacao_FluxoId",
                table: "EventosMovimentacao",
                column: "FluxoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NomeUsuario",
                table: "Usuarios",
                column: "NomeUsuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventosMovimentacao");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "FluxosOS");
        }
    }
}
