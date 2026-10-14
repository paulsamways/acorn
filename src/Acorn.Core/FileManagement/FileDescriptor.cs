namespace Acorn.Core.FileManagement;

/// <summary>Describes an opened file stream and its response metadata.</summary>
/// <param name="Stream">The readable file stream.</param>
/// <param name="FileSize">The file size in bytes.</param>
/// <param name="FileName">The file's download name.</param>
/// <param name="MediaType">The validated media type.</param>
public record FileDescriptor(Stream Stream, long FileSize, string FileName, string MediaType);
