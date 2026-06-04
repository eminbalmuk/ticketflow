using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ticketflow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCategoriesAndSupportAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Tickets",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "SupportCategoryAssignments",
                columns: table => new
                {
                    SupportUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportCategoryAssignments", x => new { x.SupportUserId, x.Category });
                    table.ForeignKey(
                        name: "FK_SupportCategoryAssignments_AspNetUsers_SupportUserId",
                        column: x => x.SupportUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportCategoryAssignments_Category",
                table: "SupportCategoryAssignments",
                column: "Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportCategoryAssignments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Tickets");
        }
    }
}
