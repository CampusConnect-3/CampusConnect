using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusConnect.Migrations.Tables
{
    /// <inheritdoc />
    public partial class TablesInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category",
                columns: table => new
                {
                    categoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    categoryName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_category", x => x.categoryID);
                });

            migrationBuilder.CreateTable(
                name: "requestStatus",
                columns: table => new
                {
                    statusID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    statusName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requestStatus", x => x.statusID);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    roleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    roleName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.roleID);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    lName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    password = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    department = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.userID);
                });

            migrationBuilder.CreateTable(
                name: "request",
                columns: table => new
                {
                    requestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    assigned_to = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    categoryID = table.Column<int>(type: "int", nullable: false),
                    priority = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    statusID = table.Column<int>(type: "int", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    closedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    buildingName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    roomNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    phoneNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request", x => x.requestID);
                    table.ForeignKey(
                        name: "FK_request_category_categoryID",
                        column: x => x.categoryID,
                        principalTable: "category",
                        principalColumn: "categoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_requestStatus_statusID",
                        column: x => x.statusID,
                        principalTable: "requestStatus",
                        principalColumn: "statusID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_request_users_assigned_to",
                        column: x => x.assigned_to,
                        principalTable: "users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_request_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "userRoles",
                columns: table => new
                {
                    roleID = table.Column<int>(type: "int", nullable: false),
                    userID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userRoles", x => new { x.roleID, x.userID });
                    table.ForeignKey(
                        name: "FK_userRoles_roles_roleID",
                        column: x => x.roleID,
                        principalTable: "roles",
                        principalColumn: "roleID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_userRoles_users_userID",
                        column: x => x.userID,
                        principalTable: "users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    fileID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    requestID = table.Column<int>(type: "int", nullable: false),
                    creatorID = table.Column<int>(type: "int", nullable: false),
                    fileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    contentType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    fileUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    uploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.fileID);
                    table.ForeignKey(
                        name: "FK_attachments_request_requestID",
                        column: x => x.requestID,
                        principalTable: "request",
                        principalColumn: "requestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_attachments_users_creatorID",
                        column: x => x.creatorID,
                        principalTable: "users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "requestComments",
                columns: table => new
                {
                    commentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    requestID = table.Column<int>(type: "int", nullable: false),
                    commentText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    creatorID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requestComments", x => x.commentID);
                    table.ForeignKey(
                        name: "FK_requestComments_request_requestID",
                        column: x => x.requestID,
                        principalTable: "request",
                        principalColumn: "requestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_requestComments_users_creatorID",
                        column: x => x.creatorID,
                        principalTable: "users",
                        principalColumn: "userID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_attachments_creatorID",
                table: "attachments",
                column: "creatorID");

            migrationBuilder.CreateIndex(
                name: "IX_attachments_requestID",
                table: "attachments",
                column: "requestID");

            migrationBuilder.CreateIndex(
                name: "IX_request_assigned_to",
                table: "request",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "IX_request_categoryID",
                table: "request",
                column: "categoryID");

            migrationBuilder.CreateIndex(
                name: "IX_request_created_by",
                table: "request",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_request_statusID",
                table: "request",
                column: "statusID");

            migrationBuilder.CreateIndex(
                name: "IX_requestComments_creatorID",
                table: "requestComments",
                column: "creatorID");

            migrationBuilder.CreateIndex(
                name: "IX_requestComments_requestID",
                table: "requestComments",
                column: "requestID");

            migrationBuilder.CreateIndex(
                name: "IX_userRoles_userID",
                table: "userRoles",
                column: "userID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attachments");

            migrationBuilder.DropTable(
                name: "requestComments");

            migrationBuilder.DropTable(
                name: "userRoles");

            migrationBuilder.DropTable(
                name: "request");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "category");

            migrationBuilder.DropTable(
                name: "requestStatus");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
