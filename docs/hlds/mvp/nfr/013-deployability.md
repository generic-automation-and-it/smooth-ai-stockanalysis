# NFR-076 – NFR-082: Deployability and operations

**Status:** Accepted · **Date:** July 2026

## Requirements

| ID | Requirement | Target | Priority |
|---|---|---|---|
| NFR-076 | Deployment to the target device is a single automated release | One step | Critical |
| NFR-077 | The service starts on boot and restarts automatically on failure | Unattended | Critical |
| NFR-078 | The system runs locally for development and debugging | Same code, no orchestration | Critical |
| NFR-079 | Local operation requires no container runtime or external service | Self-contained | Critical |
| NFR-080 | Configuration and credentials are provisioned on the device without redeployment | Independent of the artifact | High |
| NFR-081 | Rollback to the previous release is documented and tested | Documented procedure | Medium |
| NFR-082 | A health endpoint reports whether the service is operational | Available on the local network | Medium |

## Rationale

NFR-078 and NFR-079 were specified explicitly by the owner and shape more than they appear to. Requiring a container runtime for local development would have pulled the orchestration tooling — and with it the server database — back into the design, undoing the storage decision. Keeping local operation self-contained is what keeps development and production genuinely alike on a 1 GB device.

NFR-080 separates the artifact from its secrets, which matters because the repository is public and the artifact may be too. Credentials live on the device and are provisioned once.

NFR-077 is the practical definition of unattended. Domestic hardware loses power, and a service that does not return after a reboot has failed regardless of how well it ran beforehand.

## Verification

- Deployment executed end to end to the device from a release.
- Device power-cycled and automatic recovery asserted.
- Rollback executed at least once.

## Related

- BR-38 (own hardware, no hosting cost), BR-40 (manual trigger)
- `docs/adr/006-one-time-fork-of-template.md`
