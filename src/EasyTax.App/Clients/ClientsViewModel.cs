using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using EasyTax.App.Localization;
using EasyTax.App.Shell;
using EasyTax.Core.Clients;

namespace EasyTax.App.Clients;

public sealed partial class ClientsViewModel(IClientRepository clients, ILocalizer localizer) : ViewModelBase
{
	[ObservableProperty]
	public partial string Summary { get; set; } = string.Empty;

	public override async Task OnNavigatedToAsync(object? parameter)
	{
		var count = await clients.CountAsync();
		Summary = string.Format(CultureInfo.CurrentCulture, localizer["Clients_Count"], count);
	}
}
