# NFR-037 – NFR-044: Security and data isolation

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-037 | No authentication or authorization in Phase 1 | Explicit, deliberate | Accepted risk |
| NFR-038 | The system is reachable only from the private home network | No external exposure | Critical |
| NFR-039 | User-owned data is isolated at the data-access layer, not per feature | Global filter | Critical |
| NFR-040 | Shared reference data is explicitly exempt from isolation | Documented exemption | Critical |
| NFR-041 | Background execution sets user scope explicitly; there is no ambient user | Explicit scope required | Critical |
| NFR-042 | Ingestion runs under a deliberate system scope that bypasses isolation | Separate, named, auditable | Critical |
| NFR-043 | Credentials are read from environment variables and never committed | Zero secrets in the repository | Critical |
| NFR-044 | Committed configuration carries placeholders so shape is documented without values | Placeholders only | High |

## Rationale

Two facts must be held together: the system has no access control, and its source code is public. NFR-043 and NFR-044 follow directly — a leaked provider key in a public repository is the single most likely security incident this project will ever have, and it is entirely preventable.

NFR-037 is recorded as an accepted risk rather than an omission. It is defensible only because of NFR-038. If the system is ever exposed beyond the home network, this requirement is void and authentication becomes mandatory.

NFR-041 and NFR-042 identify where isolation actually leaks in systems like this one. Everything runs in a background worker on a timer, so there is no request to infer a user from. A filter that silently applies to nobody is worse than no filter, because it looks like protection. Both scopes are therefore explicit and named.

## Future

Phase 3 introduces sign-in and role-based access control. Because user identity and isolation exist from the first release, that work adds a front door rather than a data model.

## Verification

- Isolation tested by querying user-owned data as a different user.
- Shared data asserted *not* to be filtered — the inverse test matters as much as the direct one.
- Repository scanned for committed credentials as part of the build — implemented as a named **Secret scan** step in the PR gate (`gitleaks`, scan.sh, `.gitleaks.toml`). The scan covers the **PR commit range**, not the full repository history on every run (a key committed and removed within the same PR is still caught); pre-PR history and paths outside the gate's path filter are a documented blind spot covered by a one-off full scan and GitHub's native secret scanning. See `.github/CI_AGENTS.md` "Secret scanning" for the tool, pinning, scope, and blind spots; the scan's allowlist is reviewable in `.gitleaks.toml` (every entry commented). The L0 `CommittedConfigurationGuardTests` complement it by scanning committed `appsettings.json` for secret-shaped literals on every `dotnet test`.

## Related

- `docs/adr/010-user-identity-from-first-release.md`
- BR-47, BR-48
