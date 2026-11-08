using System.Security.Cryptography;
using System.Text;

namespace EasyTax.App.Security;

/// <summary>
/// The database key: one random key per install. It is wrapped with Windows DPAPI (this Windows user only) and stored next
/// to the database, so the app opens the database without asking. The same key is shown to the user once as a recovery key.
/// </summary>
public sealed class DatabaseKeyService(string keyFilePath)
{
	// Crockford base32: no I, L, O or U, so a key written on paper cannot be misread.
	private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
	private const int KeyBytes = 20;
	private const int KeyLength = KeyBytes * 8 / 5;
	private const int GroupSize = 4;

	// Binds the wrapped key to this app: another program using DPAPI as the same user cannot unwrap it by accident.
	private static readonly byte[] Entropy = "EasyTax.DatabaseKey.v1"u8.ToArray();

	/// <summary>True when a wrapped key file exists (it may still be unreadable, for example after a Windows profile change).</summary>
	public bool HasStoredKey => File.Exists(keyFilePath);

	/// <summary>Creates a new random key. It is not kept anywhere until <see cref="Store"/> is called.</summary>
	public static string Generate()
	{
		var bytes = RandomNumberGenerator.GetBytes(KeyBytes);

		var key = new StringBuilder(KeyLength);
		var buffer = 0;
		var bits = 0;
		foreach (var value in bytes)
		{
			buffer = (buffer << 8) | value;
			bits += 8;
			while (bits >= 5)
			{
				bits -= 5;
				key.Append(Alphabet[(buffer >> bits) & 31]);
			}
		}

		return key.ToString();
	}

	/// <summary>Reads and unwraps the stored key. Null when there is none, or when this Windows user cannot unwrap it.</summary>
	public string? TryLoad()
	{
		if (!File.Exists(keyFilePath))
			return null;

		try
		{
			var wrapped = File.ReadAllBytes(keyFilePath);
			var bytes = ProtectedData.Unprotect(wrapped, Entropy, DataProtectionScope.CurrentUser);

			return Encoding.UTF8.GetString(bytes);
		}
		catch (CryptographicException)
		{
			return null;
		}
	}

	/// <summary>Wraps the key with DPAPI and saves it, replacing an existing key file.</summary>
	public void Store(string key)
	{
		var wrapped = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, DataProtectionScope.CurrentUser);

		var directory = Path.GetDirectoryName(keyFilePath);
		if (!string.IsNullOrEmpty(directory))
			Directory.CreateDirectory(directory);

		// Written to a temporary file first so a crash can never leave half a key behind.
		var temporary = keyFilePath + ".tmp";
		File.WriteAllBytes(temporary, wrapped);
		File.Move(temporary, keyFilePath, overwrite: true);
	}

	/// <summary>The key as shown to people: groups of four, for example <c>A1B2-C3D4-...</c>.</summary>
	public static string Format(string key)
		=> string.Join('-', key.Chunk(GroupSize).Select(group => new string(group)));

	/// <summary>Reads a key typed by a person: ignores case, dashes and spaces and fixes look-alike letters (O to 0, I and L to 1).</summary>
	public static bool TryNormalize(string? input, out string key)
	{
		key = string.Empty;
		if (string.IsNullOrWhiteSpace(input))
			return false;

		var normalized = new StringBuilder(KeyLength);
		foreach (var character in input.ToUpperInvariant())
		{
			if (character is '-' or ' ')
				continue;

			normalized.Append(character switch
			{
				'O' => '0',
				'I' or 'L' => '1',
				_ => character,
			});
		}

		if (normalized.Length != KeyLength || normalized.ToString().Any(c => !Alphabet.Contains(c, StringComparison.Ordinal)))
			return false;

		key = normalized.ToString();

		return true;
	}
}
