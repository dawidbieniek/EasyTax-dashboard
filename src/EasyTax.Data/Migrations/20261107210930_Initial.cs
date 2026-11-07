using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EasyTax.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingYears",
                columns: table => new
                {
                    Year = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingYears", x => x.Year);
                });

            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Nip = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    PhoneNumber = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    JoinDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BankId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsSuspended = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    PlanName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    AuthorizationFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    SubscriptionFee = table.Column<decimal>(type: "TEXT", nullable: false),
                    EmploymentContract = table.Column<bool>(type: "INTEGER", nullable: false),
                    CashMethod = table.Column<bool>(type: "INTEGER", nullable: false),
                    TaxType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    PitPaymentFrequency = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IsVatPayer = table.Column<bool>(type: "INTEGER", nullable: false),
                    VatPaymentFrequency = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    UsesCashRegister = table.Column<bool>(type: "INTEGER", nullable: false),
                    FirstCashRegisterDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    ReliefType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReliefSinceDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    VatRDoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Ceidg1DoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ZusDoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InvoicedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ZusDraDoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TaxDoneAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clients_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientPhotos",
                columns: table => new
                {
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Bytes = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPhotos", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_ClientPhotos_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClientId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Month = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Income = table.Column<decimal>(type: "TEXT", nullable: false),
                    SalesInvoices = table.Column<int>(type: "INTEGER", nullable: false),
                    CostInvoices = table.Column<int>(type: "INTEGER", nullable: false),
                    IsAccounted = table.Column<bool>(type: "INTEGER", nullable: false),
                    JpkV7DoneAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VatUeDoneAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthRecords_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Banks_Name",
                table: "Banks",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clients_BankId",
                table: "Clients",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_LastName_FirstName",
                table: "Clients",
                columns: new[] { "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthRecords_ClientId_Month",
                table: "MonthRecords",
                columns: new[] { "ClientId", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingYears");

            migrationBuilder.DropTable(
                name: "ClientPhotos");

            migrationBuilder.DropTable(
                name: "MonthRecords");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Banks");
        }
    }
}
