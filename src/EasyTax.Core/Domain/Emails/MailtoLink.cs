namespace EasyTax.Core.Domain.Emails;

/// <summary>Builds a <c>mailto:</c> link that opens the user's mail app with the message filled in (nothing is sent by the app).</summary>
public static class MailtoLink
{
	public static Uri Create(string to, string subject, string body)
	{
		// Mail apps expect CRLF line breaks in the body.
		var text = body.Replace("\r\n", "\n").Replace("\n", "\r\n");

		return new Uri($"mailto:{Escape(to.Trim()).Replace("%40", "@")}?subject={Escape(subject)}&body={Escape(text)}");
	}

	private static string Escape(string value) => Uri.EscapeDataString(value);
}
