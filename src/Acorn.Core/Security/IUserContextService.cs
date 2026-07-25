namespace Acorn.Core.Security;

public interface IUserContextService
{
  Guid GetCurrentUserId();

  bool IsAuthenticated();
}
