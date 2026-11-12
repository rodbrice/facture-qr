using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactureQr.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_building_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    address_country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    address_name = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    address_postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    address_street = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    address_town = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice_number_counters",
                columns: table => new
                {
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_number_counters", x => x.year);
                    table.CheckConstraint("ck_invoice_number_counters_last_sequence", "last_sequence >= 1");
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    total = table.Column<decimal>(type: "numeric(11,2)", precision: 11, scale: 2, nullable: false),
                    number_year = table.Column<int>(type: "integer", nullable: true),
                    number_sequence = table.Column<int>(type: "integer", nullable: true),
                    issue_date = table.Column<DateOnly>(type: "date", nullable: true),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    paid_on = table.Column<DateOnly>(type: "date", nullable: true),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    bill_to_building_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    bill_to_country = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    bill_to_name = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: false),
                    bill_to_postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    bill_to_street = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    bill_to_town = table.Column<string>(type: "character varying(35)", maxLength: 35, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoices", x => x.id);
                    table.CheckConstraint("ck_invoices_issued_fields", "(status = 'Draft' AND number_year IS NULL AND number_sequence IS NULL AND issue_date IS NULL AND due_date IS NULL) OR (status <> 'Draft' AND number_year IS NOT NULL AND number_sequence IS NOT NULL AND issue_date IS NOT NULL AND due_date IS NOT NULL AND number_year = EXTRACT(YEAR FROM issue_date) AND due_date >= issue_date AND total >= 0.01)");
                    table.CheckConstraint("ck_invoices_paid_on", "(status = 'Paid') = (paid_on IS NOT NULL)");
                    table.CheckConstraint("ck_invoices_total_range", "total >= 0 AND total <= 999999999.99");
                    table.ForeignKey(
                        name: "fk_invoices_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(9,3)", precision: 9, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(11,2)", precision: 11, scale: 2, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(11,2)", precision: 11, scale: 2, nullable: false),
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invoice_lines", x => x.id);
                    table.CheckConstraint("ck_invoice_lines_amount", "amount >= 0");
                    table.CheckConstraint("ck_invoice_lines_quantity", "quantity > 0");
                    table.CheckConstraint("ck_invoice_lines_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "fk_invoice_lines_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_invoice_id_position",
                table: "invoice_lines",
                columns: new[] { "invoice_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_customer_id",
                table: "invoices",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ux_invoices_number",
                table: "invoices",
                columns: new[] { "number_year", "number_sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "invoice_number_counters");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "customers");
        }
    }
}
