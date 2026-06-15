# 1. Record architecture decisions

- Status: Accepted
- Date: 2026-02-27
- Deciders: Founding engineering team
- Tags: process, governance

## Context and Problem Statement

As CoreAlign grows from a single-sprint prototype into a multi-sprint, multi-team ERP product,
implicit architectural choices keep being relitigated by every new contributor. We need a low-ceremony
way to preserve **why** a decision was taken so the rationale survives team turnover and is
discoverable from inside the repository.

## Decision Drivers

- Decisions must live next to the code they govern, not in a wiki.
- Authoring must be cheap enough that engineers actually do it.
- Reviewable in the same PR flow as code, so the same approval gates apply.

## Considered Options

1. **Architecture Decision Records (ADRs)** in `docs/adr/` using the [MADR](https://adr.github.io/madr/) template.
2. Confluence / Notion page tree.
3. Comments inside source files near the affected code.
4. Do nothing; rely on tribal knowledge.

## Decision

We adopt **Option 1**: lightweight MADR-style ADRs in `docs/adr/` with sequential filenames
(`NNNN-title-in-kebab-case.md`), reviewed via PR.

## Consequences

- Positive: rationale is version-controlled, diffable, and discoverable. New contributors can read the chronology of decisions in roughly 30 minutes.
- Positive: PR review enforces the same quality bar as code.
- Neutral: requires light maintenance — index updates and supersession links.
- Negative: a poorly-written ADR can be worse than none. We will pair-review the first few to set the tone.

## Links

- MADR template: https://adr.github.io/madr/
- Index of ADRs: [docs/adr/README.md](README.md)
