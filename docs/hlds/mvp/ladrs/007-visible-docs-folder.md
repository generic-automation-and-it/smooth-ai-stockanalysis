# LADR-007: Visible documentation folder

**Status:** Accepted
**Date:** July 2026

## Context

The template places documentation in a hidden dot-prefixed folder, with subfolders for wiki pages, architecture decisions and non-functional requirements.

This repository will be public. The business requirements and high level design are precisely what an arriving reader wants, and a hidden folder buries them — invisible in many local file browsers, and easy to miss even on the web.

## Decision

Rename the documentation tree to a **visible** folder. Wholesale, not partially.

## Rationale

The renaming is uncontroversial; the *wholesale* part is the actual decision. Splitting new documents into a visible folder while decision records and requirements stayed hidden would leave two documentation trees — and within a month, things filed in the wrong one and a distinction nobody can articulate. A visible folder is also the stronger convention: it is what static site publishing serves from, and what most public repositories use.

## Consequences

- Relative links in the README and agent configuration require updating. A one-time cost, and it coincides with the rename pass already required.
- Made free by LADR-006; under upstream tracking this would be a permanent merge conflict.
- The root README must link the business requirements and high level design explicitly — a visible folder helps discovery but does not replace a signpost.
- Diagram markup renders natively in the hosted view, so architecture diagrams need no build step.
