namespace Acorn.Core.Data.Entities;

internal abstract class File
{
  public int Id { get; set; }

  public Guid PublicId { get; private set; } = Guid.NewGuid();

  public required byte[] BlobDigest { get; set; }

  public Blob Blob { get; set; } = null!;

  public required Guid OwnerId { get; set; }

  public User Owner { get; set; } = null!;

  public required string OriginalFilename { get; set; }

  public required string DeclaredMediaType { get; set; }

  public required string ValidatedMediaType { get; set; }

  public DateTime CreatedAt { get; set; }

  public DateTime? DeletedAt { get; set; }

  public int? ParentFileId { get; set; }

  public File? ParentFile { get; set; }

  public List<File> DerivedFiles { get; set; } = [];

  public string? Recipe { get; set; }

  public int? RecipeVersion { get; set; }

  public List<ContentAttachment> Attachments { get; set; } = [];
}
