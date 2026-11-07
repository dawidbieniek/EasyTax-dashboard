#if DEBUG
using EasyTax.Core.Domain.Banks;
using EasyTax.Core.Domain.Clients;
using EasyTax.Core.Domain.Months;

namespace EasyTax.App.Infrastructure;

/// <summary>
/// Development only (compiled out of Release builds): fills an empty database with a few sample clients and
/// 2026 month records, so screens have something to show. Run the app with <c>--seed-demo</c>.
/// </summary>
internal sealed class DemoDataSeeder(IClientRepository clients, IBankRepository banks, IMonthRecordRepository months)
{
	private static readonly int[] NipWeights = [6, 5, 7, 2, 3, 4, 5, 6, 7];

	public async Task SeedAsync()
	{
		if (await clients.CountAsync() > 0)
			return;

		var bankIds = (await banks.GetAllAsync()).ToDictionary(b => b.Name, b => b.Id);

		var jan = await AddAsync(new Client
		{
			FirstName = "Jan",
			LastName = "Kowalski",
			Nip = MakeNip(525224848),
			JoinDate = new DateOnly(2025, 1, 14),
			BankId = bankIds["mBank"],
			PhoneNumber = "123 456 789",
			Email = "jan.kowalski@example.com",
			TaxType = TaxType.Scale,
			IsVatPayer = true,
			VatPaymentFrequency = PaymentFrequency.Quarterly,
			UsesCashRegister = true,
			FirstCashRegisterDate = new DateOnly(2025, 3, 6),
			PlanName = "Premium",
			AuthorizationFee = 124.30m,
			SubscriptionFee = 99.99m,
			Notes = "Preferuje kontakt mailowy.",
			VatRDoneAt = DateTime.Now.AddMonths(-10),
			ZusDoneAt = DateTime.Now.AddDays(-12),
		});
		await AddMonthsAsync(jan, [(3000, 6, 7), (4000, 2, 4), (5000, 10, 9), (4500, 6, 2), (2600, 2, 9)], accountedMonths: 4);

		var anna = await AddAsync(new Client
		{
			FirstName = "Anna",
			LastName = "Nowak",
			Gender = Gender.Female,
			Nip = MakeNip(111222333),
			JoinDate = new DateOnly(2024, 6, 3),
			BankId = bankIds["PKO Bank Polski"],
			PhoneNumber = "600 100 200",
			Email = "anna.nowak@example.com",
			TaxType = TaxType.LumpSum,
			ReliefType = ReliefType.ZusPlus,
			ReliefSinceDate = new DateOnly(2025, 2, 1),
		});
		await AddMonthsAsync(anna, [(19500, 8, 3), (19500, 9, 4), (19500, 7, 2), (19500, 10, 5), (19500, 6, 1), (19500, 9, 3)], accountedMonths: 3);

		var maria = await AddAsync(new Client
		{
			FirstName = "Maria",
			LastName = "Wiśniewska",
			Gender = Gender.Female,
			Nip = MakeNip(222333444),
			JoinDate = new DateOnly(2026, 3, 10),
			BankId = bankIds["ING Bank Śląski"],
			TaxType = TaxType.Linear,
			ReliefType = ReliefType.FullZus,
		});
		await AddMonthsAsync(maria, [], accountedMonths: 0, firstMonth: 3);

		await AddAsync(new Client
		{
			FirstName = "Tomasz",
			LastName = "Lewandowski",
			Nip = MakeNip(444555666),
			JoinDate = new DateOnly(2022, 9, 15),
			BankId = bankIds["Santander Bank Polska"],
			TaxType = TaxType.Scale,
			ReliefType = ReliefType.PreferentialZus,
			IsVatPayer = true,
		});

		await AddAsync(new Client
		{
			FirstName = "Piotr",
			LastName = "Zieliński",
			Nip = MakeNip(555666777),
			JoinDate = new DateOnly(2023, 2, 1),
			BankId = bankIds["ING Bank Śląski"],
			TaxType = TaxType.Linear,
			ReliefType = ReliefType.FullZus,
			IsSuspended = true,
		});
	}

	private async Task<Client> AddAsync(Client client)
	{
		await clients.AddAsync(client);
		return client;
	}

	private async Task AddMonthsAsync(Client client, (decimal Income, int Sales, int Cost)[] values, int accountedMonths, int firstMonth = 1)
	{
		for (var i = 0; i < values.Length; i++)
		{
			var month = firstMonth + i;
			await months.SaveAsync(new MonthRecord
			{
				ClientId = client.Id,
				Month = new DateOnly(2026, month, 1),
				Income = values[i].Income,
				SalesInvoices = values[i].Sales,
				CostInvoices = values[i].Cost,
				IsAccounted = i < accountedMonths,
			});
		}
	}

	/// <summary>Builds a valid NIP from a nine-digit seed, moving to the next seed when the checksum would be 10.</summary>
	private static string MakeNip(int seed)
	{
		for (var candidate = seed; ; candidate++)
		{
			var nine = candidate.ToString("D9");
			var checksum = nine.Zip(NipWeights, (digit, weight) => (digit - '0') * weight).Sum() % 11;
			if (checksum != 10)
				return nine + checksum;
		}
	}
}
#endif
