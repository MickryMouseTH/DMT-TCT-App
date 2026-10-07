#!/usr/bin/env python3
"""ส่งการกดแป้นเข้า TCT ผ่าน xdotool (= เส้นทางเดียวกับคีย์บอร์ด USB จริง)
GET /key?k=<X keysym>   เช่น u, c, Return, BackSpace, Left, equal, bracketleft
GET /shot               ภาพหน้าจอ PNG
"""
import os
import subprocess
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

ENV = dict(os.environ, DISPLAY=":0")
ALLOWED = set("abcdefghijklmnopqrstuvwxyz0123456789") | {
    "Return", "BackSpace", "Escape", "Tab", "End", "Left", "Right", "Up", "Down", "period",
    "equal", "bracketleft", "bracketright", "backslash", "Delete", "Home", "space", "minus"}


class H(BaseHTTPRequestHandler):
    def log_message(self, *a):
        pass

    def _send(self, code, body, ctype="text/plain"):
        self.send_response(code)
        self.send_header("Content-Type", ctype)
        self.send_header("Access-Control-Allow-Origin", "*")
        self.end_headers()
        self.wfile.write(body)

    def do_GET(self):
        u = urlparse(self.path)
        q = parse_qs(u.query)
        if u.path == "/key":
            k = (q.get("k") or [""])[0]
            if k not in ALLOWED:
                return self._send(400, b"bad key")
            r = subprocess.run(["xdotool", "key", "--clearmodifiers", k], env=ENV, capture_output=True)
            return self._send(200 if r.returncode == 0 else 500, b"ok" if r.returncode == 0 else r.stderr)
        if u.path == "/shot":
            r = subprocess.run("xwd -root -silent | convert xwd:- png:-",
                               shell=True, env=ENV, capture_output=True)
            return self._send(200, r.stdout, "image/png")
        self._send(404, b"not found")


ThreadingHTTPServer(("0.0.0.0", 6081), H).serve_forever()
