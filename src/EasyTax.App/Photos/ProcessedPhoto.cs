namespace EasyTax.App.Photos;

/// <summary>A picked picture, already cropped to a square and shrunk, ready to store.</summary>
public sealed record ProcessedPhoto(byte[] Bytes, string ContentType);
