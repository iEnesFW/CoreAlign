# 6. QuestPDF for document rendering

- Status: Accepted
- Date: 2026-03-25
- Deciders: Backend leads, Product
- Tags: documents, pdf, licensing

## Context and Problem Statement

Sprint 3 (ERP-014) shipped the first batch of customer-facing documents: quotes, orders, invoices,
credit notes, and packing slips. We needed a PDF renderer that:

- Produced pixel-perfect, professional layouts including TR / EN, RTL-safe spacing, and embedded fonts.
- Ran as a pure .NET library (no headless Chromium, no native interop binaries on the runtime image).
- Had a licence model compatible with a commercial SaaS.

## Decision Drivers

- Layout fidelity for invoices that may be legally archived in Turkey for 10+ years.
- Build/test ergonomics on Linux containers and on Windows dev machines.
- Licence cost relative to projected document volume.

## Considered Options

1. **QuestPDF 2024.x** — pure managed .NET PDF generation with fluent layout API.
2. **DinkToPdf** — wkhtmltopdf wrapper. Native binaries required per OS.
3. **IronPdf** — HTML-to-PDF rendering via embedded Chromium. Commercial, per-developer + per-server licence.
4. **PuppeteerSharp** — drive headless Chrome. Heaviest container footprint.

## Decision

We adopt **Option 1**: QuestPDF behind an `IDocumentRenderer` abstraction (`QuestPdfDocumentRenderer`).

- The renderer interface is the only thing application code depends on, so the engine can be swapped without touching call sites.
- Document templates live in `server/src/CoreAlign.Infrastructure/Documents/Templates/` as composable C# fluent definitions.
- QuestPDF Community licence applies while CoreAlign's annual revenue stays below the threshold; we hold a paid Professional licence for production resale safety.

## Consequences

- Positive: no native binary, no GDI dependency, runs identically on Linux containers and Windows dev boxes.
- Positive: fluent C# templates are diffable and reviewable (no opaque HTML/CSS).
- Positive: benchmarked ~70ms per invoice on the staging worker, well within SLA.
- Negative: requires designers to learn the QuestPDF fluent API. Mitigated by template catalog + shared layout primitives.
- Negative: less flexible than HTML for complex web-style layouts. We accept this tradeoff for legally-formatted documents.

## Links

- `IDocumentRenderer` and `QuestPdfDocumentRenderer` introduced in ERP-014 (Sprint 3).
- QuestPDF licence terms: https://www.questpdf.com/license/
