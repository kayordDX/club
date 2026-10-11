# Issue tracker: GitHub

Issues and specs live in GitHub Issues for `kayordDX/club`.
Use the `gh` CLI from this repository.

## Operations

- Create: `gh issue create --title "..." --body "..."`
- Read: `gh issue view <number> --json number,title,body,labels,comments`
- List: `gh issue list --state open --json number,title,body,labels`
- Comment: `gh issue comment <number> --body "..."`
- Label: `gh issue edit <number> --add-label "..." --remove-label "..."`
- Claim: `gh issue edit <number> --add-assignee @me`
- Close: `gh issue close <number> --comment "..."`

For multiline bodies, use `--body-file` with a Markdown file.

“Publish to the issue tracker” means create a GitHub issue.
“Fetch the relevant ticket” means read the GitHub issue.

## Parent and blocking relationships

Use native GitHub sub-issues and issue dependencies when available.

- Sub-issue (`gh` 2.94+): `gh issue edit <parent> --add-sub-issue <child>`
- Blocker database ID: `gh api repos/kayordDX/club/issues/<blocker> --jq .id`
- Add dependency: `gh api --method POST repos/kayordDX/club/issues/<child>/dependencies/blocked_by -F issue_id=<blocker-db-id>`

If unavailable, use `Part of #<parent>` and `Blocked by: #<number>`
in child issue bodies, and a child task list in the parent.
A ticket is ready only when all blockers are closed.

## Wayfinding

Use a parent issue labelled `wayfinder:map` and linked child issues
labelled `wayfinder:research`, `wayfinder:prototype`,
`wayfinder:grilling`, or `wayfinder:task`.
Select unassigned, unblocked open children in map order.
Claim before working; record the answer, close the child, and
append a decision summary and link to the map.

## Pull requests as a triage surface

PRs as a request surface: no.
