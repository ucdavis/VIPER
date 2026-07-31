# HOOKS.md

Claude Code hooks run shell commands at fixed points in the agent lifecycle, so
they enforce conventions that a prompt rule can only request. **This repo ships
none**, deliberately. Everything under `.claude/` is gitignored and personal,
including `settings.local.json` (your permission allowlist, MCP servers, default
mode). Run `/hooks` in Claude Code to see what you have configured locally.

This file exists because hooks were a tempting answer to two problems here. One
had a better answer a layer down; the other was not worth enforcing. Both are
written up below so the same ground is not retrodden. If you add a hook anyway,
the gotchas at the bottom cost real debugging time to find.

## Prefer a mechanism one layer down

A hook is the wrong place to enforce something git can enforce itself. It runs
only on this tool's writes, needs external binaries installed, and fails open by
convention, so it can stop working with no sign at all.

Line endings are the worked example. The agent's Write and Edit tools emit bare
LF while this working tree is CRLF, which looks like it needs normalizing on
every write, and a `PostToolUse` hook running `unix2dos` was the first answer.
`.gitattributes` is the better one.

`* text=auto` normalizes every text file to LF in the repository on commit,
whatever the working copy happens to have. That beats a hook on every axis: no
external tools to install, no extension allowlist to keep current, it covers
files the agent never touches, it applies to teammates whose `core.autocrlf` is
unset, and it cannot silently stop working. Working-tree endings still follow
local git config, so Windows checkouts stay CRLF.

`*.sh` and `.husky/*` are pinned to `eol=lf`, matching `.editorconfig`, because
Jenkins runs the husky hooks on Linux where a CR on the shebang line breaks
execution.

## Do not gate staging

A `PreToolUse` hook returning `permissionDecision: "ask"` on `git add` was tried
here and removed. It forces a prompt that deliberately overrides any allowlist
entry, so every commit stalls waiting for a human, and an unattended run cannot
answer at all. That forecloses goal-directed agentic sessions, which are worth
more than the failure it guarded against: a premature commit on an unmerged
branch costs an amend.

The concrete hazard it named, sweeping `PLAN-*.md` or `SMOKETEST-*.md` into a
commit, is already handled deterministically by `.gitignore`.

## Gotchas

These all fail with exit code 0. **Check the side effect, never the exit code.**

**`jq` writes CRLF to stdout on Windows.** Any hook that reads the tool payload
with `jq -r ... | { read -r f; ... }` gets a trailing `\r` on `$f`, and every
downstream use silently misbehaves: a `case "$f" in *.cs)` stops matching and the
hook does nothing at all. Always pipe through `tr -d '\r'` first.

**`git check-ignore -v` exits 0 for negated matches too.** So a path rescued by a
`!pattern` rule is reported as though it were ignored. Use the quiet form (`-q`)
to test ignored-ness, and `-v` only to name the rule afterwards.

**Do not test a hook command with `eval`.** `eval "$CMD"` expands `$f` in the outer
shell before the command runs, so `case "$f"` sees an empty string and matches
nothing. Write the command to a file and run `bash file.sh` instead.

**Hooks run under Git Bash, not PowerShell.** Use POSIX syntax.

To check a file's line endings, ask git rather than `file`, which reports "CRLF"
even when only some lines are:

```bash
git ls-files --eol path/to/file   # i/ = repository, w/ = working tree
```
