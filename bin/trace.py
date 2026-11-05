#!/usr/bin/env python3
"""Show telemetry recorded by Acorn.

  bin/trace.py                 recent traces as span trees (no logs)
  bin/trace.py <trace-id>      show every span and log for one trace (prefix is fine)
  bin/trace.py --instance <id> only traces from one app run (service.instance.id, prefix is fine)
  bin/trace.py --dir <path>    telemetry directory (default: var/telemetry, or $TELEMETRY_DIR)
"""
import argparse
import glob
import json
import os
import sqlite3
import sys

SEVERITY = {0: "TRC", 1: "DBG", 2: "INF", 3: "WRN", 4: "ERR", 5: "CRT", 6: "NON"}
STATUS = {0: "", 1: "OK", 2: "ERROR"}


def connect(path):
    # Read-only so inspecting never creates or alters a partition.
    return sqlite3.connect(f"file:{path}?mode=ro", uri=True)


def source_column(db):
    # Partitions written before SourceName existed do not have the column.
    columns = {row[1] for row in db.execute("pragma table_info(Traces)")}
    return "SourceName" if "SourceName" in columns else "null"


def files(directory):
    return sorted(glob.glob(os.path.join(directory, "telemetry-*.db")), reverse=True)


def list_traces(directory, limit, instance=None):
    rows = []
    for path in files(directory):
        with connect(path) as db:
            rows += db.execute(
                """
                select TraceId, StartTimeUtc
                from Traces
                where ParentSpanId is null
                  and (? is null or json_extract(ResourceAttributesJson, '$."service.instance.id"') like ? || '%')
                order by StartTimeUtc desc limit ?
                """,
                (instance, instance, limit),
            ).fetchall()
    rows.sort(key=lambda r: r[1], reverse=True)
    for trace_id, _ in rows[:limit]:
        show(directory, trace_id, with_logs=False, compact=True)


def load(directory, prefix):
    spans, logs = [], []
    for path in files(directory):
        with connect(path) as db:
            spans += db.execute(
                """
                select TraceId, SpanId, ParentSpanId, Name, StartTimeUtc, DurationMs,
                       Status, StatusDescription, AttributesJson, %s
                from Traces where TraceId like ?
                """ % source_column(db),
                (prefix + "%",),
            ).fetchall()
            logs += db.execute(
                """
                select TraceId, SpanId, TimestampUtc, Severity, Category, Message, Exception
                from Logs where TraceId like ?
                """,
                (prefix + "%",),
            ).fetchall()
    return spans, logs


def attrs(text):
    try:
        return {a["key"]: a["value"] for a in json.loads(text)}
    except (ValueError, TypeError, KeyError):
        return {}


def show(directory, prefix, with_logs=True, compact=False):
    spans, logs = load(directory, prefix)
    if not with_logs:
        logs = []
    if not spans and not logs:
        print(f"No telemetry found for trace '{prefix}'.", file=sys.stderr)
        return 1

    trace_ids = {r[0] for r in spans} | {r[0] for r in logs}
    if len(trace_ids) > 1:
        print("Prefix is ambiguous, matches:", file=sys.stderr)
        for t in sorted(trace_ids):
            print(f"  {t}", file=sys.stderr)
        return 1

    origin = min([s[4] for s in spans] + [l[2] for l in logs])

    def offset(ts):
        from datetime import datetime

        def parse(v):
            return datetime.fromisoformat(v.rstrip("Z")[:26])

        return (parse(ts) - parse(origin)).total_seconds() * 1000

    by_id = {s[1]: s for s in spans}
    children, roots = {}, []
    for s in sorted(spans, key=lambda r: r[4]):
        if s[2] and s[2] in by_id:
            children.setdefault(s[2], []).append(s)
        else:
            roots.append(s)

    logs_by_span = {}
    for l in logs:
        logs_by_span.setdefault(l[1], []).append(l)

    trace_id = next(iter(trace_ids))
    if not compact:
        print(f"Trace {trace_id}")

    def emit_logs(span_logs, depth):
        for _, _, ts, sev, cat, msg, exc in sorted(span_logs, key=lambda r: r[2]):
            first, *rest = (msg or "").splitlines() or [""]
            print(f"{offset(ts):9.1f}ms {'  ' * depth}[{SEVERITY.get(sev, sev)}] {cat}: {first}")
            for line in rest:
                print(f"{'':11} {'  ' * depth}      {line}")
            if exc:
                for line in exc.splitlines():
                    print(f"{'':11} {'  ' * depth}      {line}")

    def emit(span, depth):
        _, span_id, _, name, start, duration, status, status_desc, attributes, source = span
        a = attrs(attributes)
        status_text = STATUS.get(status, "")
        extra = f"  {status_text}" if status_text else ""
        if status_desc:
            extra += f" ({status_desc})"
        if compact:
            http_status = a.get("http.response.status_code")
            suffix = f"  {http_status}" if http_status is not None else ""
            print(f"{trace_id}  {span_id}  {start[:23]}  {'  ' * depth}{name}  [{duration:.1f}ms]{suffix}{extra}  <{source or '?'}>")
            for child in children.get(span_id, []):
                emit(child, depth + 1)
            return
        print(f"{offset(start):9.1f}ms {'  ' * depth}{name}  [{duration:.1f}ms]{extra}  <{source or '?'}>")
        statement = a.get("db.statement")
        if statement:
            print(f"{'':11} {'  ' * depth}  sql: {' '.join(statement.split())[:200]}")
        if a.get("http.response.status_code") is not None:
            print(f"{'':11} {'  ' * depth}  status: {a['http.response.status_code']}")

        timeline = [(c[4], 0, c) for c in children.get(span_id, [])]
        timeline += [(l[2], 1, l) for l in logs_by_span.get(span_id, [])]
        for _, kind, item in sorted(timeline, key=lambda t: (t[0], t[1])):
            if kind == 0:
                emit(item, depth + 1)
            else:
                emit_logs([item], depth + 1)

    for root in roots:
        emit(root, 0)

    known = set(by_id)
    orphan_logs = [l for l in logs if l[1] not in known]
    if orphan_logs:
        print("\nLogs without a recorded span:")
        emit_logs(orphan_logs, 0)
    return 0


def main():
    root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("trace_id", nargs="?")
    parser.add_argument("--dir", default=os.environ.get("TELEMETRY_DIR", os.path.join(root, "var", "telemetry")))
    parser.add_argument("--instance")
    parser.add_argument("--limit", type=int, default=20)
    args = parser.parse_args()

    if not files(args.dir):
        print(f"No telemetry databases in {args.dir}", file=sys.stderr)
        return 1
    if args.trace_id:
        return show(args.dir, args.trace_id)
    list_traces(args.dir, args.limit, args.instance)
    return 0


if __name__ == "__main__":
    sys.exit(main())
