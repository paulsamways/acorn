using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Acorn.Core.ContentManagement.Database.Entities;
using Acorn.Core.ContentManagement.Exceptions;
using Acorn.Core.ContentManagement.Models;
using Acorn.Core.Database;
using Acorn.Core.Email;
using Acorn.Core.Email.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Acorn.Core.ContentManagement.Services;

internal sealed class ContentCommentService : IContentCommentService
{
  private const int TokenByteLength = 32;

  private readonly ApplicationDbContext _dbContext;
  private readonly ContentCommentRenderer _renderer;
  private readonly IEmailService _emailService;
  private readonly ICommentUrlResolver _urlResolver;
  private readonly ContentCommentOptions _options;
  private readonly TimeProvider _timeProvider;

  public ContentCommentService(
    ApplicationDbContext dbContext,
    ContentCommentRenderer renderer,
    IEmailService emailService,
    ICommentUrlResolver urlResolver,
    IOptions<ContentCommentOptions> options,
    TimeProvider timeProvider)
  {
    _dbContext = dbContext;
    _renderer = renderer;
    _emailService = emailService;
    _urlResolver = urlResolver;
    _options = options.Value;
    _timeProvider = timeProvider;
  }

  public Task<bool> CanCommentAsync(Guid contentId, CancellationToken cancellationToken = default)
    => _dbContext.Content.AnyAsync(x => x.Id == contentId && x.PublishedRevisionId != null, cancellationToken);

  public async Task<ContentCommentSubmission> SubmitAsync(NewContentComment comment, CancellationToken cancellationToken = default)
  {
    var name = comment.AuthorName?.Trim() ?? string.Empty;
    var body = comment.Body?.Trim() ?? string.Empty;
    var email = NormaliseEmail(comment.AuthorEmail);

    if (name.Length is 0 or > ContentCommentLimits.AuthorNameMaxLength)
      throw new ArgumentException("The author name is required and must be 100 characters or fewer.", nameof(comment));
    if (body.Length is 0 or > ContentCommentLimits.BodyMaxLength)
      throw new ArgumentException("The comment body is required and must be 4000 characters or fewer.", nameof(comment));
    if (email is null)
      throw new ArgumentException("A valid email address is required.", nameof(comment));

    if (!await CanCommentAsync(comment.ContentId, cancellationToken))
      throw new ContentNotFoundException(comment.ContentId);

    var now = UtcNow();
    var (parentId, depth) = await ResolveParentAsync(comment, cancellationToken);

    var isTrusted = comment.TrustedEmail is not null
      && string.Equals(NormaliseEmail(comment.TrustedEmail), email, StringComparison.Ordinal);

    if (!isTrusted)
    {
      var since = now.AddHours(-1);
      var recentCount = await _dbContext.ContentComments
        .IgnoreQueryFilters()
        .CountAsync(x => x.AuthorEmail == email && x.CreatedAt >= since, cancellationToken);
      if (recentCount >= _options.MaxSubmissionsPerAddressPerHour)
        throw new CommentThrottledException();
    }

    var entity = new ContentCommentEntity
    {
      ContentId = comment.ContentId,
      ParentCommentId = parentId,
      Depth = depth,
      AuthorName = name,
      AuthorEmail = email,
      Body = body
    };

    string? token = null;
    if (isTrusted)
      entity.Verify(_options.ApprovalMode, now);
    else
      token = IssueToken(entity, now);

    _ = _dbContext.ContentComments.Add(entity);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    if (token is not null)
      await SendVerificationAsync(entity, token, cancellationToken);

    return new ContentCommentSubmission(entity.Id, entity.State);
  }

  public async Task<ContentCommentVerification> VerifyAsync(Guid commentId, string token, CancellationToken cancellationToken = default)
  {
    var entity = await _dbContext.ContentComments
      .FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);
    var now = UtcNow();

    if (entity is null
      || string.IsNullOrEmpty(token)
      || entity.State != ContentCommentState.Unverified
      || entity.VerificationTokenHash is null
      || entity.VerificationTokenExpiresAt is not { } expiresAt
      || expiresAt <= now
      || !CryptographicOperations.FixedTimeEquals(entity.VerificationTokenHash, HashToken(token)))
    {
      throw new InvalidVerificationTokenException(commentId);
    }

    entity.Verify(_options.ApprovalMode, now);

    try
    {
      _ = await _dbContext.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      // A concurrent request used the link first.
      throw new InvalidVerificationTokenException(commentId);
    }

