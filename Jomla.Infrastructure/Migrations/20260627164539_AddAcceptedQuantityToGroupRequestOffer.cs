using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jomla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptedQuantityToGroupRequestOffer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcceptedQuantity",
                table: "group_request_offers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedQuantity",
                table: "group_request_offers");
        }
    }
}
