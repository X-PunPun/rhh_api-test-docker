using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RRHH.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class ModulosRrhh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Empleados_DepartamentoId",
                schema: "rrhh",
                table: "Empleados");

            migrationBuilder.AddColumn<string>(
                name: "Afp",
                schema: "rrhh",
                table: "Empleados",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AniosServicioPrevios",
                schema: "rrhh",
                table: "Empleados",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SistemaSalud",
                schema: "rrhh",
                table: "Empleados",
                type: "varchar(30)",
                unicode: false,
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Feriados",
                schema: "rrhh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanesSeguro",
                schema: "rrhh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aseguradora = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PrimaMensualUf = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesSeguro", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesVacaciones",
                schema: "rrhh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    DiasHabiles = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaSolicitud = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResueltaPorId = table.Column<int>(type: "int", nullable: true),
                    FechaResolucion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesVacaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolicitudesVacaciones_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalSchema: "rrhh",
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SolicitudesVacaciones_Empleados_ResueltaPorId",
                        column: x => x.ResueltaPorId,
                        principalSchema: "rrhh",
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AfiliacionesSeguro",
                schema: "rrhh",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    PlanSeguroId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaTermino = table.Column<DateOnly>(type: "date", nullable: true),
                    NumeroCargas = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfiliacionesSeguro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AfiliacionesSeguro_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalSchema: "rrhh",
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AfiliacionesSeguro_PlanesSeguro_PlanSeguroId",
                        column: x => x.PlanSeguroId,
                        principalSchema: "rrhh",
                        principalTable: "PlanesSeguro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "rrhh",
                table: "Feriados",
                columns: new[] { "Id", "Fecha", "Nombre" },
                values: new object[,]
                {
                    { 1, new DateOnly(2026, 1, 1), "Año Nuevo" },
                    { 2, new DateOnly(2026, 4, 3), "Viernes Santo" },
                    { 3, new DateOnly(2026, 4, 4), "Sábado Santo" },
                    { 4, new DateOnly(2026, 5, 1), "Día Nacional del Trabajo" },
                    { 5, new DateOnly(2026, 5, 21), "Día de las Glorias Navales" },
                    { 6, new DateOnly(2026, 6, 21), "Día Nacional de los Pueblos Indígenas" },
                    { 7, new DateOnly(2026, 6, 29), "San Pedro y San Pablo" },
                    { 8, new DateOnly(2026, 7, 16), "Día de la Virgen del Carmen" },
                    { 9, new DateOnly(2026, 8, 15), "Asunción de la Virgen" },
                    { 10, new DateOnly(2026, 9, 18), "Independencia Nacional" },
                    { 11, new DateOnly(2026, 9, 19), "Día de las Glorias del Ejército" },
                    { 12, new DateOnly(2026, 10, 12), "Encuentro de Dos Mundos" },
                    { 13, new DateOnly(2026, 10, 31), "Día de las Iglesias Evangélicas y Protestantes" },
                    { 14, new DateOnly(2026, 11, 1), "Día de Todos los Santos" },
                    { 15, new DateOnly(2026, 12, 8), "Inmaculada Concepción" },
                    { 16, new DateOnly(2026, 12, 25), "Navidad" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_DepartamentoId_FechaTermino",
                schema: "rrhh",
                table: "Empleados",
                columns: new[] { "DepartamentoId", "FechaTermino" });

            migrationBuilder.CreateIndex(
                name: "IX_AfiliacionesSeguro_EmpleadoId_PlanSeguroId",
                schema: "rrhh",
                table: "AfiliacionesSeguro",
                columns: new[] { "EmpleadoId", "PlanSeguroId" });

            migrationBuilder.CreateIndex(
                name: "IX_AfiliacionesSeguro_PlanSeguroId",
                schema: "rrhh",
                table: "AfiliacionesSeguro",
                column: "PlanSeguroId");

            migrationBuilder.CreateIndex(
                name: "IX_Feriados_Fecha",
                schema: "rrhh",
                table: "Feriados",
                column: "Fecha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanesSeguro_Aseguradora_Nombre",
                schema: "rrhh",
                table: "PlanesSeguro",
                columns: new[] { "Aseguradora", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVacaciones_EmpleadoId_Estado",
                schema: "rrhh",
                table: "SolicitudesVacaciones",
                columns: new[] { "EmpleadoId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesVacaciones_ResueltaPorId",
                schema: "rrhh",
                table: "SolicitudesVacaciones",
                column: "ResueltaPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AfiliacionesSeguro",
                schema: "rrhh");

            migrationBuilder.DropTable(
                name: "Feriados",
                schema: "rrhh");

            migrationBuilder.DropTable(
                name: "SolicitudesVacaciones",
                schema: "rrhh");

            migrationBuilder.DropTable(
                name: "PlanesSeguro",
                schema: "rrhh");

            migrationBuilder.DropIndex(
                name: "IX_Empleados_DepartamentoId_FechaTermino",
                schema: "rrhh",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "Afp",
                schema: "rrhh",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "AniosServicioPrevios",
                schema: "rrhh",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "SistemaSalud",
                schema: "rrhh",
                table: "Empleados");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_DepartamentoId",
                schema: "rrhh",
                table: "Empleados",
                column: "DepartamentoId");
        }
    }
}
