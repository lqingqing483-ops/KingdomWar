# Change Log System

## Structure

```
.opencode/changelog/
├── changelog.ps1        # Script to append records (append-only)
├── README.md            # This file
├── 2026-07-05.jsonl     # Daily log files (automatically created)
└── ...
```

## Format

Each record is a single JSON line (JSONL format):

```json
{"id":"chg_12345678-143000","timestamp":"2026-07-05T14:30:00Z","agent":"build-1","task":"Implement login","action":"create","files":["src/auth/login.cs"],"status":"passed","detail":""}
```

## Usage

```powershell
# Record operation start
.opencode\changelog\changelog.ps1 -Agent "build-1" -Task "Task description" -Action "modify" -Files "file1.cs,file2.cs" -Status "started"

# Record operation result
.opencode\changelog\changelog.ps1 -Agent "build-1" -Task "Task description" -Action "modify" -Files "file1.cs" -Status "passed"
```

## Rules

- **APPEND ONLY** — AI may only add records, never modify or delete
- AI edit permissions should deny the changelog directory
- Use for tracing: given a bug, search changelog for affected files to find the root cause
