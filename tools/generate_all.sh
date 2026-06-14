#!/usr/bin/env bash
# Generate ALL tour models with textured (refine) passes and drop them into
# Assets/StreamingAssets/Models. Requires MESHY_API_KEY in the environment.
#
#   export MESHY_API_KEY="msy_..."
#   tools/generate_all.sh                # everything, textured
#   tools/generate_all.sh --hd           # everything, HD textures (more credits)
#   tools/generate_all.sh --essential    # only the essential props
set -euo pipefail
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -z "${MESHY_API_KEY:-}" ]; then
  echo "MESHY_API_KEY is not set. Add it as an environment secret, then re-run." >&2
  exit 2
fi

# Default to --all --refine unless the caller overrides the selection.
if [ "$#" -eq 0 ]; then
  exec python3 "$DIR/meshy_generate.py" --all --refine
else
  exec python3 "$DIR/meshy_generate.py" --refine "$@"
fi
