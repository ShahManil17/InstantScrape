---
name: clickup-ticket-delivery
description: Execute full delivery workflow from prompts like "do task for ticketId XXX". Use when a user provides a ClickUp task ID and expects end-to-end implementation: read task details, create a branch from main named {ticketId}_{task-name-slug}, implement changes, run syntax/build checks and fix errors, commit and push, merge into development if conflict-free, push development, and update ClickUp status to READY TO TEST.
---

# ClickUp Ticket Delivery

Run this workflow when the user asks to perform a task by ticket ID.

## Inputs

- `ticketId`: required ClickUp task ID (for example `DEV-1234` or numeric ID).
- Optional constraints from user message: scope limits, commit style, testing limits.

## Workflow

1. Parse `ticketId` from the user prompt.
2. Read the ClickUp task:
   - Call `clickup_get_task` with `task_id=ticketId`.
   - Extract task title, description, acceptance details, and current status.
3. Build branch name:
   - Start with `{ticketId}_`.
   - Append a slug from task title: lowercase, alphanumeric and hyphens only, collapse repeated separators, max 15 chars.
   - Final pattern: `{ticketId}_{slug}`.
4. Prepare Git base:
   - Ensure there are no uncommitted changes in the repository; if there are, put that in the stash with the appropriate name.
   - Ensure repository is available and not in the middle of merge/rebase/cherry-pick.
   - `git fetch origin`
   - `git checkout main`
   - `git pull origin main`
5. Create feature branch:
   - `git checkout -b {ticketId}_{slug}`
6. Implement ticket requirements:
   - Apply code changes strictly tied to the ClickUp task.
   - Keep edits minimal and production-safe.
7. Validate syntax/build:
   - For .NET repos: run `dotnet build` on the solution/project in Debug.
   - If errors appear, fix and rerun until build passes or a hard blocker is found.
8. Commit and push branch:
   - `git add -A`
   - Commit message format: `{ticketId}: <short task action>`
   - `git push -u origin {ticketId}_{slug}`
9. Merge into development:
   - `git checkout development`
   - `git pull origin development`
   - `git merge --no-ff {ticketId}_{slug}`
   - If conflicts occur, stop and report conflict files; do not force merge.
   - If merge succeeds: `git push origin development`
10. Update ClickUp task:
   - Call `clickup_update_task` with `task_id=ticketId` and `status=READY TO TEST`.
11. Report result:
   - Branch name, commit hash, build status, merge status, and task status update result.

## Conflict and Failure Rules

- If branch creation fails because branch exists:
  - Checkout existing branch and continue only if it is clearly for the same ticket.
- If build fails and cannot be resolved confidently:
  - Do not merge.
  - Push branch only if user asks; otherwise stop with clear blocker summary.
- If merge conflicts occur:
  - Do not push partial merge and end the execution.
- If ClickUp status update fails after successful merge:
  - Report merge success separately and include ClickUp API failure details.

## Command Defaults

- Git remote: `origin`
- Base branch: `main`
- Target branch for integration: `development`
- Required ClickUp target status: `READY TO TEST`

