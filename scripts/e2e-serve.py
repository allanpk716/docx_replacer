#!/usr/bin/env python3
"""
Minimal HTTP server for Velopack E2E update testing.

Serves releases.win.json and .nupkg files from a specified directory,
logging each GET request to stdout so the tester can observe Velopack
update check and download traffic in real time.

Usage:
    python e2e-serve.py [--port PORT] [--directory DIR]

Defaults: port=8080, directory=../e2e-test/v1.1.0/
"""

import argparse
import http.server
import os
import sys
import threading


class VelopackRequestHandler(http.server.SimpleHTTPRequestHandler):
    """Logs each request line to stdout for observability."""

    def do_GET(self):
        print(f"[E2E-SERVE] GET {self.path} from {self.client_address[0]}")
        sys.stdout.flush()
        return super().do_GET()

    def log_message(self, format, *args):
        # Suppress default stderr logging; we log our own format above
        pass


def main():
    parser = argparse.ArgumentParser(
        description="Serve Velopack release artifacts for E2E update testing"
    )
    parser.add_argument(
        "--port", type=int, default=8080, help="Port to listen on (default: 8080)"
    )
    parser.add_argument(
        "--directory",
        default=None,
        help="Directory to serve (default: ../e2e-test/v1.1.0/)",
    )
    args = parser.parse_args()

    if args.directory:
        serve_dir = os.path.abspath(args.directory)
    else:
        script_dir = os.path.dirname(os.path.abspath(__file__))
        serve_dir = os.path.join(script_dir, "..", "e2e-test", "v1.1.0")
        serve_dir = os.path.abspath(serve_dir)

    if not os.path.isdir(serve_dir):
        print(f"[E2E-SERVE] ERROR: directory not found: {serve_dir}")
        sys.exit(1)

    os.chdir(serve_dir)

    server = http.server.HTTPServer(("", args.port), VelopackRequestHandler)
    print(f"[E2E-SERVE] Serving {serve_dir} on http://localhost:{args.port}/")
    print("[E2E-SERVE] Press Ctrl+C to stop")
    sys.stdout.flush()

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        print("\n[E2E-SERVE] Shutting down")
        server.shutdown()


if __name__ == "__main__":
    main()
