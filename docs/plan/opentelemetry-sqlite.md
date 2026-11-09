# OpenTelemetry with local SQLite storage

## Goal

Store useful application telemetry locally, without running a collector, dashboard, or telemetry network endpoint. SQLite files should be straightforward to inspect during diagnosis and should live beneath the application's configured `DataDirectory`.

The application already persists its primary database and other files under `DataDirectory`; telemetry storage should use that same path-resolution boundary (for example, `telemetry/`). The production deployment persists `/app/data`.

This design is for operational diagnostics, not an audit log or durable business record. Telemetry may be dropped during overload or storage failure. It can contain sensitive application data, so keep filesystem access restricted and do not record credentials, tokens, or other secrets.

## Scope and rollout

Implement this in stages:

1. **Traces and logs:** daily SQLite partitions, bounded asynchronous writes, and retention.
2. **Metrics:** only after selecting the required instruments, aggregation, cardinality limits, and retention/downsampling policy. Do not treat each metric as a single `double`; histogram and counter data need explicit aggregation semantics.

The first stage does not expose a telemetry HTTP endpoint or require a dashboard. It does not change the application's primary database or make telemetry part of the existing application-database backup.

## Storage layout

Use UTC-day partitions for traces and logs:

```text
<DataDirectory>/telemetry/telemetry-yyyy-MM-dd.db
```

Partition a trace by its end timestamp (start timestamp plus duration), and a log by its event timestamp. This keeps the writer's file choice deterministic even for activities spanning midnight. A trace and its logs can therefore land in different files; diagnosis tools or queries must search the relevant UTC-day files rather than assume a cross-file SQL join.

Retain 30 days by default. Retention is file-level, which avoids row-by-row deletion, but daily files do not inherently prevent database fragmentation. Retention must parse the expected filename format, run only after the writer has closed the relevant database, and handle SQLite journal/WAL sidecar files safely. Do not delete a database that has an open connection.

Store timestamps in a documented, consistently normalized UTC representation. Use parameterized SQL. The initial schema should preserve enough context for useful diagnostics:

- **Traces:** trace ID, span ID, parent span ID, name, kind, start/end timestamps, status, duration, activity attributes, resource attributes (stored as JSON from the provider's resource, including a per-process `service.instance.id` GUID that distinguishes app runs), events, and links. Use `(TraceId, SpanId)` as the key and support lookup by trace ID.
- **Logs:** event timestamp, severity, category, event ID, rendered message, exception, optional trace/span IDs, structured state and scope properties, and resource attributes. Index trace ID (and span ID where present) and timestamp.

Represent dynamic attributes and event/link collections as JSON where that is simpler than normalized child tables. Define serialization and null handling explicitly; do not silently discard unsupported values.
Span, log, event and link attribute collections are serialized as JSON objects, using case-sensitive keys and retaining the last value for duplicate keys. Empty collections are `{}`. Event and link attributes remain JSON strings within their respective event/link records; log scopes retain their separate existing representation. Existing array-form attributes are not migrated.

## OpenTelemetry integration

Use a signal-appropriate integration for each signal; a single `BaseProcessor<Activity>` does not handle logs and metrics:

- **Traces:** an OpenTelemetry trace exporter receives completed activities and maps them to trace records.
- **Logs:** an OpenTelemetry logging exporter receives log records and maps them to log records. Preserve structured state and scopes where available rather than storing only the rendered message.
- **Metrics (later stage):** use a metric reader/exporter and define how each supported instrument is aggregated and represented.

Use OpenTelemetry's normal instrumentation and resource configuration where applicable. The trace setup subscribes to ASP.NET Core, the runtime's built-in `System.Net.Http` source (which redacts URL query strings by default since .NET 9; do not set `System.Net.Http.DisableQueryRedaction`), and the `Acorn` source. EF Core has no built-in activity source, so database spans come from the beta `OpenTelemetry.Instrumentation.EntityFrameworkCore` package, which emits one client span per command with the SQL text (`db.statement`). Parameter values are recorded only if the experimental `OTEL_DOTNET_EXPERIMENTAL_EFCORE_ENABLE_TRACE_DB_QUERY_PARAMETERS` environment variable is set; it must stay unset. `bin/trace.py` lists recent traces (optionally filtered with `--instance <service.instance.id>`) and prints one trace's spans and logs as a timeline. Exclude the development email logger from the OpenTelemetry logging provider because it logs message bodies, including password-reset links. Review any new instrumentation and log fields for sensitive data before enabling them.

## Write path and failure behavior

Do not perform SQLite writes synchronously on request-completion or logging call paths. Use OpenTelemetry's batch activity and log-record export processors with a configured, finite queue. They move exports to background workers; the SQLite store serializes those workers and retention with one lock, and commits each partition batch in a transaction. This preserves a single active SQLite writer without adding a separate channel/worker layer.

When an OpenTelemetry processor queue fills, records are dropped rather than blocking application callbacks. The SDK records queue-drop self-observability counters, but this first stage does not export those counters; exposing them is part of the future metrics work. Storage/export failures are logged and returned as failed exports, with no unbounded retry or in-memory backlog. Provider shutdown flushes the batch processors before the storage service is disposed.

Use structured, parameterized commands and initialize each partition's schema idempotently. Enable WAL mode for telemetry databases so inspection readers can proceed concurrently with the writer; WAL does not allow parallel writers. Use short-lived, unpooled connections, and checkpoint before retention deletes a partition. WAL sidecars must be handled during retention and copying.

## Retention, operations, and backups

Run retention as part of the telemetry storage lifecycle or a hosted maintenance service. Coordinate it with the single writer so a partition is closed before deletion. Retention failures must be visible in application diagnostics.

Telemetry is excluded from the current application-database backup unless a later requirement says otherwise. It is diagnostic data and may be regenerated; the metrics phase must revisit this decision if long-term metric history becomes important.

To inspect a live database, use SQLite's online backup API (or stop the application and safely close/checkpoint the database) before copying it. Do not assume copying only the `.db` file while it is open produces a consistent snapshot. Local copies can be opened with standard SQLite desktop tools.

## Configuration and dependencies

Keep the initial retention period and queue capacity as documented options with sensible defaults; avoid adding unrelated configuration. Resolve the telemetry directory through the existing `DataDirectory` service so relative paths remain within the configured data root.

Add only the OpenTelemetry packages required for the selected trace and log integrations. Follow the repository's central package management and lock-file conventions, and choose compatible package versions for the project's target framework.

## Verification

Tests should cover:

- Trace and log serialization, structured attributes, optional correlation IDs, and parameterized persistence.
- UTC partition selection, including an activity spanning midnight and events arriving out of order.
- Bounded-queue overflow and visible dropped-record accounting.
- Batched writes, schema creation, writer shutdown/flush, and database write failures.
- Retention around the cutoff, including safe handling of open connections and SQLite sidecars.
- No telemetry endpoint or external collector is required by the local-storage configuration.

Metrics require their own tests for each supported instrument and aggregation once that phase is designed.
