# Content Comments Implementation Plan

> **Status:** Implemented

## Goals

- Visitors can comment on published content (posts, bookmarks, notes).
- Comments are written in basic Markdown.
- Each comment has an author name and an email address. The email is private and never rendered or exposed publicly.
- Spam is limited by requiring email verification before a comment can progress.
- Comments are hierarchical: a comment can be a reply to another comment.
- A setting controls whether verified comments are approved automatically or require admin approval.

## Decision

- Comments attach to the stable **content ID**, not a revision, so they survive edits and republishing.
- Commenting is allowed only while the content has a `PublishedRevisionId` and is not soft-deleted.
- Comment lifecycle is a state machine on the comment row.
- Verification uses a single-use, expiring token sent by email. Only a hash of the token is stored.
- A "don't ask me again" cookie lets a returning commenter skip verification for future comments.
- Approval mode is an application setting: `AutoApprove` or `AdminApproval`.

## State model

| State | Meaning | Public? |
|---|---|---|
| `Unverified` | Submitted; email ownership not yet proven. | No |
| `Verified` | Author proved ownership of the email. Awaiting approval (admin mode only). | No |
| `Approved` | Visible to readers. | **Yes** |
| `Hidden` | Hidden by an admin (spam, abuse, retraction). | No |

Transitions:

- `Unverified → Verified`: author follows the verification link, or the trusted cookie applies.
- `Verified → Approved`: automatically on verification when the mode is `AutoApprove`; otherwise an admin action.
- `Verified → Hidden`, `Approved → Hidden`, `Unverified → Hidden`: admin action.
- `Hidden → Approved`: admin action (un-hide).
- No other transitions. Enforce them in one place (a method on the entity or service) and throw on invalid transitions.
- Record a timestamp per transition (`VerifiedAt`, `ApprovedAt`, `HiddenAt`).
- In `AutoApprove` mode, `Verified` is still recorded (`VerifiedAt` set) and the comment moves to `Approved` in the same operation.

### Approval setting

- Add comment options bound from configuration (e.g. `Comments:ApprovalMode`, values `Auto` / `Admin`), defaulting to `Admin`.
- The mode is read at the moment of verification. Changing it affects only future verifications and does not retroactively approve comments already `Verified`.
- Optionally expose the setting in the admin UI later; configuration is sufficient for the first iteration.

## Hierarchy (threading)

- `ParentCommentId` nullable self-referencing FK on the comment.
- A reply must belong to the same content as its parent. Validate in the service and enforce with a composite FK `(ContentId, ParentCommentId) → (ContentId, Id)` (requires a unique index on `(ContentId, Id)`).
- Use `DeleteBehavior.Restrict`. Comments are hidden rather than deleted, so threads are not broken.
- Replies may only target a parent that is `Approved`. Replying to an unapproved or hidden comment is rejected.
- Visibility rule: a comment is shown publicly only if it is `Approved` **and** every ancestor is `Approved`. Hiding a comment therefore hides its entire subtree.
- Depth: store a `Depth` column (root = 0) and cap it (e.g. 5). Replies beyond the cap attach to the deepest allowed ancestor, which keeps rendering and queries bounded.
- Querying: load all approved comments for a content item in one query ordered by `CreatedAt`, then assemble the tree in memory. Per-content volumes are expected to be small, so no recursive CTE is needed initially.
- Rendering: a recursive partial with indentation limited by the depth cap. Each approved comment has a "Reply" link that targets the form at that comment.
- Ordering: roots in chronological order, replies chronological under their parent.

## Data model

New `ContentCommentEntity` in `Acorn.Core/ContentManagement/Database/Entities/`, with an entity type configuration and migration, following existing conventions (`IAuditable`, UUIDv7 ID, `IConcurrencyTracked`).

