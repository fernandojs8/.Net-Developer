#!/usr/bin/env bash
set -euo pipefail
if [ "$#" -ne 1 ]; then echo "Usage: bash scripts/rollback.sh IMAGE" >&2; exit 2; fi
docker image inspect "$1" >/dev/null
APP_IMAGE="$1" docker compose up -d --no-deps --no-build web web2
for port in 5080 5081; do
  healthy=false
  for attempt in {1..60}; do
    if curl -fsS "http://localhost:$port/health/ready" >/dev/null; then healthy=true; break; fi
    sleep 2
  done
  test "$healthy" = true
done
