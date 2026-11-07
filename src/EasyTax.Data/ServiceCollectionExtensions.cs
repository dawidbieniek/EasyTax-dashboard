using EasyTax.Core.Database;
using EasyTax.Core.Domain.Banks;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;
using EasyTax.Core.Domain.Years;
using EasyTax.Data.Banks;
using EasyTax.Data.Clients;
using EasyTax.Data.Months;
using EasyTax.Data.Years;
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
			services.AddSingleton<IBankRepository, BankRepository>();
			services.AddSingleton<IMonthRecordRepository, MonthRecordRepository>();
			services.AddSingleton<IAccountingYearRepository, AccountingYearRepository>();

			return services;
		}
	}
}