| Column | Notes |
|---|---|
| `Id` | UUIDv7. |
| `ContentId` | FK to content. |
| `ParentCommentId` | Nullable self-FK (see Hierarchy). |
| `Depth` | Integer, capped. |
| `AuthorName` | Required, max length (e.g. 100). |
| `AuthorEmail` | Required, max 254. Normalised (trim, lower-case). Never returned by public queries or view models. |
| `Body` | Required Markdown source, max length (e.g. 4000). |
| `State` | Enum, with a check constraint for valid values. |
| `VerificationTokenHash` | Nullable. Hash only. Cleared once used. |
| `VerificationTokenExpiresAt` | Nullable. |
| `CreatedAt`, `UpdatedAt` | Via `IAuditable`. |
| `VerifiedAt`, `ApprovedAt`, `HiddenAt` | Nullable transition timestamps. |
| `ConcurrencyToken` | Via `IConcurrencyTracked`. |

Indexes:

- `(ContentId, State, CreatedAt)` for public listing.
- `(State, CreatedAt)` for the admin queue.
- `(ContentId, Id)` unique, to support the composite parent FK.
- `VerificationTokenHash` unique, filtered to non-null.

Privacy:

- The email is personal data, kept only for verification and moderator contact.
- Provide an admin action to anonymise/purge the email of a comment, and consider a retention policy for stale `Unverified` comments (e.g. delete after 7 days).

## Verification flow

1. The visitor submits name, email, body, and optional parent comment (antiforgery-protected POST).
2. The server validates input and creates the comment as `Unverified` with a random token (at least 32 bytes from a CSPRNG). Only its hash and an expiry (e.g. 48 hours) are stored.
3. If a valid trusted cookie matches the submitted email, skip the email and verify immediately (applying the approval mode).
4. Otherwise send `CommentVerificationEmailMessage` (new `EmailMessage` subclass alongside `AccountActivationEmailMessage`) with the verification URL.
5. `GET /content/comments/verify?id=…&token=…` shows a confirmation page. A GET must not change state, because mail scanners prefetch links.
6. The confirmation page POSTs to verify. The server checks the token (constant-time hash compare, not expired, state is `Unverified`), moves the comment to `Verified` (and on to `Approved` in `AutoApprove` mode), and clears the token.
7. The confirmation page includes a checkbox: **"Don't ask me to verify again on this browser"**. If checked, the response sets the trusted cookie.
8. Show a neutral message after submit ("check your email"). Do not reveal whether an address was used before.

Expired or invalid tokens show a generic failure page, with a rate-limited resend option.

### Trusted-commenter cookie

- New cookie name in `Constants` (e.g. `Acorn.CommentAuthor`).
- Value is protected with ASP.NET Core Data Protection (already configured), containing the verified email and issue time. Never a plain-text email.
- `HttpOnly`, `Secure`, `SameSite=Lax`, long expiry (e.g. 1 year).
- Issued only after successful verification where the user opted in.
- Honoured only when the submitted email matches the cookie's email.
- It skips verification only; the approval mode still applies.
- Pre-fill name and email on the form from the cookie, and provide a "not you?" action that clears it.

## Markdown and security

- Support **basic Markdown only**: paragraphs, emphasis, strong, inline code, code blocks, block quotes, lists, and links.
- Register a separate comment `MarkdownPipeline` rather than reusing the authored-content one (which uses `UseAdvancedExtensions`). It must not enable advanced extensions, and must call `DisableHtml()`.
- Images are not supported: disable image syntax so comments cannot embed remote images or reference attachments.
- Headings, tables, and other block extensions are not enabled. Decide whether to strip headings or render them as plain text. Prefer rendering them as text.
- All links get `rel="nofollow ugc noopener"`; only `http`, `https`, and `mailto` schemes are allowed.
- Render on read from the stored Markdown source.
- Razor escapes the author name. The email is never rendered.
- Antiforgery is already global. Add a honeypot field and a minimum-time-to-submit check.
- Add rate limiting (`AddRateLimiter`, not currently configured) per IP on submit and resend, and cap verification emails per address per hour so the form cannot be used to spam third parties.
- Validate email format and length. The verification link, not format validation, proves ownership.
- Tokens are single-use, expiring, and compared in constant time.

## Application layer

In `Acorn.Core/ContentManagement/`:

