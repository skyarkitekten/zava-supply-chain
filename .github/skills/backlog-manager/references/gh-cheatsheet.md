# gh CLI Cheatsheet

Substitute `$REPO`, `$OWNER`, and `$PROJECT` with the values resolved in the SKILL's [Setup](../SKILL.md). Prefer GitHub MCP tools when available; use these as fallback or for Projects v2 (which MCP issue tools don't cover).

> Tip: export them in your shell first so the snippets below run as-is:
> ```bash
> REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
> OWNER=${REPO%%/*}
> PROJECT=<number>   # from: gh project list --owner "$OWNER"
> ```

## Issues

```bash
# List untriaged (no priority label)
gh issue list --repo "$REPO" --search 'is:open -label:"priority: p0" -label:"priority: p1" -label:"priority: p2" -label:"priority: p3"'

# Read an issue (body + labels + comments)
gh issue view 42 --repo "$REPO" --comments

# Create
gh issue create --repo "$REPO" \
  --title "Fix inventory lookup timeout" \
  --body-file /tmp/issue.md \
  --label "bug,priority: p1,area: lab01"

# Label / assign
gh issue edit 42 --repo "$REPO" --add-label "priority: p1,area: lab01"
gh issue edit 42 --repo "$REPO" --add-assignee "$OWNER"

# Comment
gh issue comment 42 --repo "$REPO" --body "Needs acceptance criteria before it's ready."

# Stale (no update in 30+ days) — substitute the date
gh issue list --repo "$REPO" --search 'is:open updated:<2026-05-08' --json number,title,updatedAt
```

## Labels

```bash
gh label list --repo "$REPO"
gh label create "priority: p1" --color d93f0b --description "Next up" --repo "$REPO"
```

## Projects v2 (needs read:project / project scope)

```bash
# If any project command errors on scope:
gh auth refresh -s read:project,project

# Find the project number for this owner
gh project list --owner "$OWNER"

# List items on the board
gh project item-list "$PROJECT" --owner "$OWNER"

# Add an issue to the board
gh project item-add "$PROJECT" --owner "$OWNER" --url "https://github.com/$REPO/issues/42"

# Inspect fields and their option ids
gh project field-list "$PROJECT" --owner "$OWNER"

# Set a single-select field (Status / Priority) on an item
gh project item-edit --id <ITEM_ID> --project-id <PROJECT_ID> \
  --field-id <FIELD_ID> --single-select-option-id <OPTION_ID>
```

> Item/field/option ids come from `item-list` and `field-list` (add `--format json` for machine-readable output).
