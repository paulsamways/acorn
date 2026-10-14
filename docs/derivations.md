# Attachment Derivations

## Current behavior

`IContentAttachmentService` owns attachment creation and serving. `IContentService` is limited to published-content queries. Image derivations are generated during upload and stored as child `ContentAttachment` records; serving an attachment never generates a missing derivation.

Uploads must use a readable, seekable stream. The service checks that the current user owns the target content, validates the upload, writes the original, runs every registered derivation producer that accepts the validated metadata, and saves the attachment records. The returned UUID identifies the root attachment.

The current producers accept image metadata and create these WebP renditions:

| Name | Maximum longest edge | Use |
| --- | ---: | --- |
| `large` | 4096 px | Large rendition |
| `inline` | 1200 px | Content body |
| `preview` | 320 px | Post excerpt preview |

Renditions preserve aspect ratio, are not upscaled, apply EXIF orientation, and remove EXIF, XMP, and IPTC profiles before encoding at WebP quality 82. Their filenames are based on the original filename with the derivation name and `.webp` suffix; names are shortened when necessary to meet the file validator's maximum filename length.

The original and all generated renditions currently have `Public` attachment visibility. Publication still gates access: a non-owner can open an attachment only when its content is published, and an attachment's own visibility is also enforced. Public cacheability is reported only for a public attachment on published content. This means the original can be served by its filename URL for published content; generated Markdown image URLs use the corresponding WebP rendition.

## URL generation and serving

Markdown image references use `file:<attachment-uuid>`. `ContentModelMapper` selects the `inline` rendition for content bodies and `preview` for excerpts. `AttachmentUrlResolver` asks `IContentAttachmentService` for the selected rendition's stored filename, then generates the existing named route:

```text
/content/attachments/{attachmentId}/{filename}
```

The filename is part of the URL and identifies which stored file is requested. `OpenAttachmentByFilenameAsync` matches it against the root attachment or one of its children, then applies publication, ownership, and visibility checks. The explicitly named `OpenAttachmentAsync` and `GetAttachmentFilenameAsync` operations accept derivation names case-insensitively; passing no derivation name selects the original, not a default child.

The split between `IContentService` and `IContentAttachmentService` is important: `ContentService` depends on the mapper, the mapper depends on `IAttachmentUrlResolver`, and the resolver depends on `IContentAttachmentService`. The resolver no longer depends on `IContentService`, so attachment URL generation does not form a DI cycle.

## Current limitations

- Derivation producers are registered individually; there is no named pipeline selection, preferred rendition, or default-child fallback.
- Non-seekable upload streams are rejected rather than buffered.
- There is no explicit upload size, decoded-pixel, or processing-time budget in the derivation producer.
- Attachment lifecycle operations such as detach, soft-delete, restore, and purge are not part of `IContentAttachmentService`.

See [file-plan.md](./file-plan.md) for the broader file-storage plan. That document records the original design proposal; this page describes the currently implemented derivation and serving behavior.
