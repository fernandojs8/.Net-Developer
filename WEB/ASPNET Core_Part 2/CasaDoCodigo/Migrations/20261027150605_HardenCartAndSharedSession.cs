using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CasaDoCodigo.Migrations
{
    /// <inheritdoc />
    public partial class HardenCartAndSharedSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Produto WHERE LEN(Codigo) > 450) OR EXISTS (SELECT 1 FROM Cadastro WHERE LEN(Nome) > 50)
                    THROW 51000, 'Revise valores legados maiores que o limite antes de migrar.', 1;
                IF EXISTS (SELECT Codigo FROM Produto GROUP BY Codigo HAVING COUNT(*) > 1)
                    THROW 51001, 'Revise codigos duplicados de produtos antes de migrar.', 1;
                IF EXISTS (SELECT PedidoId, ProdutoId FROM ItemPedido GROUP BY PedidoId, ProdutoId HAVING COUNT(*) > 1)
                    THROW 51002, 'Revise itens duplicados antes de migrar.', 1;
                IF EXISTS (SELECT 1 FROM ItemPedido WHERE Quantidade < 0 OR Quantidade > 99)
                    THROW 51003, 'Revise quantidades legadas antes de migrar.', 1;
                CREATE TABLE [dbo].[SessionCache] (
                    [Id] nvarchar(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL PRIMARY KEY,
                    [Value] varbinary(max) NOT NULL,
                    [ExpiresAtTime] datetimeoffset NOT NULL,
                    [SlidingExpirationInSeconds] bigint NULL,
                    [AbsoluteExpiration] datetimeoffset NULL
                );
                CREATE INDEX [IX_SessionCache_ExpiresAtTime] ON [dbo].[SessionCache] ([ExpiresAtTime]);
                """);
            migrationBuilder.DropIndex(
                name: "IX_ItemPedido_PedidoId",
                table: "ItemPedido");

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Produto",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ItemPedido",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Cadastro",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Produto_Codigo",
                table: "Produto",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemPedido_PedidoId_ProdutoId",
                table: "ItemPedido",
                columns: new[] { "PedidoId", "ProdutoId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ItemPedido_Quantidade",
                table: "ItemPedido",
                sql: "[Quantidade] >= 0 AND [Quantidade] <= 99");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("SessionCache");
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropIndex(
                name: "IX_Produto_Codigo",
                table: "Produto");

            migrationBuilder.DropIndex(
                name: "IX_ItemPedido_PedidoId_ProdutoId",
                table: "ItemPedido");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ItemPedido_Quantidade",
                table: "ItemPedido");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ItemPedido");

            migrationBuilder.AlterColumn<string>(
                name: "Codigo",
                table: "Produto",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Nome",
                table: "Cadastro",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_ItemPedido_PedidoId",
                table: "ItemPedido",
                column: "PedidoId");
        }
    }
}
