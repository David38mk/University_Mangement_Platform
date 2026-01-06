using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace University_Management_Platform.Migrations
{
    /// <inheritdoc />
    public partial class TeacherExp1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId",
                table: "Teachers",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IdentityUserId",
                table: "Students",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "IdentityUserId",
                table: "Students");
        }
    }
}
