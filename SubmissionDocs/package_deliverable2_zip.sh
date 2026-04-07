#!/usr/bin/env bash
# Create a course-style submission zip: Unity project roots + SubmissionDocs,
# excluding Library/Logs/Temp and other bulky or local-only paths.
#
# Usage (from repo root):
#   bash SubmissionDocs/package_deliverable2_zip.sh
#
# After running, add your Windows build folder manually if required, e.g.:
#   zip -r ../Deadlight_D2_Submission.zip BuildWindows -x "*.DS_Store"
#
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$REPO_ROOT"

STAMP="$(date +%Y%m%d_%H%M)"
OUTZIP="${REPO_ROOT}/Deadlight_Group16_Deliverable2_${STAMP}.zip"

echo "Writing: $OUTZIP"

zip -r "$OUTZIP" \
  Assets \
  Packages \
  ProjectSettings \
  SubmissionDocs \
  -x "*/.DS_Store" \
  -x "*/*.tmp" \
  -x "SubmissionDocs/*.aux" \
  -x "SubmissionDocs/*.log" \
  -x "SubmissionDocs/*.out" \
  -x "SubmissionDocs/*.toc" \
  -x "SubmissionDocs/*.synctex.gz"

# Optional validation templates (small; omit entire tmp/ tree)
for f in tmp/runtime_smoke_report.txt tmp/build_mac.log; do
  if [[ -f "$REPO_ROOT/$f" ]]; then
    zip -j "$OUTZIP" "$REPO_ROOT/$f"
  fi
done

echo ""
echo "Done. Excluded (not in zip): Library/, Logs/, Temp/, UserSettings/ — add Windows build + re-zip if your brief requires it."
echo "Suggested: unzip -l \"$OUTZIP\" | head"
