using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeDraftLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneratedContent",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "JobDescription",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "PdfBlobUrl",
                table: "Resumes");

            migrationBuilder.RenameColumn(
                name: "TargetJobTitle",
                table: "Resumes",
                newName: "TargetCompany");

            migrationBuilder.RenameColumn(
                name: "CompanyDescription",
                table: "Resumes",
                newName: "FailedReason");

            migrationBuilder.AddColumn<string>(
                name: "EditedResumeJson",
                table: "Resumes",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeneratedResumeJson",
                table: "Resumes",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenerationRequestJson",
                table: "Resumes",
                type: "character varying(20000)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Resumes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedResumeJson",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "GeneratedResumeJson",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "GenerationRequestJson",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Resumes");

            migrationBuilder.RenameColumn(
                name: "TargetCompany",
                table: "Resumes",
                newName: "TargetJobTitle");

            migrationBuilder.RenameColumn(
                name: "FailedReason",
                table: "Resumes",
                newName: "CompanyDescription");

            migrationBuilder.AddColumn<string>(
                name: "GeneratedContent",
                table: "Resumes",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobDescription",
                table: "Resumes",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfBlobUrl",
                table: "Resumes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
