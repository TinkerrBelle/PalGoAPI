using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PalGoAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationCode",
                table: "AspNetUsers",
                type: "NVARCHAR2(2000)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailVerificationExpiry",
                table: "AspNetUsers",
                type: "TIMESTAMP(7)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IsEmailVerified",
                table: "AspNetUsers",
                type: "NUMBER(10)",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailVerificationExpiry",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "AspNetUsers");
        }
    }
}
