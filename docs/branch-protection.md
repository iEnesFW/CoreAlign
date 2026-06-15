# Branch Protection Rules

This document captures the branch protection configuration applied to the CoreAlign GitHub repository.
Settings are administrative and must be enforced via the GitHub UI (Settings -> Branches -> Branch
protection rules) or via `gh api` calls. They are described here so the policy is auditable in source
control and reviewable via PR like any other change.

## Protected Branches

| Pattern     | Purpose                                        |
| ----------- | ---------------------------------------------- |
| `main`      | Default branch. Always deployable.             |
| `release/*` | Stabilisation branches for active minor lines. |

## Rules Applied to `main`

1. **Require a pull request before merging.**
   - Required approving reviews: **1**.
   - Dismiss stale pull request approvals when new commits are pushed: **on**.
   - Require review from Code Owners: **on** (uses `.github/CODEOWNERS`).
   - Require approval of the most recent reviewable push: **on**.

2. **Require status checks to pass before merging.**
   - Require branches to be up to date before merging: **on**.
   - Required status checks (must match job names in `.github/workflows/ci.yml`):
     - `Frontend (lint / typecheck / test / build)`
     - `Backend (.NET 10 build + test)`
     - `Coverage gate`

3. **Require conversation resolution before merging.** All review threads must be resolved.

4. **Require signed commits.** Aligns with the signed tags requirement in `docs/release-process.md`.

5. **Require linear history.** Forces squash or rebase merges; no merge commits on `main`.

6. **Do not allow bypassing the above settings.** Admins included.

7. **Restrict who can push to matching branches.** Only the `corealign/maintainers` team may push (in practice only via PR merge).

8. **Rules applied to everyone including administrators.**

9. **Allow force pushes:** **off**.

10. **Allow deletions:** **off**.

## Rules Applied to `release/*`

Same as `main`, with the following adjustments:

- Required approving reviews: **2** (release branches are higher risk).
- Force pushes: **off**.
- Deletions: **off**.
- Required status checks: same list as `main`.

## Tag Protection

Pattern `v*` (production release tags):

- Only `corealign/maintainers` may create or delete matching tags.
- Aligns with `docs/release-process.md` signed-tag requirement.

## How to Apply

UI: `Settings -> Branches -> Add branch ruleset` and replicate the bullets above.

CLI alternative (requires `gh` >= 2.40 and admin privileges):

```bash
gh api -X PUT repos/corealign/corealign/branches/main/protection \
  --input docs/branch-protection-main.json
```

A `branch-protection-main.json` companion file may be added later; until then the UI is the source of truth.

## Change Procedure

To modify branch protection:

1. Open a PR updating this document with the proposed change and rationale.
2. After approval, an org admin applies the change in the GitHub UI.
3. Reference the PR number in the audit-log entry (Settings -> Audit log on the org).
