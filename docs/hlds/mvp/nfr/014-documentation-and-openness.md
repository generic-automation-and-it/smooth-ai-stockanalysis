# NFR-083 – NFR-089: Documentation and openness

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-083 | The repository is public and written for an external reader | Public | High |
| NFR-084 | The README is sufficient for a competent third party to install and operate the product | Self-sufficient | High |
| NFR-085 | Every external provider required is listed, with a link to create an account | Complete list | High |
| NFR-086 | A disclaimer states that this is a personal research tool and not financial advice | Prominent | Critical |
| NFR-087 | Documentation is in a visible folder and linked from the README | Discoverable | High |
| NFR-088 | Interactive API documentation is generated and served by the application | Available locally | Medium |
| NFR-089 | Architecture diagrams render natively in the hosted repository view | No build step | Medium |

## Rationale

NFR-086 is the only requirement in this document that is not about convenience. The repository publishes software that produces stock recommendations. Anyone finding it must be told plainly what it is and what it is not, and that responsibility for any investment decision remains entirely with the person making it. The disclaimer is a requirement, not a courtesy.

NFR-085 is what makes NFR-084 achievable. The product depends on six or more third-party services, several requiring registration before anything works. Naming them without linking to registration leaves a reader to guess which of several similarly-named products is meant.

NFR-087 is the reason the documentation folder was made visible rather than hidden. The business requirements and design are precisely what an arriving reader wants, and a hidden folder buries them.

## Verification

- Installation followed from the README on a clean device by someone who did not write it.
- Diagram rendering confirmed in the hosted view.

## Related

- `docs/hlds/mvp/ladrs/007-visible-docs-folder.md`
- BR-49, BR-50
