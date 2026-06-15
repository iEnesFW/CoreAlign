# Release Process

This document describes how CoreAlign cuts releases. It covers SemVer rules,
branch and tag conventions, and the changelog update procedure.

## 1. Versioning (SemVer 2.0.0)

CoreAlign follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html):

| Segment | Increment when...                                                                              | Examples           |
| ------- | ---------------------------------------------------------------------------------------------- | ------------------ |
| `MAJOR` | A public, breaking change is shipped (API contract removal, DB column drop, persona reshape).  | `1.0.0` -> `2.0.0` |
| `MINOR` | A new backwards-compatible capability is added (new endpoint, new module, new optional field). | `1.4.0` -> `1.5.0` |
| `PATCH` | Backwards-compatible bug fixes, performance tweaks, internal refactors.                        | `1.4.2` -> `1.4.3` |

Pre-release suffixes are allowed for staged rollouts:

- `vX.Y.Z-rc.1` — release candidate cut from a `release/X.Y` branch.
- `vX.Y.Z-beta.1` — early access build, customer-portal gated by feature flag.

Build metadata (`+sha.<short>`) is optional and never affects ordering.

## 2. Branch Conventions

| Branch                                              | Purpose                                                                    | Lifetime                                             |
| --------------------------------------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------- |
| `main`                                              | Always deployable, always green CI.                                        | Permanent.                                           |
| `release/X.Y`                                       | Stabilisation branch for a minor line. Cherry-picks for patches land here. | Permanent for active minor lines; archived when EOL. |
| `feat/<ticket>-short-desc`                          | Feature work, opened against `main`.                                       | Deleted on merge.                                    |
| `fix/<ticket>-short-desc`                           | Bug fix work.                                                              | Deleted on merge.                                    |
| `chore/...`, `docs/...`, `refactor/...`, `test/...` | Categorised non-feature work.                                              | Deleted on merge.                                    |

Rules:

- Never push to `main` directly. All changes flow through PR (see `docs/branch-protection.md`).
- Force-push is forbidden on `main` and `release/*`.
- Release branches are created by the release manager once a minor line is feature-frozen: `git switch -c release/0.8 main`.

## 3. Tag Conventions

- Production tags are immutable and prefixed with `v`: `v0.7.0`, `v0.7.1`, `v1.0.0-rc.2`.
- Tags are always annotated and signed:
  ```bash
  git tag -s v0.7.1 -m "Release 0.7.1"
  git push origin v0.7.1
  ```
- A tag triggers `release.yml` which builds the artifact set and drafts a GitHub Release populated from the matching `CHANGELOG.md` section.

## 4. Changelog Update Procedure

`CHANGELOG.md` lives at the repo root in [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/) format.

For every PR that touches user-visible behaviour:

1. Append a bullet under the `## [Unreleased]` section in the matching category (`Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`).
2. Keep the bullet customer-facing: describe the outcome, not the implementation. Avoid file paths and class names.
3. Reference the ticket id in parentheses where useful: `(STOCK-014)`.

When cutting a release:

1. Decide the version bump (SemVer table above).
2. In a PR titled `chore(release): vX.Y.Z`:
   - Rename `## [Unreleased]` to `## [X.Y.Z] - YYYY-MM-DD`.
   - Add a fresh empty `## [Unreleased]` block at the top.
   - Update the compare-link footnotes at the bottom of the file.
3. Merge the PR.
4. From `main` (or `release/X.Y`), create the tag and push:
   ```bash
   git switch main
   git pull --ff-only
   git tag -s vX.Y.Z -m "Release X.Y.Z"
   git push origin vX.Y.Z
   ```
5. Verify the GitHub Release was drafted by the `release.yml` workflow and publish it.

## 5. Hotfix Flow

1. Branch from the relevant `release/X.Y`: `git switch -c fix/PATCH-123-bad-rounding release/0.7`.
2. Land fix via PR targeting the release branch.
3. Cut `vX.Y.Z+1` from the release branch.
4. Cherry-pick the fix forward into `main` if applicable.

## 6. Yank / Deprecation

To yank a broken release:

1. Add a `### Yanked` note under the affected version in `CHANGELOG.md` with reason and recommended replacement.
2. Mark the GitHub Release as a pre-release (do not delete the tag).
3. Communicate via release notes and customer-portal banner.
