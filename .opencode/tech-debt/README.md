# Technical Debt Tracking

## Structure

```
.opencode/tech-debt/
├── README.md
├── rules.yaml                 # Auto-detection rules
└── items.json                 # Current debt items
```

## Detection Rules

```yaml
# .opencode/tech-debt/rules.yaml
detection:
  hardcoded_values:
    enabled: true
    threshold: 3  # >3 magic numbers per file = debt item
    
  long_function:
    enabled: true
    max_lines: 50
    max_complexity: 10
    
  missing_tests:
    enabled: true
    min_coverage: 60
    
  duplicate_code:
    enabled: true
    min_lines: 10
    similarity: 0.8
```

## Debt Item Format

```json
{
  "id": "TD-001",
  "type": "long_function",
  "file": "src/game/player.cs",
  "line": 120,
  "description": "Update() function is 80 lines, handles too many concerns",
  "severity": "medium",
  "created_by": "build-1",
  "created_at": "2026-07-05",
  "score": 42
}
```

## Score Guide
| Score | Level | Action |
|-------|-------|--------|
| 0-20  | Green | Monitor |
| 21-50 | Yellow | Plan to fix |
| 51-80 | Orange | Must fix next sprint |
| 81-100| Red | Stop features, fix now |
