using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jomla.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addedimagesanddescriptiontogroupRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ItemTitle",
                table: "group_requests",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "group_requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrls",
                table: "group_requests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "group_requests");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "group_requests");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "group_requests",
                newName: "ItemTitle");
        }
    }
}
