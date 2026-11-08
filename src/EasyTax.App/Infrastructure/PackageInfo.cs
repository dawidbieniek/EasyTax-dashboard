using System.Runtime.InteropServices;
using Windows.ApplicationModel;

namespace EasyTax.App.Infrastructure;

/// <summary>Whether the app runs from the installed MSIX package or unpackaged (development).</summary>
internal static class PackageInfo
{
	/// <summary>True when the app has a package identity (installed from the MSIX).</summary>
	public static bool IsPackaged { get; } = DetectPackaged();

	private static bool DetectPackaged()
	{
		try
		{
			// Throws when the process has no package identity.
			_ = Package.Current.Id;

			return true;
		}
		catch (Exception ex) when (ex is InvalidOperationException or COMException)
		{
			return false;
		}
	}
}
