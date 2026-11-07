using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EasyTax.Data;

/// <summary>Used only by the EF command-line tools (<c>dotnet ef migrations add</c>); the app never creates a context this way.</summary>
internal sealed class EasyTaxDbContextFactory : IDesignTimeDbContextFactory<EasyTaxDbContext>
{
	public EasyTaxDbContext CreateDbContext(string[] args)
		=> new(new DbContextOptionsBuilder<EasyTaxDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
