namespace Acorn.Core.Email;

public abstract class EmailMessage
{
  public abstract string GetSubject();

  public abstract string GetBodyPlainText();
}
