#!/usr/bin/env python3
import argparse
import json
import sys
import urllib.error
import urllib.request
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Normalize k6 deployment probe logs, compute a summary, and optionally push them to Loki."
    )
    parser.add_argument("--input", required=True, help="Raw k6 stdout/stderr log file")
    parser.add_argument("--output", required=True, help="Normalized NDJSON output path")
    parser.add_argument("--summary-output", required=True, help="Summary JSON output path")
    parser.add_argument("--deployment-id", required=True)
    parser.add_argument("--environment", required=True)
    parser.add_argument("--service", required=True)
    parser.add_argument("--workflow", required=True)
    parser.add_argument("--run-id", required=True)
    parser.add_argument("--target-url", required=True)
    parser.add_argument("--probe-interval-ms", type=int, default=100)
    parser.add_argument("--loki-url", help="Optional Loki push endpoint")
    return parser.parse_args()


def parse_timestamp(value: str) -> datetime:
    if value.endswith("Z"):
        value = value[:-1] + "+00:00"
    return datetime.fromisoformat(value).astimezone(timezone.utc)


def to_loki_ns(timestamp: str) -> str:
    dt = parse_timestamp(timestamp)
    return str(int(dt.timestamp() * 1_000_000_000))


def try_parse_json(line: str) -> dict[str, Any] | None:
    line = line.strip()
    if not line or not line.startswith("{"):
        return None

    try:
        parsed = json.loads(line)
    except json.JSONDecodeError:
        return None

    if not isinstance(parsed, dict):
        return None

    return parsed


@dataclass
class ProbeEvent:
    payload: dict[str, Any]
    timestamp: datetime
    success: bool


def load_probe_events(args: argparse.Namespace) -> list[ProbeEvent]:
    raw_path = Path(args.input)
    events: list[ProbeEvent] = []

    for raw_line in raw_path.read_text(encoding="utf-8", errors="replace").splitlines():
        payload = try_parse_json(raw_line)
        if not payload or payload.get("event_type") != "probe":
            continue

        timestamp = payload.get("timestamp")
        if not isinstance(timestamp, str):
            continue

        try:
            parsed_timestamp = parse_timestamp(timestamp)
        except ValueError:
            continue

        normalized = {
            "event_type": "probe",
            "timestamp": parsed_timestamp.isoformat().replace("+00:00", "Z"),
            "deployment_id": args.deployment_id,
            "environment": args.environment,
            "service": args.service,
            "workflow": args.workflow,
            "run_id": args.run_id,
            "target_url": args.target_url,
            "status_code": int(payload.get("status_code", 0) or 0),
            "success": bool(payload.get("success", False)),
            "success_value": 1 if bool(payload.get("success", False)) else 0,
            "duration_ms": float(payload.get("duration_ms", 0) or 0),
        }

        error = payload.get("error")
        if isinstance(error, str) and error:
            normalized["error"] = error

        events.append(
            ProbeEvent(
                payload=normalized,
                timestamp=parsed_timestamp,
                success=normalized["success"],
            )
        )

    events.sort(key=lambda event: event.timestamp)
    return events


def build_summary(args: argparse.Namespace, probes: list[ProbeEvent]) -> dict[str, Any]:
    if not probes:
        raise ValueError("No probe events were found in the k6 output.")

    total_probes = len(probes)
    failed_probes = sum(1 for probe in probes if not probe.success)

    current_streak = 0
    longest_streak = 0
    for probe in probes:
        if probe.success:
            current_streak = 0
            continue
        current_streak += 1
        if current_streak > longest_streak:
            longest_streak = current_streak

    success_percent = ((total_probes - failed_probes) / total_probes) * 100
    started_at = probes[0].payload["timestamp"]
    ended_at = probes[-1].payload["timestamp"]

    return {
        "event_type": "summary",
        "timestamp": ended_at,
        "deployment_id": args.deployment_id,
        "environment": args.environment,
        "service": args.service,
        "workflow": args.workflow,
        "run_id": args.run_id,
        "target_url": args.target_url,
        "started_at": started_at,
        "ended_at": ended_at,
        "probe_interval_ms": args.probe_interval_ms,
        "total_probes": total_probes,
        "failed_probes": failed_probes,
        "success_percent": round(success_percent, 3),
        "estimated_downtime_ms": failed_probes * args.probe_interval_ms,
        "longest_failure_streak_ms": longest_streak * args.probe_interval_ms,
    }


def write_outputs(output_path: Path, summary_path: Path, probes: list[ProbeEvent], summary: dict[str, Any]) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    summary_path.parent.mkdir(parents=True, exist_ok=True)

    lines = [json.dumps(probe.payload, separators=(",", ":")) for probe in probes]
    lines.append(json.dumps(summary, separators=(",", ":")))
    output_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
    summary_path.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")


def push_to_loki(loki_url: str, probes: list[ProbeEvent], summary: dict[str, Any]) -> None:
    stream_labels = {
        "source": "deployment-k6",
        "service": summary["service"],
        "environment": summary["environment"],
        "deployment_id": summary["deployment_id"],
        "workflow": summary["workflow"],
        "run_id": summary["run_id"],
    }

    values: list[list[str]] = []
    for probe in probes:
        values.append(
            [
                to_loki_ns(probe.payload["timestamp"]),
                json.dumps(probe.payload, separators=(",", ":")),
            ]
        )

    values.append(
        [
            to_loki_ns(summary["timestamp"]),
            json.dumps(summary, separators=(",", ":")),
        ]
    )

    payload = {
        "streams": [
            {
                "stream": stream_labels,
                "values": values,
            }
        ]
    }

    request = urllib.request.Request(
        loki_url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )

    with urllib.request.urlopen(request, timeout=30) as response:
        if response.status >= 300:
            raise RuntimeError(f"Loki push failed with status {response.status}")


def main() -> int:
    args = parse_args()

    try:
        probes = load_probe_events(args)
        summary = build_summary(args, probes)
        write_outputs(Path(args.output), Path(args.summary_output), probes, summary)

        if args.loki_url:
            push_to_loki(args.loki_url, probes, summary)

        print("Deployment downtime summary:")
        print(json.dumps(summary, indent=2))
        return 0
    except urllib.error.URLError as error:
        print(f"Failed to push deployment logs to Loki: {error}", file=sys.stderr)
        return 1
    except Exception as error:  # noqa: BLE001
        print(f"Failed to prepare deployment logs: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
