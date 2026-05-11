using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api_service.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "S3Url",
                table: "Documents",
                newName: "FileUrl");

            migrationBuilder.RenameColumn(
                name: "CreateTime",
                table: "Documents",
                newName: "CreateAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileUrl",
                table: "Documents",
                newName: "S3Url");

            migrationBuilder.RenameColumn(
                name: "CreateAt",
                table: "Documents",
                newName: "CreateTime");
        }
    }
}
