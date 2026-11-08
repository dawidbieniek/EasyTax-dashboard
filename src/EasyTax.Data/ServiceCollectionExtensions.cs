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
			// The native SQLCipher library has to be registered once before the first connection.
			SQLitePCL.Batteries_V2.Init();

			services.AddSingleton(options);

			// The key is read when the first context is created, so the app can set it after the host is built.
			services.AddDbContextFactory<EasyTaxDbContext>((provider, builder) => builder.UseSqlite(provider.GetRequiredService<EasyTaxDataOptions>().ConnectionString()));
			services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
			services.AddSingleton<IClientRepository, ClientRepository>();
			services.AddSingleton<IClientPhotoRepository, ClientPhotoRepository>();
			services.AddSingleton<IBankRepository, BankRepository>();
			services.AddSingleton<IMonthRecordRepository, MonthRecordRepository>();
			services.AddSingleton<IAccountingYearRepository, AccountingYearRepository>();

			return services;
		}
	}
}
