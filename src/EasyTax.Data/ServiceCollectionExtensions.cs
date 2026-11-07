using EasyTax.Core.Clients;
using EasyTax.Core.Database;
using EasyTax.Data.Clients;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EasyTax.Data;

public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		/// <summary>Registers the DbContext factory, repositories and the database initializer.</summary>
		public IServiceCollection AddEasyTaxData(EasyTaxDataOptions options)
		{
			var connectionString = new SqliteConnectionStringBuilder { DataSource = options.DatabasePath }.ToString();

			services.AddSingleton(options);
			services.AddDbContextFactory<EasyTaxDbContext>(builder => builder.UseSqlite(connectionString));
			services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
			services.AddSingleton<IClientRepository, ClientRepository>();

			return services;
		}
	}
}
