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
  -x "Assets/_Recovery/*" \
  -x "Assets/_Recovery.meta" \
  -x "SubmissionDocs/Deliverable 3 - Copy.pdf" \
  -x "SubmissionDocs/*.docx" \
  -x "SubmissionDocs/*.aux" \
  -x "SubmissionDocs/*.log" \
  -x "SubmissionDocs/*.out" \
  -x "SubmissionDocs/*.toc" \
  -x "SubmissionDocs/*.synctex.gz"

echo ""
echo "Done. Excluded (not in zip): Library/, Logs/, Temp/, UserSettings/, placeholder validation files — add Windows build + any real smoke-test notes if your brief requires them."
echo "Suggested: unzip -l \"$OUTZIP\" | head"
