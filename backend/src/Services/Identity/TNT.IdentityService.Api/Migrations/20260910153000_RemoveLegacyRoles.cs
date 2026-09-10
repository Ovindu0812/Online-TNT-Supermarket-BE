using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TNT.IdentityService.Api.Data;

#nullable disable

namespace TNT.IdentityService.Api.Migrations;

[DbContext(typeof(IdentityDbContext))]
[Migration("20260910153000_RemoveLegacyRoles")]
public partial class RemoveLegacyRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE \"ApplicationUsers\" SET \"Role\" = 'Buyer' " +
            "WHERE \"Role\" IN ('Seller', 'Manager');");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The previous role cannot be inferred after being converted to Buyer.
    }
}
