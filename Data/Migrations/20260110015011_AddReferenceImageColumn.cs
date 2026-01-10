using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashArt.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceImageColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceImageUrl",
                table: "CompetitionThemes",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReferenceImageUrl",
                table: "CompetitionThemes");
        }
    }
}