    return new ContentCommentVerification(entity.Id, entity.ContentId, entity.AuthorName, entity.AuthorEmail, entity.State);
  }

  public async Task ResendVerificationAsync(Guid commentId, CancellationToken cancellationToken = default)
  {
    var entity = await _dbContext.ContentComments
      .FirstOrDefaultAsync(x => x.Id == commentId, cancellationToken);

    if (entity is null || entity.State != ContentCommentState.Unverified || entity.IsEmailAnonymised)
      return;

    var now = UtcNow();
    if (entity.VerificationSentAt is { } sentAt && sentAt + _options.ResendInterval > now)
      throw new CommentThrottledException();

    var token = IssueToken(entity, now);
    _ = await _dbContext.SaveChangesAsync(cancellationToken);

    await SendVerificationAsync(entity, token, cancellationToken);
  }

  public async Task<IReadOnlyList<ContentComment>> GetCommentsAsync(Guid contentId, CancellationToken cancellationToken = default)
  {
    var entities = await _dbContext.ContentComments
      .AsNoTracking()
      .Where(x => x.ContentId == contentId && x.State == ContentCommentState.Approved)
      .OrderBy(x => x.CreatedAt)
      .ThenBy(x => x.Id)
      .ToArrayAsync(cancellationToken);

    // Only comments reachable from an approved root are shown, so hiding a comment hides its replies.
    var childrenByParent = entities
      .Where(x => x.ParentCommentId is not null)
      .ToLookup(x => x.ParentCommentId!.Value);

    ContentComment Build(ContentCommentEntity entity)
      => ToModel(entity, [.. childrenByParent[entity.Id].Select(Build)]);

    return [.. entities.Where(x => x.ParentCommentId is null).Select(Build)];
  }

  public async Task<ContentComment> GetVisibleCommentAsync(Guid contentId, Guid commentId, CancellationToken cancellationToken = default)
  {
    var entity = await _dbContext.ContentComments
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == commentId && x.ContentId == contentId, cancellationToken);

    if (entity is null || !await IsVisibleAsync(entity, cancellationToken))
      throw new CommentNotFoundException(commentId);

    return ToModel(entity, []);
  }

  private async Task<(Guid? ParentId, int Depth)> ResolveParentAsync(NewContentComment comment, CancellationToken cancellationToken)
  {
    if (comment.ParentCommentId is not { } requestedParentId)
      return (null, 0);

    var parent = await _dbContext.ContentComments
      .AsNoTracking()
      .FirstOrDefaultAsync(x => x.Id == requestedParentId && x.ContentId == comment.ContentId, cancellationToken);

    if (parent is null || !await IsVisibleAsync(parent, cancellationToken))
      throw new CommentNotFoundException(requestedParentId);

    // Replies to a comment at the deepest level become its siblings.
    return parent.Depth >= _options.MaxDepth
      ? (parent.ParentCommentId, parent.Depth)
      : (parent.Id, parent.Depth + 1);
  }

  private async Task<bool> IsVisibleAsync(ContentCommentEntity comment, CancellationToken cancellationToken)
  {
    var current = comment;

    while (true)
    {
      if (current.State != ContentCommentState.Approved)
        return false;
      if (current.ParentCommentId is not { } parentId)
        return true;

      var parent = await _dbContext.ContentComments
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.Id == parentId, cancellationToken);
      if (parent is null)
        return false;

      current = parent;
    }
  }

  private string IssueToken(ContentCommentEntity entity, DateTime now)
  {
    var token = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenByteLength));

    entity.VerificationTokenHash = HashToken(token);
    entity.VerificationTokenExpiresAt = now + _options.VerificationTokenLifetime;
    entity.VerificationSentAt = now;

    return token;
  }

  private Task SendVerificationAsync(ContentCommentEntity entity, string token, CancellationToken cancellationToken)
    => _emailService.SendAsync(
      entity.AuthorEmail,
      entity.AuthorName,
      new CommentVerificationEmailMessage(_urlResolver.GetVerificationUrl(entity.Id, token)),
      cancellationToken);

  private ContentComment ToModel(ContentCommentEntity entity, IReadOnlyList<ContentComment> replies)
    => new(
      entity.Id,
      entity.ContentId,
      entity.ParentCommentId,
      entity.Depth,
      entity.AuthorName,
      _renderer.Render(entity.Body),
      new DateTimeOffset(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc)),
      replies);

  private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

  private static string? NormaliseEmail(string? email)
  {
    var trimmed = email?.Trim();
    if (string.IsNullOrEmpty(trimmed)
      || trimmed.Length > ContentCommentLimits.AuthorEmailMaxLength
      || !MailAddress.TryCreate(trimmed, out var address)
      || address.Address != trimmed)
    {
      return null;
    }

    return trimmed.ToLowerInvariant();
  }

  private static byte[] HashToken(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

  private static string Base64UrlEncode(byte[] bytes)
    => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
