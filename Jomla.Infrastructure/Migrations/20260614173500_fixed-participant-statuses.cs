using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jomla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixedparticipantstatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BatchNumber",
                table: "supplier_batches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "group_request_participants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "batch_participants",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "supplier_batches");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "group_request_participants");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "batch_participants");
        }
    }
}
