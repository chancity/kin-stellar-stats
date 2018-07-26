using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace kin_stellar_stats.Migrations
{
    public partial class Migration1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FlattenedOperation",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    SourceAccount = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    EffectType = table.Column<string>(nullable: true),
                    Memo = table.Column<string>(nullable: true),
                    Discriminator = table.Column<string>(nullable: false),
                    Account = table.Column<string>(nullable: true),
                    Funder = table.Column<string>(nullable: true),
                    StartingBalance = table.Column<double>(nullable: true),
                    Amount = table.Column<double>(nullable: true),
                    AssetCode = table.Column<string>(nullable: true),
                    AssetIssuer = table.Column<string>(nullable: true),
                    AssetType = table.Column<string>(nullable: true),
                    From = table.Column<string>(nullable: true),
                    To = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlattenedOperation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KinAccounts",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Memo = table.Column<string>(nullable: true),
                    Balance = table.Column<double>(nullable: false),
                    AccountCreditedCount = table.Column<int>(nullable: false),
                    AccountDebitedCount = table.Column<int>(nullable: false),
                    AccountCreditedVolume = table.Column<double>(nullable: false),
                    AccountDebitedVolume = table.Column<double>(nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    LastActive = table.Column<DateTimeOffset>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KinAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paginations",
                columns: table => new
                {
                    CursorType = table.Column<string>(nullable: false),
                    PagingToken = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paginations", x => x.CursorType);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Paginations_CursorType",
                table: "Paginations",
                column: "CursorType",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FlattenedOperation");

            migrationBuilder.DropTable(
                name: "KinAccounts");

            migrationBuilder.DropTable(
                name: "Paginations");
        }
    }
}
