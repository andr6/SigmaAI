import sys
import json

# Simulated CLI for the GAN cyber range simulator
# Usage: python3 gan_range_runner.py <command>
# Commands: start, metrics, stop


def start_simulation():
    print("Simulation started")


def get_metrics():
    metrics = {
        "compromise_rate": 0.15,
        "avg_detection_time": "5m",
        "patches_deployed": 3
    }
    print(json.dumps(metrics))


def stop_simulation():
    print("Simulation stopped")


def main():
    if len(sys.argv) < 2:
        print("Usage: gan_range_runner.py <start|metrics|stop>", file=sys.stderr)
        sys.exit(1)

    cmd = sys.argv[1]
    if cmd == "start":
        start_simulation()
    elif cmd == "metrics":
        get_metrics()
    elif cmd == "stop":
        stop_simulation()
    else:
        print(f"Unknown command: {cmd}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
