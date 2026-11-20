using System.Globalization;
using EasyTax.App.Localization;
using EasyTax.App.Shell;
using EasyTax.Core.Domain.Alerts;
using EasyTax.Core.Domain.Emails;
using Microsoft.Extensions.Logging;
using Windows.System;

namespace EasyTax.App.Alerts;

internal sealed class MailService(
	IEmailTemplateRepository templates,
	IAlertService alerts,
	IDialogService dialogs,
	IToastService toasts,
	ILocalizer localizer,
	ILogger<MailService> logger) : IMailService
{
	public async Task WriteAsync(ClientAlert alert)
	{
		if (string.IsNullOrWhiteSpace(alert.Client.Email))
		{
			toasts.Warning(localizer["Mail_NoEmail"]);
			return;
		}

		var template = await templates.GetAsync(EmailTemplateSlot.For(alert));
		if (template is null)
		{
			toasts.Error(localizer["Mail_NoTemplate"]);
			return;
		}

		var link = MailtoLink.Create(
			alert.Client.Email,
			EmailTemplateRenderer.Render(template.Subject, alert),
			EmailTemplateRenderer.Render(template.Body, alert));

		if (!await TryOpenAsync(link))
		{
			toasts.Error(localizer["Mail_OpenFailed"]);
			return;
		}

		await OfferToMarkHandledAsync(alert);
	}

	private async Task<bool> TryOpenAsync(Uri link)
	{
		try
		{
			return await Launcher.LaunchUriAsync(link);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Could not open the mail app");
			return false;
		}
	}

	private async Task OfferToMarkHandledAsync(ClientAlert alert)
	{
		var alreadyHandled = alerts.Items.Any(item => item.IsHandled && item.Alert.Client.Id == alert.Client.Id && item.Alert.Kind == alert.Kind && item.Alert.Year == alert.Year);
		if (alreadyHandled)
			return;

		var confirmed = await dialogs.ConfirmAsync(
			localizer["Mail_Done_Title"],
			[new DialogText(string.Format(CultureInfo.CurrentCulture, localizer["Mail_Done_Message"], alert.Client.FullName))],
			localizer["Mail_Done_Yes"],
			localizer["Mail_Done_No"]);

		if (confirmed)
			await alerts.AcknowledgeAsync(alert);
	}
}
