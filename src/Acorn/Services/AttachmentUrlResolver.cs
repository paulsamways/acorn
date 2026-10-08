using Acorn.Core.ContentManagement;
using Microsoft.AspNetCore.WebUtilities;

namespace Acorn.Services;

internal sealed class AttachmentUrlResolver(LinkGenerator linkGenerator) : IAttachmentUrlResolver
{
  private const string DefaultImageRecipe = "web-1200";

  public string ResolveUrl(Guid contentPublicId, Guid filePublicId)
  {
    var path = linkGenerator.GetPathByName(
      Routes.FileDerivativeGetRoute,
      new { publicId = filePublicId, recipe = DefaultImageRecipe });
    if (path is null)
      throw new InvalidOperationException($"The file derivative route '{Routes.FileDerivativeGetRoute}' is not registered.");

    return QueryHelpers.AddQueryString(path, "contentPublicId", contentPublicId.ToString("D"));
  }
}
