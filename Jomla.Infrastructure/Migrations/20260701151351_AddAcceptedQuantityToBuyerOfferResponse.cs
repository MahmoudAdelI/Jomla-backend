using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jomla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAcceptedQuantityToBuyerOfferResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcceptedQuantity",
                table: "buyer_offer_responses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedQuantity",
                table: "buyer_offer_responses");
        }
    }
}
