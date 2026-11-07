namespace EasyTax.App.Shell;

/// <summary>Maps a view model type to the page type that displays it.</summary>
public interface IPageRegistry
{
	Type GetPageType(Type viewModelType);
}
