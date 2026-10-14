namespace Acorn.Core.ContentManagement;

/// <summary>Specifies who may access a content attachment.</summary>
public enum AttachmentVisibility
{
  /// <summary>Anyone may access the attachment when its content is published.</summary>
  Public,

  /// <summary>Authenticated users may access the attachment when its content is published.</summary>
  Authenticated,

  /// <summary>Only the attachment owner may access the attachment.</summary>
  Owner
}
