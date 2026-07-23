using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InvenFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFilePathToInboundDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawAiJson",
                table: "InboundDocuments");

            migrationBuilder.RenameColumn(
                name: "FileUrl",
                table: "InboundDocuments",
                newName: "FilePath");

            migrationBuilder.AlterColumn<double>(
                name: "ConfidenceScore",
                table: "InboundDocuments",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FilePath",
                table: "InboundDocuments",
                newName: "FileUrl");

            migrationBuilder.AlterColumn<decimal>(
                name: "ConfidenceScore",
                table: "InboundDocuments",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AddColumn<string>(
                name: "RawAiJson",
                table: "InboundDocuments",
                type: "text",
                nullable: true);
        }
    }
}
