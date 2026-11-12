---
name: telemetry-sql
description: |
  Write and explain SQLite queries for Acorn's local OpenTelemetry logs and traces.
  Use when a query will be run in bin/log-viewer.html or when analyzing
  telemetry-yyyy-MM-dd.db files produced by Acorn.
---

# Acorn telemetry SQL

Write queries for the SQLite databases produced by Acorn's telemetry store. Unless
the user requests another tool, make every query runnable in `bin/log-viewer.html`.
Use only the schema and behavior documented here; do not assume there are metrics
tables or columns that are not listed.

## Workflow and viewer rules

1. Use `bin/backup_telemetry.sh` to make SQLite backups when the application may be
   writing to the source databases. Give it the telemetry directory and a separate
   destination directory. It creates consistent `.db` snapshots with SQLite's
   online backup API.
2. Open `bin/log-viewer.html` in a browser and select one or more `.db` snapshots.
   Do not select `-wal` or `-shm` files. The viewer processes the database files in
   browser memory; it does not upload them. The page loads SQLite from a public CDN.
3. Use exactly one SQLite statement per run. Prefer read-only `SELECT` or `WITH ...
   SELECT` queries. Ctrl/⌘+Enter runs the query. A loaded script only replaces the
   editor contents; it does not execute the query.
4. Query `Logs` and `Traces` to search across all selected databases. The viewer
   creates temporary combined views with `UNION ALL`; duplicate snapshots can
   therefore produce duplicate rows. Use `SourceDatabase` to identify a row's
   input filename. To target one selected file, use its attached alias, such as
   `db1.Logs` or `db2.Traces`; aliases follow file selection order.
5. Check the viewer's **View schemas** panel if a selected file may have an older
   or different schema. Combined views include the union of columns, with `NULL`
   for columns missing from an individual source. The raw attached tables do not
   have the viewer-added `SourceDatabase` column.

The viewer executes SQLite through sql.js. Use SQLite syntax and functions such as
`json_extract`, `json_each`, `COUNT`, and `strftime`. Do not use SQL Server,
PostgreSQL, or application-language syntax. Query changes are in-memory only and
are discarded when the page is reloaded.

## Database layout

The application writes daily UTC partitions named
`telemetry-yyyy-MM-dd.db` under its configured data directory's `telemetry/`
folder. `Logs` rows are partitioned by `TimestampUtc`; `Traces` rows by
`EndTimeUtc`. A span and its log records can be in different daily files. Select
all relevant partitions in the viewer before querying across time or correlating
logs and spans. There is no `Metrics` table in this schema.

Timestamps are ISO 8601 UTC strings formatted with the .NET round-trip (`O`)
format, normally ending in `Z`. They are text, not SQLite numeric timestamps.
They can be ordered and compared as strings because they use the same normalized
UTC format. Use matching round-trip UTC strings for range bounds.

## Exact stored schema

These are the current table declarations. A column without `NOT NULL` is
nullable. SQLite's `INTEGER PRIMARY KEY` on `Logs.Id` is the row identifier.

### `Traces`

| Column | SQLite type | Nullability / meaning |
|---|---|---|
| `TraceId` | `TEXT` | NOT NULL; trace ID, lowercase hexadecimal |
| `SpanId` | `TEXT` | NOT NULL; span ID, lowercase hexadecimal |
| `ParentSpanId` | `TEXT` | Nullable; parent span ID, or `NULL` for a root span |
| `Name` | `TEXT` | NOT NULL; activity display name |
| `SourceName` | `TEXT` | Nullable; activity source name |
| `SourceVersion` | `TEXT` | Nullable; activity source version |
| `Kind` | `INTEGER` | NOT NULL; `System.Diagnostics.ActivityKind` numeric value |
| `StartTimeUtc` | `TEXT` | NOT NULL; UTC activity start |
| `EndTimeUtc` | `TEXT` | NOT NULL; UTC activity end |
| `DurationMs` | `REAL` | NOT NULL; duration in milliseconds |
| `Status` | `INTEGER` | NOT NULL; `System.Diagnostics.ActivityStatusCode` numeric value |
| `StatusDescription` | `TEXT` | Nullable; activity status description |
| `AttributesJson` | `TEXT` | NOT NULL; JSON object of span attributes |
| `EventsJson` | `TEXT` | NOT NULL; JSON array of activity events |
| `LinksJson` | `TEXT` | NOT NULL; JSON array of activity links |
| `ResourceAttributesJson` | `TEXT` | NOT NULL; JSON object of OpenTelemetry resource attributes |

Primary key: (`TraceId`, `SpanId`). Index: `idx_traces_end_time` on
(`EndTimeUtc`). Older partitions may predate `SourceName` and `SourceVersion`;
the application adds these columns when it opens those partitions.

`Kind` values are `0` Internal, `1` Server, `2` Client, `3` Producer, and `4`
Consumer. `Status` values are `0` Unset, `1` Ok, and `2` Error.

### `Logs`

