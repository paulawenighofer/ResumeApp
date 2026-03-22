using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Certifications_AspNetUsers_ApplicationUserId",
                table: "Certifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Educations_AspNetUsers_ApplicationUserId",
                table: "Educations");

            migrationBuilder.DropForeignKey(
                name: "FK_Experiences_AspNetUsers_ApplicationUserId",
                table: "Experiences");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AspNetUsers_ApplicationUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Resumes_AspNetUsers_ApplicationUserId",
                table: "Resumes");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_AspNetUsers_ApplicationUserId",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Skills_ApplicationUserId",
                table: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Resumes_ApplicationUserId",
                table: "Resumes");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ApplicationUserId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Experiences_ApplicationUserId",
                table: "Experiences");

            migrationBuilder.DropIndex(
                name: "IX_Educations_ApplicationUserId",
                table: "Educations");

            migrationBuilder.DropIndex(
                name: "IX_Certifications_ApplicationUserId",
                table: "Certifications");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Resumes");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Experiences");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Educations");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Certifications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Skills",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Resumes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Projects",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Experiences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Educations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Certifications",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_ApplicationUserId",
                table: "Skills",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Resumes_ApplicationUserId",
                table: "Resumes",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ApplicationUserId",
                table: "Projects",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Experiences_ApplicationUserId",
                table: "Experiences",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Educations_ApplicationUserId",
                table: "Educations",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Certifications_ApplicationUserId",
                table: "Certifications",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Certifications_AspNetUsers_ApplicationUserId",
                table: "Certifications",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Educations_AspNetUsers_ApplicationUserId",
                table: "Educations",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Experiences_AspNetUsers_ApplicationUserId",
                table: "Experiences",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AspNetUsers_ApplicationUserId",
                table: "Projects",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Resumes_AspNetUsers_ApplicationUserId",
                table: "Resumes",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_AspNetUsers_ApplicationUserId",
                table: "Skills",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
