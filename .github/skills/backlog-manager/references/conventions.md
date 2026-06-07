# Backlog Conventions

Full taxonomy and rationale for the `backlog-manager` skill. The SKILL.md enforces these; this file is the detail.

## Type labels (exactly one per issue)

| Label           | Use for                                          | Color     |
| --------------- | ------------------------------------------------ | --------- |
| `bug`           | Something is broken / not working as intended    | `#d73a4a` |
| `enhancement`   | New feature, capability, or improvement          | `#a2eeef` |
| `documentation` | Docs, READMEs, lab instructions                  | `#0075ca` |
| `question`      | Needs discussion/decision before it's actionable | `#d876e3` |

These match the repo's existing default labels — reuse them, don't duplicate.

## Priority labels (exactly one per issue)

| Label          | Meaning                | Color     |
| -------------- | ---------------------- | --------- |
| `priority: p0` | Blocking / do now      | `#b60205` |
| `priority: p1` | Next up                | `#d93f0b` |
| `priority: p2` | Soon                   | `#fbca04` |
| `priority: p3` | Someday / nice-to-have | `#0e8a16` |

Always mirror the label into the Project board `Priority` field so reports stay consistent.

Create a missing priority label with (`$REPO` resolved in Setup):

```bash
gh label create "priority: p1" --color d93f0b --description "Next up" --repo "$REPO"
```

## Area labels (one or more)

Area maps to whatever top-level component or folder the work touches. Define the set that fits the repo on first use; a good default is one `area: <name>` label per top-level source folder, plus:

| Label         | Scope                                   |
| ------------- | --------------------------------------- |
| `area: <x>`   | A top-level component/folder (repeat per area) |
| `area: data`  | Shared data / fixtures                  |
| `area: docs`  | Top-level docs / READMEs                |
| `area: infra` | Tooling, CI, project plumbing           |

Suggested color for all area labels: `#5319e7`. Record the chosen area set in `/memories/repo/backlog.md` so it stays consistent across runs.

## Status (Project board field — never a label)

`Triage` → `Todo` → `In Progress` → `In Review` → `Done`

- `Triage`: not yet meeting Definition of Ready.
- `Todo`: ready, prioritized, unassigned/assigned.
- `In Progress` / `In Review`: actively being worked / in PR.
- `Done`: merged/closed and verified.

## Definition of Ready (DoR)

An issue is **ready** (eligible for `Status: Todo`) when it has:

1. A clear, imperative title.
2. Problem/context in the body.
3. A checklist of **acceptance criteria**.
4. Exactly one **Type** label.
5. Exactly one **Priority** label.
6. At least one **Area** label.

If any are missing: keep it in `Triage`, label `needs-triage`, and comment listing the gaps.

## Definition of Done (DoD)

- Acceptance criteria all checked.
- PR merged with a closing keyword (`Closes #N`).
- Board item moved to `Done`.
