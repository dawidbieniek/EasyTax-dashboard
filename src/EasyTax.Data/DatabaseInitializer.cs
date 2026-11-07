using EasyTax.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace EasyTax.Data;

internal sealed class DatabaseInitializer(IDbContextFactory<EasyTaxDbContext> contextFactory, EasyTaxDataOptions options) : IDatabaseInitializer
{
	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		var directory = Path.GetDirectoryName(options.DatabasePath);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

		// TODO(M1): replace with context.Database.MigrateAsync() once the model is stable and the first migration exists.
		await context.Database.EnsureCreatedAsync(cancellationToken);
	}
}
