using System.Globalization;
using EasyTax.App.Localization;
using EasyTax.App.Shell;

namespace EasyTax.App.Clients;

/// <summary>The "delete this client?" confirmation, shared by the clients list and the client page.</summary>
internal static class ClientDeleteDialog
{
	/// <summary>
	/// Asks the user to confirm. The client's name is on its own bold line. Records may have to be kept for a legal retention
	/// period, so the dialog says so and suggests suspending instead (and, for a suspended client, since when). True when they confirmed.
	/// </summary>
	public static Task<bool> ConfirmAsync(IDialogService dialogs, ILocalizer localizer, string clientName, DateTime? suspendedAt)
	{
		var message = new List<DialogText>
		{
			new(localizer["List_Delete_Question"]),
			new(clientName, IsBold: true),
			new(localizer["List_Delete_Warning"]),
			new(localizer["List_Delete_Retention"]),
		};

		if (suspendedAt is { } since)
		{
			var date = DateOnly.FromDateTime(since.ToLocalTime()).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
			message.Add(new DialogText(string.Format(CultureInfo.CurrentCulture, localizer["List_Delete_SuspendedSince"], date), IsBold: true));
		}

		return dialogs.ConfirmAsync(localizer["List_Delete_Title"], message, localizer["List_Delete_Confirm"], localizer["Dialog_Cancel"], destructive: true);
	}
}
