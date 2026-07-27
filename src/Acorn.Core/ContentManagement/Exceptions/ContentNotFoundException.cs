namespace Acorn.Core.ContentManagement.Exceptions;

public class ContentNotFoundException : Exception
{
  public ContentNotFoundException(int id)
    : this(id, $"Content with Id={id} was not found")
  {
  }
  public ContentNotFoundException(int id, string message) : this(id, message, null) { }
  public ContentNotFoundException(int id, string message, Exception? inner) : base(message, inner)
  {
    Id = id;
  }

  public int Id { get; private set; }
}
