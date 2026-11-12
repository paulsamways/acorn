# OpenTelemetry with local SQLite storage

## Goal

Store useful application telemetry locally without a collector, dashboard, or telemetry network endpoint. Keep the SQLite files under the configured `DataDirectory`, where developers can inspect them during diagnosis.

The application already stores its primary database and other files under `DataDirectory`. Resolve telemetry paths through the same boundary, for example, in `telemetry/`. The production deployment persists `/app/data`.

Use this store for operational diagnostics. Do not use it as an audit log or durable business record. Overload or storage failures can cause telemetry loss. Telemetry can contain sensitive application data, so restrict filesystem access. Never record credentials, tokens, or other secrets.

## Scope and rollout

Implement the work in two stages:

1. **Traces and logs:** Write to daily SQLite partitions. Bound asynchronous writes and add retention.
2. **Metrics:** Start this stage after defining the required instruments, aggregation, cardinality limits, and retention or downsampling policy. Do not store each metric as a single `double`. Counters and histograms need explicit aggregation rules.

The first stage does not add a telemetry HTTP endpoint or require a dashboard. It does not change the primary application database or add telemetry to the existing application-database backup.

## Storage layout

Use UTC-day partitions for traces and logs:

```text
<DataDirectory>/telemetry/telemetry-yyyy-MM-dd.db
```

Partition each trace by its end time, calculated from its start time and duration. Partition each log by its event time. The writer can then choose a file consistently, even when an activity crosses midnight. A trace and its logs can be in different files. Diagnostic tools and queries must search the relevant UTC-day files rather than assume they can join across files.

Keep 30 days of data by default. File-level retention avoids deleting rows one at a time, but daily files can still fragment a database. Parse the expected filename format before deleting files. Run retention only after the writer closes the database. Handle SQLite journal and WAL sidecar files safely. Never delete a database that has an open connection.

Store timestamps in a documented and consistently normalized UTC format. Use parameterized SQL. The initial schema must retain enough context for useful diagnostics:

- **Traces:** trace ID, span ID, parent span ID, name, kind, start and end timestamps, status, duration, activity attributes, resource attributes, events, and links. Store resource attributes as JSON from the provider's resource. Include a per-process `service.instance.id` GUID to distinguish application runs. Use `(TraceId, SpanId)` as the key and support lookup by trace ID.
- **Logs:** event timestamp, severity, category, event ID, rendered message, exception, optional trace and span IDs, structured state and scope properties, and resource attributes. Index the trace ID, the span ID when present, and the timestamp.

Store dynamic attributes and event or link collections as JSON when that is simpler than normalized child tables. Define serialization and null handling explicitly. Do not silently discard unsupported values.

Serialize span, log, event, and link attribute collections as JSON objects. Use case-sensitive keys and keep the last value when keys repeat. Represent empty collections as `{}`. Keep event and link attributes as JSON strings inside their event or link records. Log scopes keep their separate existing representation. Do not migrate existing array-form attributes.

## OpenTelemetry integration

Use an integration for each signal. A single `BaseProcessor<Activity>` does not handle logs or metrics.

- **Traces:** An OpenTelemetry trace exporter receives completed activities and maps them to trace records.
- **Logs:** An OpenTelemetry logging exporter receives log records and maps them to log records. Preserve structured state and scopes when available. Do not store only the rendered message.
- **Metrics (later stage):** Use a metric reader and exporter. Define how to aggregate and represent each supported instrument.

Use OpenTelemetry's normal instrumentation and resource configuration where applicable. Subscribe the trace setup to ASP.NET Core, the runtime's built-in `System.Net.Http` source, and the `Acorn` source. Since .NET 9, `System.Net.Http` redacts URL query strings by default. Do not set `System.Net.Http.DisableQueryRedaction`.

EF Core has no built-in activity source. Add the beta `OpenTelemetry.Instrumentation.EntityFrameworkCore` package to create database spans. It emits one client span per command and records the SQL text in `db.statement`. It records parameter values only when the experimental `OTEL_DOTNET_EXPERIMENTAL_EFCORE_ENABLE_TRACE_DB_QUERY_PARAMETERS` environment variable is set. Keep this variable unset.

`bin/trace.py` lists recent traces. Use `--instance <service.instance.id>` to filter them. The script prints a trace's spans and logs as a timeline.

Exclude the development email logger from the OpenTelemetry logging provider. It logs message bodies, including password-reset links. Review new instrumentation and log fields for sensitive data before enabling them.

## Write path and failure behavior

Do not write to SQLite synchronously from request-completion or logging call paths. Use OpenTelemetry's batch activity and log-record export processors with a finite queue. These processors export records on background workers. The SQLite store serializes those workers and retention with one lock, then commits each partition batch in a transaction. This keeps one active SQLite writer without a separate channel or worker layer.

When a processor queue fills, OpenTelemetry drops records instead of blocking application callbacks. The SDK tracks queue-drop self-observability counters. The first stage does not export these counters. Exporting them belongs to the future metrics work.

Log storage and export failures, and return failed exports. Do not retry without a bound or keep an unbounded in-memory backlog. During shutdown, flush the batch processors before disposing the storage service.

Use structured, parameterized commands. Initialize each partition's schema idempotently. Enable WAL mode so readers can inspect telemetry databases while the writer is active. WAL does not allow parallel writers. Use short-lived, unpooled connections. Checkpoint before retention deletes a partition. Handle WAL sidecars during retention and copying.

## Retention, operations, and backups

Run retention as part of the telemetry storage lifecycle or in a hosted maintenance service. Coordinate it with the single writer so the writer closes a partition before deletion. Make retention failures visible in application diagnostics.

Exclude telemetry from the current application-database backup unless a later requirement changes this decision. Telemetry is diagnostic data and can be regenerated. Revisit this decision during the metrics stage if long-term metric history becomes important.

To inspect a live database, use SQLite's online backup API. You can also stop the application and safely close or checkpoint the database before copying it. Copying only the `.db` file while it is open might not produce a consistent snapshot. Open local copies with standard SQLite desktop tools.

## Configuration and dependencies

Document the initial retention period and queue capacity as configurable options with sensible defaults. Do not add unrelated configuration. Resolve the telemetry directory through the existing `DataDirectory` service so relative paths remain within the configured data root.

Add only the OpenTelemetry packages required for the selected trace and log integrations. Follow the repository's central package management and lock-file conventions. Choose package versions compatible with the project's target framework.

## Verification

Tests should cover:

- Trace and log serialization, structured attributes, optional correlation IDs, and parameterized persistence.
- UTC partition selection, including activities that cross midnight and events that arrive out of order.
- Bounded-queue overflow and visible dropped-record accounting.
- Batched writes, schema creation, writer shutdown and flush, and database write failures.
- Retention around the cutoff, including safe handling of open connections and SQLite sidecars.
- Confirmation that local-storage configuration requires no telemetry endpoint or external collector.

Once the metrics stage is designed, add tests for each supported instrument and its aggregation.
