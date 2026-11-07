using System.Net.Mail;

namespace EasyTax.Core.Domain.Clients;

/// <summary>Validation rules for client data. Phone and email are optional: empty is valid.</summary>
public static class ClientValidation
{
	private const int NipLength = 10;
	private const int MinPhoneDigits = 9;
	private const int MaxPhoneDigits = 15;

	private static readonly int[] NipWeights = [6, 5, 7, 2, 3, 4, 5, 6, 7];

	/// <summary>Strips everything but digits, so "123-456-32-18" and "1234563218" compare equal.</summary>
	public static string NormalizeNip(string? nip) => new([.. (nip ?? string.Empty).Where(char.IsDigit)]);

	/// <summary>Polish NIP: 10 digits whose last digit is the weighted checksum of the first nine (mod 11, never 10).</summary>
	public static bool IsValidNip(string? nip)
	{
		var digits = NormalizeNip(nip);
		if (digits.Length != NipLength)
			return false;

		var checksum = digits.Zip(NipWeights, (digit, weight) => (digit - '0') * weight).Sum() % 11;

		return checksum != 10 && checksum == digits[^1] - '0';
	}

	public static bool IsValidPhone(string? phone)
	{
		if (string.IsNullOrWhiteSpace(phone))
			return true;

		var digitCount = phone.Count(char.IsDigit);
		var onlyPhoneCharacters = phone.All(c => char.IsDigit(c) || " +-()".Contains(c));

		return onlyPhoneCharacters && digitCount is >= MinPhoneDigits and <= MaxPhoneDigits;
	}

	public static bool IsValidEmail(string? email)
	{
		if (string.IsNullOrWhiteSpace(email))
			return true;

		return MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim();
	}
}
