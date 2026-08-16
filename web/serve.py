#!/usr/bin/env python3
"""Dev server for the BONES playtest: plain static files with caching disabled,
so the browser always picks up the latest engine/app changes on a normal refresh.
Run: python3 web/serve.py [port]   (default 8000)"""
import http.server
import sys

PORT = int(sys.argv[1]) if len(sys.argv) > 1 else 8000


class NoCacheHandler(http.server.SimpleHTTPRequestHandler):
    def end_headers(self):
        self.send_header('Cache-Control', 'no-store, must-revalidate')
        self.send_header('Pragma', 'no-cache')
        self.send_header('Expires', '0')
        super().end_headers()

    def log_message(self, *args):
        pass  # keep the console quiet


if __name__ == '__main__':
    import os
    os.chdir(os.path.dirname(os.path.abspath(__file__)))
    print(f'BONES playtest at http://localhost:{PORT} (caching disabled)')
    http.server.ThreadingHTTPServer(('', PORT), NoCacheHandler).serve_forever()
