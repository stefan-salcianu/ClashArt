using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClashArt.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProofOfWorkVideoUrl",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProofOfWorkVideoUrl",
                table: "Posts");
        }
    }
}