- `IContentCommentService` (public):
  - `SubmitAsync(contentId, parentCommentId?, name, email, body, trustedEmail, ct)`
  - `VerifyAsync(commentId, token, ct)`
  - `ListApprovedAsync(contentId, ct)`, returning the assembled tree
  - `ResendVerificationAsync(...)`
- `IContentCommentModerationService` (admin):
  - `ListAsync(state?, paging)`
  - `ApproveAsync`, `HideAsync`, `UnhideAsync`, using the concurrency token
  - `AnonymiseAsync`
- `ContentCommentOptions` with `ApprovalMode`.
- Models: `Comment` (id, author name, rendered HTML, created at, replies) and `CommentModerationItem` (adds email, state, timestamps, parent).
- Exceptions: reuse the existing not-found and concurrency exceptions; add `InvalidCommentTransitionException` and `InvalidVerificationTokenException`.
- Register services and options in `ServiceCollectionExtensions`.
- `CommentVerificationEmailMessage`: subject, plain-text body, `VerificationUrl`, and a line saying to ignore the email if it wasn't requested.

## Web layer

Routes (added to `Routes.cs` in its existing style):

- `POST /content/{id:guid}/comments` → submit (optional `parentCommentId`)
- `GET /content/comments/verify` → confirmation page
- `POST /content/comments/verify` → verify (+ optional trusted cookie)
- `POST /content/comments/forget` → clear trusted cookie
- Admin area:
  - `GET /admin/comments` (queue, filter by state)
  - `POST /admin/comments/approve/{id:guid}`
  - `POST /admin/comments/hide/{id:guid}`
  - `POST /admin/comments/unhide/{id:guid}`
  - `POST /admin/comments/anonymise/{id:guid}`

Views:

- `_Comments.cshtml` (recursive tree plus form) on post, bookmark, and note detail pages.
- Verification confirmation and result pages.
- Admin moderation list with state filter, parent context, and actions, reusing the concurrency error display pattern.

## Testing

Add to `Acorn.Core.Tests` (following `ServicesTestBase`):

- State machine: valid transitions succeed, invalid ones throw.
- Approval mode: `Auto` goes `Unverified → Approved` on verification, `Admin` stops at `Verified`; changing the mode does not affect existing `Verified` comments.
- Submit: creates `Unverified`, stores only the token hash, sends one email, and rejects unpublished or deleted content.
- Verify: valid token works; expired, reused, and wrong tokens fail; the token is cleared.
- Trusted path: matching email skips the email; a non-matching email does not.
- Hierarchy: the parent must be on the same content and `Approved`; depth cap is enforced; hiding a parent hides its subtree publicly; the tree is assembled and ordered correctly.
- Public queries return only visible comments and never expose the email.
- Markdown: HTML is escaped, images and advanced syntax are not rendered, links get `nofollow ugc`, and disallowed schemes are dropped.
- Moderation: concurrency conflicts are detected.
- Web: GET verify does not change state; rate limiting returns 429.

## Delivery steps

1. Entity, configuration (including hierarchy constraints), state enum, and migration.
2. State machine, options, and comment service (submit, verify, list tree).
3. Verification email message and token generation/hashing.
4. Comment Markdown pipeline.
5. Public controller, routes, views (threaded), and trusted cookie.
6. Rate limiting and honeypot.
7. Admin moderation service, controller, and views.
8. Tests throughout; update docs and `todo.md` when done.

## Out of scope (for now)

- Editing or deleting a comment by its author.
- Notifications to the site owner or to parent-comment authors.
- Subscriptions to replies.
- Third-party spam services.

## Implementation notes (deviations from the plan)

- `Unhide` is only allowed for comments that were previously verified; a never-verified comment cannot be unhidden.
- Added `IContentCommentService.CanCommentAsync` and a `/content/{id}/comments/result` page (POST-redirect-GET).
- Comment UI is wired to posts only, as they are the only content with a public detail page. The service is content-agnostic.
- Stale unverified comments are not cleaned up yet (future work).
