using EasyTax.Core.Common;

namespace EasyTax.Core.Domain.Emails;

/// <summary>The text of one email, with <c>{tokens}</c> filled in per client (see <see cref="EmailTemplateRenderer"/>). Editable by the user.</summary>
public class EmailTemplate : Entity
{
	/// <summary>Which alert the template is for: <see cref="EmailTemplateSlot.Key"/>. Unique.</summary>
	public required string Key { get; set; }

	public required string Subject { get; set; }

	public required string Body { get; set; }
}
