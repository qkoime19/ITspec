using Microsoft.EntityFrameworkCore.Migrations;
using SqlCode = ITspec.Api.Model.Data.Sql.SqlObjects;

#nullable disable

namespace ITspec.Api.Model.Data.Migrations
{
    /// <summary>
    /// Втората миграция: табличният тип, процедурите и изгледите.
    /// EF я генерира празна (тези обекти не са част от модела), а методите са попълнени на ръка.
    /// SQL кодът е в Model/Data/Sql/SqlObjects.cs.
    /// </summary>
    public partial class SqlObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // тип -> 3 процедури -> 2 изгледа
            SqlCode.Create(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // обратен ред: изгледи -> процедури -> тип
            SqlCode.Drop(migrationBuilder);
        }
    }
}
