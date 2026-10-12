# TODO

## Derivatives

```c#
    ArgumentNullException.ThrowIfNull(parentContent);

    MemoryStream? output = null;
    var outputOwnershipTransferred = false;
    try
    {
      parentContent.Position = 0;
      using var image = await Image.LoadAsync(parentContent, cancellationToken);
      image.Mutate(processingContext => processingContext.AutoOrient());

      var maximumDimension = Math.Max(image.Width, image.Height);
      if (maximumDimension > 1200)
      {
        var scale = 1200d / maximumDimension;
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));
        image.Mutate(processingContext => processingContext.Resize(width, height));
      }

      image.Metadata.ExifProfile = null;
      image.Metadata.XmpProfile = null;
      image.Metadata.IptcProfile = null;

      output = new MemoryStream();
      await image.SaveAsWebpAsync(output, new WebpEncoder { Quality = 82 }, cancellationToken);
      output.Position = 0;
      outputOwnershipTransferred = true;
      return output;
    }
    finally
    {
      parentContent.Position = 0;
      if (!outputOwnershipTransferred)
        output?.Dispose();
    }
```
