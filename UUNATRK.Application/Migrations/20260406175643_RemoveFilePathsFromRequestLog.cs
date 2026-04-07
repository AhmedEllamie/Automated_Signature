using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UUNATRK.Application.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFilePathsFromRequestLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaperImagePath",
                table: "RequestLogs");

            migrationBuilder.DropColumn(
                name: "SignatureSvgPath",
                table: "RequestLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaperImagePath",
                table: "RequestLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureSvgPath",
                table: "RequestLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
