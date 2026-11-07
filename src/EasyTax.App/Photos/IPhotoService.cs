namespace EasyTax.App.Photos;

public interface IPhotoService
{
	/// <summary>Lets the user choose a picture and returns it cropped to a square and shrunk; null when they cancel or the file is not a usable picture.</summary>
	Task<ProcessedPhoto?> PickAsync();
}
