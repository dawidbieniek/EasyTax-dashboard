using EasyTax.App.Localization;
using EasyTax.App.Shell;

namespace EasyTax.App.Clients;

/// <summary>The "delete this client?" confirmation, shared by the clients list and the client page.</summary>
internal static class ClientDeleteDialog
{
	/// <summary>Asks the user to confirm. The client's name is on its own bold line. True when they confirmed.</summary>
	public static Task<bool> ConfirmAsync(IDialogService dialogs, ILocalizer localizer, string clientName)
		=> dialogs.ConfirmAsync(
			localizer["List_Delete_Title"],
			[
				new DialogText(localizer["List_Delete_Question"]),
				new DialogText(clientName, IsBold: true),
				new DialogText(localizer["List_Delete_Warning"]),
			],
			localizer["List_Delete_Confirm"],
			localizer["Dialog_Cancel"]);
}
