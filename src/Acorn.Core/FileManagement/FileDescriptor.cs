namespace Acorn.Core.FileManagement;

public record FileDescriptor(Stream Stream, long FileSize, string FileName, string MediaType);
