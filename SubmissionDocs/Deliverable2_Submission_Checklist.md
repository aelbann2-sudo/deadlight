# Deliverable 2 Submission Checklist

## Written material (submit what the brief asks for)

- **Primary:** `SubmissionDocs/Deliverable2_MidProject_Report.pdf` (compile from `Deliverable2_MidProject_Report.tex`)
- **Mirror:** `SubmissionDocs/Deliverable2_MidProject_Report.md` (same content as PDF; for graders who want Markdown)
- **Supplemental:** `Deliverable2_Narrative_Worldbuilding_Appendix.md`, `Deliverable2_Testing_Iteration_Notes.md`

Compile PDF (from `SubmissionDocs/`):

```bash
pdflatex -interaction=nonstopmode Deliverable2_MidProject_Report.tex
pdflatex -interaction=nonstopmode Deliverable2_MidProject_Report.tex
```

## Unity project (required folders)

- `Assets`
- `Packages`
- `ProjectSettings`

## Windows build (required by assignment)

1. On a machine with **Windows Build Support** installed, open the project in Unity.
2. Menu: **`Deadlight` → `Build Windows`** (`Assets/Editor/DeadlightMenuItems.cs`).
3. Smoke-test the `.exe` (menu, L1, L2, victory path).
4. Include the build output folder in the submission zip **or** as a separate upload per course instructions.

## Submission zip (exclude junk)

From repo root:

```bash
bash SubmissionDocs/package_deliverable2_zip.sh
```

**Exclude** these from any manual zip (they bloat or are machine-local):

- `Library`
- `Logs`
- `Temp`
- `UserSettings` (optional; often excluded for team hand-ins)

If the brief requires a **Windows build inside the zip**, add that folder and re-zip, or run `zip -r` once with project + `BuildWindows` (or your output folder name).

## Validation artifacts (optional but recommended)

- `tmp/runtime_smoke_report.txt` — checklist; fill after testing the submission build.
- `tmp/build_mac.log` — placeholder; replace with real Unity log snippet if instructors want build proof.

These are **not** a substitute for running the game; they document that someone verified the build.

## Pre-flight before upload

- [ ] PDF opens; figures render (`d2_fig_*.png` present in `SubmissionDocs/`)
- [ ] Windows build runs on a clean Windows machine (or VM)
- [ ] Zip does not contain `Library/` or `Temp/`
- [ ] Report matches scope: **Levels 1–2** playable; crafting **off** in this milestone (`enableCrafting = false`)