| Column | SQLite type | Nullability / meaning |
|---|---|---|
| `Id` | `INTEGER` | Primary key |
| `TimestampUtc` | `TEXT` | NOT NULL; event timestamp in UTC |
| `ObservedTimestampUtc` | `TEXT` | Nullable; observed timestamp in UTC |
| `Severity` | `INTEGER` | NOT NULL; `Microsoft.Extensions.Logging.LogLevel` numeric value |
| `SeverityText` | `TEXT` | NOT NULL; severity name |
| `Category` | `TEXT` | Nullable; logger category |
| `EventId` | `INTEGER` | NOT NULL |
| `EventName` | `TEXT` | Nullable |
| `Message` | `TEXT` | Nullable; rendered message |
| `Exception` | `TEXT` | Nullable; rendered exception |
| `TraceId` | `TEXT` | Nullable; correlated trace ID |
| `SpanId` | `TEXT` | Nullable; correlated span ID |
| `AttributesJson` | `TEXT` | NOT NULL; JSON object of structured log attributes |
| `ScopesJson` | `TEXT` | NOT NULL; JSON array of scope values |
| `ResourceAttributesJson` | `TEXT` | NOT NULL; JSON object of OpenTelemetry resource attributes |

Indexes: `idx_logs_trace_span` on (`TraceId`, `SpanId`) and
`idx_logs_timestamp` on (`TimestampUtc`). `Severity` values follow
`LogLevel`: `0` Trace, `1` Debug, `2` Information, `3` Warning, `4` Error,
`5` Critical, and `6` None. For normal severity filters, prefer `SeverityText`
such as `IN ('Error', 'Critical')` instead of a numeric comparison.

## JSON field shapes

The `*Json` columns are SQLite `TEXT` containing JSON. Span, log, and resource
attributes are JSON objects. Empty attribute collections are `{}`. Attribute
keys are case-sensitive; when keys repeat, the last value is retained. Values
that are not JSON primitives are normalized to strings or JSON-compatible
values.

`EventsJson` is an array of objects with `name`, `timestampUtc`, and
`attributesJson`. `LinksJson` is an array with `traceId`, `spanId`, `traceFlags`,
and `attributesJson`. In both arrays, `attributesJson` is itself a JSON-encoded
string, not a nested JSON object. `ScopesJson` is an array of normalized scope
values; it does not use the attribute-object representation.

Use JSON path quotes for OpenTelemetry keys containing dots or other punctuation:

```sql
json_extract(AttributesJson, '$."http.request.method"')
```

For activity event attributes, extract the inner JSON string before looking up
an attribute:

```sql
json_extract(
  json_extract(event.value, '$.attributesJson'),
  '$."exception.type"'
)
```

## Query patterns

### Recent log entries

```sql
SELECT TimestampUtc, SeverityText, Category, EventId, Message, SourceDatabase
FROM Logs
ORDER BY TimestampUtc DESC
LIMIT 100;
```

### Logs in a UTC time range

Use UTC round-trip strings for both bounds:

```sql
SELECT TimestampUtc, SeverityText, Category, Message, TraceId, SpanId
FROM Logs
WHERE TimestampUtc >= '2026-11-13T00:00:00.0000000Z'
  AND TimestampUtc <  '2026-11-14T00:00:00.0000000Z'
ORDER BY TimestampUtc;
```

### ASP.NET Core requests by path

The ASP.NET Core request span uses `SourceName = 'Microsoft.AspNetCore'` and
`Kind = 1` (Server). Request details are span attributes, not dedicated columns.

```sql
WITH requests AS (
  SELECT
    StartTimeUtc,
    json_extract(AttributesJson, '$."url.path"') AS request_path,
    json_extract(AttributesJson, '$."http.request.method"') AS method,
    json_extract(AttributesJson, '$."http.response.status_code"') AS status_code,
    json_extract(AttributesJson, '$."client.address"') AS client
  FROM Traces
  WHERE SourceName = 'Microsoft.AspNetCore'
    AND Kind = 1
)
SELECT request_path, method, status_code, COUNT(*) AS requests,
       MIN(StartTimeUtc) AS first_seen, MAX(StartTimeUtc) AS last_seen
FROM requests
GROUP BY request_path, method, status_code
ORDER BY requests DESC
LIMIT 100;
```

Check `AttributesJson` in the viewer before assuming an attribute is present or
has a particular type. Missing JSON paths return `NULL`.

### Logs correlated to a trace

Replace the example ID with the lowercase hexadecimal trace ID:

```sql
SELECT TimestampUtc, SeverityText, Category, Message, SpanId
FROM Logs
WHERE TraceId = '0123456789abcdef0123456789abcdef'
ORDER BY TimestampUtc;
```

Query the matching spans separately if needed:

```sql
SELECT SpanId, ParentSpanId, Name, Kind, StartTimeUtc, EndTimeUtc,
       DurationMs, Status, StatusDescription
FROM Traces
WHERE TraceId = '0123456789abcdef0123456789abcdef'
ORDER BY StartTimeUtc;
```

## Query-writing guidance

- Start with a bounded result (`LIMIT`) and select only useful columns. Large
  telemetry files and unbounded result tables can make browser queries slow.
- Use `SeverityText` for readable log filters and `Kind`/`Status` according to
  the enum mappings above.
- Use `json_extract` for known JSON fields. Use `json_each` to expand event or
  link arrays. Do not treat JSON-encoded attribute strings in `EventsJson` or
  `LinksJson` as objects until you extract them.
- Include `SourceDatabase` when a result needs partition provenance.
- Do not assume spans and their logs are stored in the same partition. The
  viewer can combine selected files, but it cannot find records in files that
  were not selected.
- Telemetry may contain sensitive application data. Avoid returning raw
  attributes, messages, exceptions, client addresses, or user agents unless
  they are needed for the user's diagnostic question. Prefer counts or
  redacted/grouped summaries when possible.
