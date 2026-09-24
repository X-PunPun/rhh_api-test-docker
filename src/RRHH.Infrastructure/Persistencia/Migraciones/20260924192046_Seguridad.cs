using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RRHH.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Seguridad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "seguridad");

            migrationBuilder.CreateTable(
                name: "Auditoria",
                schema: "seguridad",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CodigoResultado = table.Column<int>(type: "int", nullable: true),
                    Ip = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "seguridad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    HashClave = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Rol = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: true),
                    Regiones = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    IntentosFallidos = table.Column<int>(type: "int", nullable: false),
                    BloqueadoHasta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SelloSeguridad = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UltimoAcceso = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalSchema: "rrhh",
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokensRenovacion",
                schema: "seguridad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Hash = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SelloSeguridad = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    CreadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiraEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevocadoEn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoRevocacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensRenovacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensRenovacion_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "seguridad",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Fecha",
                schema: "seguridad",
                table: "Auditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_UsuarioId",
                schema: "seguridad",
                table: "Auditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensRenovacion_Hash",
                schema: "seguridad",
                table: "TokensRenovacion",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensRenovacion_UsuarioId_RevocadoEn",
                schema: "seguridad",
                table: "TokensRenovacion",
                columns: new[] { "UsuarioId", "RevocadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                schema: "seguridad",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_EmpleadoId",
                schema: "seguridad",
                table: "Usuarios",
                column: "EmpleadoId",
                unique: true,
                filter: "[EmpleadoId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditoria",
                schema: "seguridad");

            migrationBuilder.DropTable(
                name: "TokensRenovacion",
                schema: "seguridad");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "seguridad");
        }
    }
}
