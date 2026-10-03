---
name: release
description: Commit pending changes, bump the fbognini.WebFramework version and publish it to nuget.org through the tag-triggered workflow
disable-model-invocation: true
user-invocable: true
argument-hint: "[version, e.g. 12.1.0 or 12.1.0-alpha.1]"
allowed-tools: Bash, Read, Edit, Grep, Glob
effort: high
---

# Release fbognini.WebFramework

Commit what is pending, pick the next SemVer version, bump it and publish the package. Publishing is the `publish` workflow in `.github/workflows/publish.yml`: pushing a `v*` tag packs `src/fbognini.WebFramework` with the version taken from the tag and pushes it to nuget.org.

Requested version: `$ARGUMENTS` (empty means: propose one from the changes).

## Current state

- Current branch: !`git branch --show-current`
- Sync with remote: !`git status -sb | head -1`
- Status: !`git status --short`
- Staged diff: !`git diff --cached --stat`
- Unstaged diff: !`git diff --stat`
- Untracked files: !`git ls-files --others --exclude-standard`
- Latest tag: !`git describe --tags --abbrev=0`
- Version in csproj: !`grep -o '<Version>[^<]*' src/fbognini.WebFramework/fbognini.WebFramework.csproj`
- Commits since the latest tag: !`git log --oneline "$(git describe --tags --abbrev=0)..HEAD"`
- Recent commits (for style reference): !`git log --format='%h %s%n%b' -10`

## Instructions

0. **Check the starting point.** Releases are cut from `master`, and in this repo committing straight on `master` is the normal flow.
   - If the current branch is not `master`, stop and ask the user how to proceed.
   - Run `git fetch origin --tags`. If `master` is behind `origin/master`, stop and tell the user: pulling or rebasing is their call.
   - If there are no pending changes and no commits since the latest tag, say there is nothing to release and stop.

1. **Commit the pending changes**, if any.
   - Read the actual diffs and the untracked files to understand what each change does.
   - Group them logically: one commit per coherent unit of work (a feature, a fix, a refactoring, an infrastructure change). Tests go in the same commit as the code they cover. Do NOT over-split: if everything belongs to one task, make one commit.
   - Grouping is per change, not per file: an unrelated hunk often hides inside a file that otherwise belongs to one group. Stage only the hunks of the group: save `git diff -- <file>` to the scratchpad, delete the hunks that belong elsewhere, `git apply --cached <patch>`, then check the result with `git diff --cached`. This leaves the working tree untouched. Never use `git add -p`, it is interactive.
   - Stage files by name with `git add <specific files>`, deletions included.
   - Commit foundational changes first when one group depends on another.
   - Message style, as in the history of this repo: an English summary line in the imperative mood ("Add", "Fix", "Stop", "Let", "Rename") that says WHAT changed. Add a body only when the WHY is not obvious, written as one prose paragraph (see `4741a50` in the log). No Conventional Commits prefixes (`feat:`, `fix:`), no Co-Authored-By, no AI references.
   - Use a HEREDOC for the message:
     ```
     git commit -m "$(cat <<'EOF'
     Summary line here

     Optional body explaining why.
     EOF
     )"
     ```

2. **Pick the version.** Read `git log <latest tag>..HEAD` and `git diff <latest tag>..HEAD -- src/fbognini.WebFramework` to judge the impact on consumers of the package.
   - Major: a public type or member removed, renamed or with a changed signature, a default behavior consumers rely on changed, a target framework dropped. Example: renaming `ApiResult.IsSuccess` to `OK` produced 12.0.0.
   - Minor: a new public type, member or option, a new property in the request log.
   - Patch: fixes only, with no new public surface.
   - A prerelease (`12.1.0-alpha.1`) is allowed when the user asks for one.
   - If `$ARGUMENTS` carries a version, validate it against the rules above and say so if it looks wrong, but let the user decide.
   - The version must be greater than the latest tag, and `v<version>` must not exist locally (`git tag -l`) or on the remote (`git ls-remote --tags origin`).
   - Present the version with a one-line reason per bump-driving change, and wait for the user to confirm it.

3. **Check the scope of the package.** Only `fbognini.WebFramework` is published. `src/fbognini.WebFramework.Logging.MSSqlServer` has its own version line and the workflow never pushes it: if commits since the latest tag touched it, tell the user those changes do not ship with this release.

4. **Verify the build.** Run `dotnet build --configuration Release` and `dotnet test --configuration Release`, the same steps the workflow runs. On failure stop and report the decisive error: a red build here is a red publish later.

5. **Bump the version.** Edit `<Version>` in `src/fbognini.WebFramework/fbognini.WebFramework.csproj`, nothing else, and commit only that file as `Bump version to <version>`.

6. **Ask for the go-ahead before anything leaves the machine.** Show `git log --oneline origin/master..HEAD` and the version, and state plainly that pushing the tag publishes to nuget.org right away: the `nuget` environment has no required reviewer, only a policy that admits `v*` tags. A version published on nuget.org cannot be deleted, only unlisted, and its number can never be reused. Wait for an explicit yes given in this run.

7. **Push `master` and wait for CI.**
   - `git push origin master`.
   - Find the `build` run for the pushed commit with `gh run list --workflow build --branch master --commit <sha> --json databaseId,status` (it may take a few seconds to appear), then `gh run watch <id> --exit-status` with a timeout of at least 10 minutes.
   - If the run fails, show `gh run view <id> --log-failed` trimmed to the decisive lines and stop: do not tag.

8. **Tag and publish.**
   - Create an annotated tag on the bump commit whose message is the bare version, like the existing ones: `git tag -a v<version> -m <version>`.
   - Push that tag alone: `git push origin v<version>`.
   - Find the `publish` run for the tag and watch it with `gh run watch <id> --exit-status`.
   - If it fails, show the failing step and stop. Do not delete or move the tag without asking. After a partial failure the escape hatch is a manual dispatch of the same workflow: `gh workflow run publish -f version=<version>`, safe because the push uses `--skip-duplicate`.

9. **Report.** Show `git log --oneline -5`, the tag, the two run URLs and `https://www.nuget.org/packages/fbognini.WebFramework/<version>` (indexing can take a few minutes). List the consumer-facing changes in two or three lines, including anything consumers must do to opt in, such as registering a new filter.

## Rules

- NEVER use `git add -A` or `git add .`: always add specific files.
- NEVER amend, rebase or force-push.
- NEVER push before the explicit go-ahead of step 6. An approval from an earlier run does not count.
- NEVER use `git push --tags`: push the one release tag by name.
- NEVER move, delete or reuse a version tag that already exists.
- NEVER bump the version of `fbognini.WebFramework.Logging.MSSqlServer` as part of this flow.
- If unsure about grouping, prefer fewer commits over many tiny ones.
