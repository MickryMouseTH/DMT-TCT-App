#!/bin/bash
# ส่งแป้นเข้า TCT แบบคีย์บอร์ดจริง:  ./kb.sh u c   |  ./kb.sh shot <ชื่อ>
cd "$(dirname "$0")"
if [ "$1" = "shot" ]; then mkdir -p shots; curl -s -o "shots/$2.png" "http://localhost:8090/api/shot"; echo "shots/$2.png"; exit; fi
for k in "$@"; do
  curl -s -XPOST localhost:8090/api -H 'Content-Type: application/json' -d "{\"action\":\"kb\",\"k\":\"$k\",\"name\":\"$k\"}" >/dev/null
  sleep 0.6
done
