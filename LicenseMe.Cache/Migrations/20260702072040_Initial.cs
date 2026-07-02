using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LicenseMe.Cache.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OsiLicense",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SpdxId = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmissionUrl = table.Column<string>(type: "TEXT", nullable: true),
                    SubmitterName = table.Column<string>(type: "TEXT", nullable: true),
                    Approved = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApprovalDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LicenseStewardVersion = table.Column<string>(type: "TEXT", nullable: true),
                    LicenseStewardUrl = table.Column<string>(type: "TEXT", nullable: true),
                    BoardMinutes = table.Column<string>(type: "TEXT", nullable: true),
                    Stewards = table.Column<string>(type: "TEXT", nullable: false),
                    Keywords = table.Column<string>(type: "TEXT", nullable: false),
                    Links_Self_Href = table.Column<string>(type: "TEXT", nullable: false),
                    Links_Html_Href = table.Column<string>(type: "TEXT", nullable: false),
                    Links_Collection_Href = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsiLicense", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LicenseText",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    LicenseText = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicenseText", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicenseText_OsiLicense_Id",
                        column: x => x.Id,
                        principalTable: "OsiLicense",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OsiLicenseTimestamp",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsiLicenseTimestamp", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OsiLicenseTimestamp_OsiLicense_Id",
                        column: x => x.Id,
                        principalTable: "OsiLicense",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicenseText");

            migrationBuilder.DropTable(
                name: "OsiLicenseTimestamp");

            migrationBuilder.DropTable(
                name: "OsiLicense");
        }
    }
}
