---
name: backlog-manager
description: 'Manage a project backlog where GitHub Issues + GitHub Projects are the single source of truth. USE FOR: triaging new issues (label, assign, prioritize), grooming/refining items (clarify, add acceptance criteria, definition of ready), creating issues from requests or conversations, adding/moving items on the Project board (Status, Priority, Iteration), reporting backlog state (what is ready, stale, blocked, in progress), and linking issues to PRs or parent/sub-issues. Trigger phrases: "manage the backlog", "triage issues", "groom the backlog", "what should we work on next", "add this to the board", "file an issue", "backlog report", "is this ready". DO NOT USE FOR: writing feature code, general git operations, or CI/CD configuration.'
argument-hint: 'What backlog action? e.g. "triage new issues", "groom #42", "backlog report", "file an issue for X"'
---

# Backlog Manager

GitHub Issues + GitHub Projects are the **single source of truth** for the backlog. Never track work in scratch files, comments-as-tasks, or local notes — everything lives as an Issue and (when actionable) a Project board item.

## Configuration

This skill is portable: it reads its target from these variables instead of hardcoded values. Resolve them once via [Setup](#0-setup-resolve-the-configuration) and cache them in repo memory. Throughout this skill, substitute:

| Variable   | Meaning                           | How resolved                                                           |
| ---------- | --------------------------------- | ---------------------------------------------------------------------- |
| `$REPO`    | `owner/name` of the backlog repo  | `gh repo view --json nameWithOwner` or the git `origin` remote         |
| `$OWNER`   | Project owner (user or org login) | owner segment of `$REPO`, unless the board lives under a different org |
| `$PROJECT` | Project (v2) number for the board | `gh project list --owner $OWNER`                                       |

Wherever you see `$REPO`, `$OWNER`, or `$PROJECT` in this skill or its references, replace them with the resolved values.

## Core Rules

1. **Source of truth is GitHub.** Read current state from Issues/Project before acting; do not rely on stale context. Write every decision back as labels, field values, comments, or links.
2. **One unit of work = one Issue.** If a request implies multiple deliverables, split into separate issues (use parent/sub-issues to keep them linked).
3. **Actionable issues belong on the board.** Every issue that is `ready` or beyond must be a Project item with `Status` and `Priority` set.
4. **Confirm before mutating shared state.** Creating/closing issues, bulk re-labeling, or moving many board items: state the plan and get a thumbs-up first. Single, clearly-requested edits can proceed directly.
5. **Prefer MCP, fall back to gh CLI.** See [Tooling](#tooling).

## Conventions

These are the enforced conventions. Full taxonomy and rationale: [references/conventions.md](./references/conventions.md).

- **Type** (exactly one): `bug`, `enhancement`, `documentation`, `question`.
- **Priority** (exactly one): `priority: p0` (now/blocking) · `p1` (next) · `p2` (soon) · `p3` (someday). Mirror into the Project `Priority` field.
- **Area** (one or more): `area: <name>` per top-level component/folder, plus `area: data`, `area: docs`, `area: infra`. Define the repo's area set on first use and cache it (see conventions reference).
- **Status** lives on the **Project board only** (`Todo` → `In Progress` → `In Review` → `Done`), never as a label.
- **Definition of Ready (DoR):** clear title, problem/context, acceptance criteria, one Type, one Priority, ≥1 Area. Items meeting DoR get `Status: Todo`; otherwise keep in `Triage`/backlog and add what's missing.

If a required label does not exist yet, create it (see conventions reference for colors), then apply it.

## Tooling

Prefer GitHub MCP tools when available (e.g. `issue_read`, `issue_write`, `add_issue_comment`, `list_issues`, `search_issues`, `sub_issue_write`, `pull_request_read`). Fall back to `gh` CLI otherwise.

**Project (boards) needs the `read:project`/`project` scope.** If a `gh project …` call fails with a missing-scope error, tell the user to run `gh auth refresh -s read:project,project` (do not run auth commands silently). MCP issue tools do **not** cover Projects v2 — board reads/writes go through `gh project …` or the GraphQL API.

Common gh commands: [references/gh-cheatsheet.md](./references/gh-cheatsheet.md).

## 0. Setup (resolve the configuration)

Do this the first time the skill runs in a workspace, then cache it.

1. Check repo memory `/memories/repo/backlog.md` for recorded `$REPO`, `$OWNER`, and `$PROJECT`. If all present, use them and skip the rest.
2. Resolve `$REPO` and `$OWNER`:
   ```bash
   gh repo view --json nameWithOwner -q .nameWithOwner   # -> $REPO
   ```
   `$OWNER` is the part before `/` (unless the user says the board lives under a different org).
3. Resolve `$PROJECT`:
   ```bash
   gh project list --owner $OWNER
   ```
   Pick the backlog project and note its **number**. If there are several, ask the user which one.
4. Record all three to repo memory so future runs skip discovery:
   ```
   /memories/repo/backlog.md
   - $REPO: <owner/name>
   - $OWNER: <login>
   - $PROJECT: <number>
   ```
5. If a project command fails on scope, surface the `gh auth refresh -s read:project,project` instruction and stop until resolved.

## Workflows

Pick the workflow matching the user's intent.

### A. Triage new issues

1. List untriaged issues: open issues with no Priority label (or labeled `needs-triage`).
2. For each: read it, then set exactly one **Type**, one **Priority**, and ≥1 **Area**. Assign an owner only if the user names one.
3. If it meets the **Definition of Ready**, add it to the Project with `Status: Todo` and mirror Priority. Otherwise leave a comment listing what's missing and label `needs-triage`.
4. Summarize: counts by priority, and any items that couldn't be triaged.

### B. Groom / refine an item

1. Read the issue (`issue_read`).
2. Tighten the title (imperative, specific). Ensure the body has: problem/context, proposed approach (if known), and a checklist of **acceptance criteria**.
3. Verify Type/Priority/Area labels. Split into sub-issues if it's really multiple deliverables (`sub_issue_write`).
4. Post the refined body via edit; note in a comment what changed and confirm it now meets DoR.

### C. Create an issue from a request/conversation

1. Draft title + body using the template in [references/issue-template.md](./references/issue-template.md) (context, acceptance criteria, links).
2. Search existing issues first to avoid duplicates (`search_issues`). If a match exists, comment/link instead of creating.
3. Show the draft, confirm, then create with Type/Priority/Area labels.
4. If actionable, add to the Project board with Status/Priority.

### D. Update the Project board (status / priority / iteration)

1. Resolve config (Setup) and the item's project-item id (`gh project item-list $PROJECT --owner $OWNER`).
2. Set fields with `gh project item-edit` (Status, Priority, Iteration). Keep label and board Priority in sync.
3. Confirm the move and report the new column state.

### E. Backlog report

1. Pull open issues + board items.
2. Produce a concise markdown report:
   - **Ready (Todo):** count + top items by priority.
   - **In progress / In review:** items + assignees.
   - **Blocked:** items labeled blocked or referencing an open dependency.
   - **Stale:** open issues with no update in >30 days (`search_issues` with `updated:<YYYY-MM-DD>`).
   - **Needs triage:** issues missing Priority.
3. End with a recommended "work next" shortlist (highest priority, ready, unassigned).

### F. Link issues to PRs / sub-issues

1. For PR↔issue: ensure the PR body has a closing keyword (`Closes #N`) or add a cross-link comment.
2. For parent/child: use `sub_issue_write` to attach sub-issues to a tracking/epic issue; keep the parent's checklist in sync.

## Completion Checks

Before declaring a backlog task done, verify:

- [ ] Every touched issue has exactly one Type and one Priority, plus ≥1 Area.
- [ ] Actionable issues appear on the Project board with Status + Priority set.
- [ ] No work was recorded outside GitHub (no TODO files / ad-hoc lists).
- [ ] Changes were summarized back to the user with issue numbers/links.
