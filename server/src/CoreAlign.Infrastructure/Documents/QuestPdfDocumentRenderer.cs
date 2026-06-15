using System.Globalization;
using CoreAlign.Application.Documents;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CoreAlign.Infrastructure.Documents;

public sealed class QuestPdfDocumentRenderer : IDocumentRenderer
{
    private const string Slate900 = "#0F172A";
    private const string Slate700 = "#334155";
    private const string Slate500 = "#64748B";
    private const string Slate200 = "#E2E8F0";
    private const string Slate100 = "#F1F5F9";
    private const string Slate50 = "#F8FAFC";
    private const string Brand = "#2563EB";

    public Task<byte[]> RenderInvoiceAsync(InvoiceDocumentModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildInvoicePdf(model));

    public Task<byte[]> RenderCreditNoteAsync(InvoiceDocumentModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildInvoicePdf(model));

    public Task<byte[]> RenderOrderConfirmationAsync(OrderDocumentModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildOrderPdf(model));

    public Task<byte[]> RenderPackingSlipAsync(ShipmentDocumentModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildShipmentPdf(model));

    public Task<byte[]> RenderQuoteAsync(QuoteDocumentModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildQuotePdf(model));

    public Task<byte[]> RenderDealerCommissionStatementAsync(DealerCommissionStatementModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(BuildDealerCommissionStatementPdf(model));

    private static byte[] BuildDealerCommissionStatementPdf(DealerCommissionStatementModel model)
    {
        var culture = ResolveCulture(model.Currency);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                var meta = new List<(string, string)>
                {
                    ("From", model.FromUtc.ToString("yyyy-MM-dd", culture)),
                    ("To", model.ToUtc.ToString("yyyy-MM-dd", culture)),
                    ("Currency", model.Currency),
                };
                page.Header().Element(h => RenderHeader(h, model.Tenant, model.DocumentTitle, $"{model.DealerCode ?? model.DealerName}", meta));
                page.Content().Element(c => RenderCommissionStatementContent(c, model, culture));
                page.Footer().Element(f => RenderFooter(f, null, null, null));
            });
        }).GeneratePdf();
    }

    private static void RenderCommissionStatementContent(IContainer container, DealerCommissionStatementModel model, CultureInfo culture)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Item().PaddingBottom(8).Text(model.DealerName).FontSize(11).Bold().FontColor(Slate900);

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(70);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(2);
                    c.RelativeColumn(1.2f);
                    c.RelativeColumn(0.8f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.2f);
                });
                table.Header(h =>
                {
                    HeaderCell(h.Cell(), "Date");
                    HeaderCell(h.Cell(), "Order");
                    HeaderCell(h.Cell(), "Shipment");
                    HeaderCell(h.Cell(), "Customer");
                    HeaderCell(h.Cell().AlignRight(), "Order total");
                    HeaderCell(h.Cell().AlignRight(), "%");
                    HeaderCell(h.Cell().AlignRight(), "Commission");
                    HeaderCell(h.Cell().AlignRight(), "Status");
                });
                foreach (var line in model.Lines)
                {
                    BodyCell(table.Cell(), line.AccruedAtUtc.ToString("yyyy-MM-dd", culture));
                    BodyCell(table.Cell(), line.OrderNumber);
                    BodyCell(table.Cell(), line.ShipmentNumber ?? string.Empty);
                    BodyCell(table.Cell(), line.CustomerName);
                    BodyCell(table.Cell().AlignRight(), line.OrderTotal.ToString("N2", culture));
                    BodyCell(table.Cell().AlignRight(), line.CommissionPercent.ToString("N2", culture));
                    BodyCell(table.Cell().AlignRight(), line.CommissionAmount.ToString("N2", culture));
                    BodyCell(table.Cell().AlignRight(), line.Status);
                }
            });

            col.Item().PaddingTop(12).Row(totals =>
            {
                totals.RelativeItem();
                totals.ConstantItem(260).Column(t =>
                {
                    t.Item().Element(c => TotalsRow(c, "Total accrued", model.TotalAccrued, model.Currency, culture));
                    t.Item().Element(c => TotalsRow(c, "Total paid", model.TotalPaid, model.Currency, culture));
                    t.Item().BorderTop(1).BorderColor(Slate200).PaddingTop(4)
                        .Element(c => TotalsRow(c, "Outstanding", model.TotalOutstanding, model.Currency, culture, emphasized: true));
                });
            });
        });
    }

    private static byte[] BuildQuotePdf(QuoteDocumentModel model)
    {
        var culture = ResolveCulture(model.Currency);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                var headerMeta = new List<(string, string)>
                {
                    ("Quote date", model.QuoteDate.ToString("yyyy-MM-dd", culture)),
                    ("Valid until", model.ValidUntilUtc.ToString("yyyy-MM-dd", culture)),
                    ("Currency", model.Currency),
                };
                page.Header().Element(h => RenderHeader(h, model.Tenant, model.DocumentTitle, model.QuoteNumber, headerMeta));
                page.Content().Element(c => RenderQuoteContent(c, model, culture));
                page.Footer().Element(f => RenderFooter(f, model.PaymentTerms, model.CustomerNotes ?? model.PublicNotes, model.TermsAndConditions));
            });
        }).GeneratePdf();
    }

    private static void RenderQuoteContent(IContainer container, QuoteDocumentModel model, CultureInfo culture)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Item().Row(parties =>
            {
                parties.RelativeItem().Element(c => RenderParty(c, "Seller / Satıcı", model.Seller));
                parties.ConstantItem(12);
                parties.RelativeItem().Element(c => RenderParty(c, "Buyer / Alıcı", model.Buyer));
            });

            col.Item().PaddingTop(12).Element(c => RenderLineItemsTable(c, model.Lines, model.Currency, culture));

            col.Item().PaddingTop(10).Row(totals =>
            {
                totals.RelativeItem();
                totals.ConstantItem(260).Element(c => RenderQuoteTotals(c, model, culture));
            });

            if (model.TaxBreakdown.Count > 0)
            {
                col.Item().PaddingTop(8).Element(c => RenderTaxBreakdown(c, model.TaxBreakdown, model.Currency, culture));
            }
        });
    }

    private static void RenderQuoteTotals(IContainer container, QuoteDocumentModel model, CultureInfo culture)
    {
        container.Column(col =>
        {
            col.Item().Element(c => TotalsRow(c, "Subtotal", model.Subtotal, model.Currency, culture));
            if (model.DiscountTotal > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Discount", -model.DiscountTotal, model.Currency, culture));
            }
            col.Item().Element(c => TotalsRow(c, "Tax", model.TaxTotal, model.Currency, culture));
            if (model.WithholdingTotal > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Withholding", -model.WithholdingTotal, model.Currency, culture));
            }
            if (model.ShippingCost > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Shipping", model.ShippingCost, model.Currency, culture));
            }
            if (model.RoundingAdjustment != 0)
            {
                col.Item().Element(c => TotalsRow(c, "Rounding", model.RoundingAdjustment, model.Currency, culture));
            }
            col.Item().PaddingTop(4).BorderTop(1).BorderColor(Slate200).PaddingTop(4).Element(c => TotalsRow(c, "Grand Total", model.GrandTotal, model.Currency, culture, emphasized: true));
        });
    }

    private static byte[] BuildInvoicePdf(InvoiceDocumentModel model)
    {
        var culture = ResolveCulture(model.Currency);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Element(h => RenderHeader(h, model.Tenant, model.DocumentTitle, model.DocumentNumber, new[]
                {
                    ("Issue date", model.IssueDate.ToString("yyyy-MM-dd", culture)),
                    ("Due date", model.DueDate.ToString("yyyy-MM-dd", culture)),
                    ("Currency", model.Currency),
                }));
                page.Content().Element(c => RenderInvoiceContent(c, model, culture));
                page.Footer().Element(f => RenderFooter(f, model.PaymentTerms, model.PublicNotes, model.TermsAndConditions));
            });
        }).GeneratePdf();
    }

    private static byte[] BuildOrderPdf(OrderDocumentModel model)
    {
        var culture = ResolveCulture(model.Currency);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                var headerMeta = new List<(string, string)>
                {
                    ("Order date", model.OrderDate.ToString("yyyy-MM-dd", culture)),
                    ("Currency", model.Currency),
                };
                if (model.RequestedDeliveryDate.HasValue)
                {
                    headerMeta.Add(("Requested delivery", model.RequestedDeliveryDate.Value.ToString("yyyy-MM-dd", culture)));
                }
                page.Header().Element(h => RenderHeader(h, model.Tenant, model.DocumentTitle, model.OrderNumber, headerMeta));
                page.Content().Element(c => RenderOrderContent(c, model, culture));
                page.Footer().Element(f => RenderFooter(f, model.PaymentTerms, model.CustomerNotes, null));
            });
        }).GeneratePdf();
    }

    private static byte[] BuildShipmentPdf(ShipmentDocumentModel model)
    {
        var culture = CultureInfo.InvariantCulture;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                var meta = new List<(string, string)>
                {
                    ("Order", model.OrderNumber),
                    ("Created", model.CreatedDate.ToString("yyyy-MM-dd", culture)),
                };
                if (model.DispatchedAt.HasValue)
                {
                    meta.Add(("Dispatched", model.DispatchedAt.Value.ToString("yyyy-MM-dd", culture)));
                }
                if (!string.IsNullOrWhiteSpace(model.CarrierName))
                {
                    meta.Add(("Carrier", model.CarrierName!));
                }
                if (!string.IsNullOrWhiteSpace(model.TrackingNumber))
                {
                    meta.Add(("Tracking", model.TrackingNumber!));
                }
                page.Header().Element(h => RenderHeader(h, model.Tenant, model.DocumentTitle, model.ShipmentNumber, meta));
                page.Content().Element(c => RenderShipmentContent(c, model));
                page.Footer().Element(f => RenderFooter(f, null, model.Notes, null));
            });
        }).GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(30);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(t => t.FontFamily("Helvetica").FontSize(9).FontColor(Slate900));
    }

    private static void RenderHeader(IContainer container, DocumentTenantHeader tenant, string title, string documentNumber, IEnumerable<(string Label, string Value)> meta)
    {
        container.PaddingBottom(12).BorderBottom(1).BorderColor(Slate200).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(tenant.LegalName).FontSize(13).Bold().FontColor(Slate900);
                if (!string.IsNullOrWhiteSpace(tenant.TradeName))
                {
                    col.Item().Text(tenant.TradeName!).FontSize(9).FontColor(Slate500);
                }
                var address = ComposeAddress(tenant.AddressLine1, tenant.AddressLine2, tenant.City, tenant.StateProvince, tenant.PostalCode, tenant.Country);
                if (!string.IsNullOrWhiteSpace(address))
                {
                    col.Item().Text(address).FontSize(8).FontColor(Slate500);
                }
                if (!string.IsNullOrWhiteSpace(tenant.TaxNumber))
                {
                    col.Item().Text($"Tax No: {tenant.TaxNumber} {tenant.TaxOffice}".TrimEnd()).FontSize(8).FontColor(Slate500);
                }
                if (!string.IsNullOrWhiteSpace(tenant.Phone))
                {
                    col.Item().Text($"Phone: {tenant.Phone}").FontSize(8).FontColor(Slate500);
                }
                if (!string.IsNullOrWhiteSpace(tenant.Email))
                {
                    col.Item().Text($"Email: {tenant.Email}").FontSize(8).FontColor(Slate500);
                }
            });

            row.ConstantItem(220).Column(col =>
            {
                col.Item().AlignRight().Text(title).FontSize(14).Bold().FontColor(Brand);
                col.Item().AlignRight().PaddingTop(2).Text(documentNumber).FontSize(11).SemiBold().FontColor(Slate700);
                col.Item().PaddingTop(6).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });
                    foreach (var (label, value) in meta)
                    {
                        table.Cell().AlignRight().Text(label).FontSize(8).FontColor(Slate500);
                        table.Cell().AlignRight().Text(value).FontSize(9).SemiBold().FontColor(Slate900);
                    }
                });
            });
        });
    }

    private static void RenderInvoiceContent(IContainer container, InvoiceDocumentModel model, CultureInfo culture)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Item().Row(parties =>
            {
                parties.RelativeItem().Element(c => RenderParty(c, "Seller / Satıcı", model.Seller));
                parties.ConstantItem(12);
                parties.RelativeItem().Element(c => RenderParty(c, "Buyer / Alıcı", model.Buyer));
            });

            col.Item().PaddingTop(12).Element(c => RenderLineItemsTable(c, model.Lines, model.Currency, culture));
            col.Item().PaddingTop(10).Row(totals =>
            {
                totals.RelativeItem();
                totals.ConstantItem(260).Element(c => RenderTotals(c, model, culture));
            });

            if (model.TaxBreakdown.Count > 0)
            {
                col.Item().PaddingTop(8).Element(c => RenderTaxBreakdown(c, model.TaxBreakdown, model.Currency, culture));
            }
        });
    }

    private static void RenderOrderContent(IContainer container, OrderDocumentModel model, CultureInfo culture)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Item().Row(parties =>
            {
                parties.RelativeItem().Element(c => RenderParty(c, "Seller / Satıcı", model.Seller));
                parties.ConstantItem(12);
                parties.RelativeItem().Element(c => RenderParty(c, "Buyer / Alıcı", model.Buyer));
            });

            col.Item().PaddingTop(12).Element(c => RenderLineItemsTable(c, model.Lines, model.Currency, culture));

            col.Item().PaddingTop(10).Row(totals =>
            {
                totals.RelativeItem();
                totals.ConstantItem(260).Element(c => RenderOrderTotals(c, model, culture));
            });

            if (model.TaxBreakdown.Count > 0)
            {
                col.Item().PaddingTop(8).Element(c => RenderTaxBreakdown(c, model.TaxBreakdown, model.Currency, culture));
            }
        });
    }

    private static void RenderShipmentContent(IContainer container, ShipmentDocumentModel model)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Item().Row(parties =>
            {
                parties.RelativeItem().Element(c => RenderParty(c, "From / Gönderici", model.Seller));
                parties.ConstantItem(12);
                parties.RelativeItem().Element(c => RenderParty(c, "Ship to / Teslim", model.Buyer));
            });

            if (!string.IsNullOrWhiteSpace(model.WarehouseName))
            {
                col.Item().PaddingTop(6).Text($"Warehouse: {model.WarehouseName}").FontSize(9).FontColor(Slate700);
            }

            col.Item().PaddingTop(12).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(28);
                    c.ConstantColumn(80);
                    c.RelativeColumn(3);
                    c.ConstantColumn(60);
                    c.ConstantColumn(80);
                    c.ConstantColumn(80);
                });
                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "#");
                    HeaderCell(header.Cell(), "SKU");
                    HeaderCell(header.Cell(), "Item");
                    HeaderCell(header.Cell().AlignRight(), "Qty");
                    HeaderCell(header.Cell(), "Lot");
                    HeaderCell(header.Cell(), "Serial");
                });
                foreach (var line in model.Lines)
                {
                    BodyCell(table.Cell(), line.LineNumber.ToString(CultureInfo.InvariantCulture));
                    BodyCell(table.Cell(), line.Sku);
                    table.Cell().Element(c => c.PaddingVertical(3).PaddingHorizontal(4)).Column(inner =>
                    {
                        inner.Item().Text(line.Name).FontSize(9).FontColor(Slate900);
                        if (!string.IsNullOrWhiteSpace(line.Notes))
                        {
                            inner.Item().Text(line.Notes!).FontSize(8).FontColor(Slate500);
                        }
                    });
                    BodyCell(table.Cell().AlignRight(), line.Quantity.ToString("0.####", CultureInfo.InvariantCulture));
                    BodyCell(table.Cell(), line.LotNumber ?? "-");
                    BodyCell(table.Cell(), line.SerialNumber ?? "-");
                }
            });
        });
    }

    private static void RenderParty(IContainer container, string title, DocumentParty party)
    {
        container.Background(Slate50).Padding(8).Column(col =>
        {
            col.Item().Text(title).FontSize(8).FontColor(Slate500).Bold();
            col.Item().PaddingTop(2).Text(party.LegalName).FontSize(10).Bold().FontColor(Slate900);
            if (!string.IsNullOrWhiteSpace(party.TradeName))
            {
                col.Item().Text(party.TradeName!).FontSize(8).FontColor(Slate500);
            }
            var addr = ComposeAddress(party.AddressLine1, party.AddressLine2, party.City, party.StateProvince, party.PostalCode, party.Country);
            if (!string.IsNullOrWhiteSpace(addr))
            {
                col.Item().PaddingTop(2).Text(addr).FontSize(8).FontColor(Slate700);
            }
            if (!string.IsNullOrWhiteSpace(party.TaxNumber))
            {
                col.Item().PaddingTop(2).Text($"Tax: {party.TaxNumber} {party.TaxOffice}".TrimEnd()).FontSize(8).FontColor(Slate700);
            }
            if (!string.IsNullOrWhiteSpace(party.Email))
            {
                col.Item().Text(party.Email!).FontSize(8).FontColor(Slate700);
            }
            if (!string.IsNullOrWhiteSpace(party.Phone))
            {
                col.Item().Text(party.Phone!).FontSize(8).FontColor(Slate700);
            }
        });
    }

    private static void RenderLineItemsTable(IContainer container, IReadOnlyList<DocumentLine> lines, string currency, CultureInfo culture)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(c =>
            {
                c.ConstantColumn(22);
                c.ConstantColumn(70);
                c.RelativeColumn(3);
                c.ConstantColumn(48);
                c.ConstantColumn(40);
                c.ConstantColumn(60);
                c.ConstantColumn(40);
                c.ConstantColumn(70);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#");
                HeaderCell(header.Cell(), "SKU");
                HeaderCell(header.Cell(), "Item / Açıklama");
                HeaderCell(header.Cell().AlignRight(), "Qty");
                HeaderCell(header.Cell(), "Unit");
                HeaderCell(header.Cell().AlignRight(), "Price");
                HeaderCell(header.Cell().AlignRight(), "Tax%");
                HeaderCell(header.Cell().AlignRight(), $"Total ({currency})");
            });

            foreach (var line in lines)
            {
                BodyCell(table.Cell(), line.LineNumber.ToString(CultureInfo.InvariantCulture));
                BodyCell(table.Cell(), line.Sku);
                table.Cell().Element(c => c.PaddingVertical(3).PaddingHorizontal(4)).Column(inner =>
                {
                    inner.Item().Text(line.Name).FontSize(9).FontColor(Slate900);
                    if (!string.IsNullOrWhiteSpace(line.Description))
                    {
                        inner.Item().Text(line.Description!).FontSize(8).FontColor(Slate500);
                    }
                });
                BodyCell(table.Cell().AlignRight(), line.Quantity.ToString("0.####", culture));
                BodyCell(table.Cell(), line.UnitCode ?? "-");
                BodyCell(table.Cell().AlignRight(), line.UnitPrice.ToString("N2", culture));
                BodyCell(table.Cell().AlignRight(), line.TaxRatePercent.ToString("0.##", culture));
                BodyCell(table.Cell().AlignRight(), line.LineTotal.ToString("N2", culture));
            }
        });
    }

    private static void RenderTotals(IContainer container, InvoiceDocumentModel model, CultureInfo culture)
    {
        container.Column(col =>
        {
            col.Item().Element(c => TotalsRow(c, "Subtotal", model.Subtotal, model.Currency, culture));
            if (model.DiscountTotal > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Discount", -model.DiscountTotal, model.Currency, culture));
            }
            col.Item().Element(c => TotalsRow(c, "Tax", model.TaxTotal, model.Currency, culture));
            if (model.WithholdingTotal > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Withholding", -model.WithholdingTotal, model.Currency, culture));
            }
            if (model.ShippingCost > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Shipping", model.ShippingCost, model.Currency, culture));
            }
            if (model.RoundingAdjustment != 0)
            {
                col.Item().Element(c => TotalsRow(c, "Rounding", model.RoundingAdjustment, model.Currency, culture));
            }
            col.Item().PaddingTop(4).BorderTop(1).BorderColor(Slate200).PaddingTop(4).Element(c => TotalsRow(c, "Grand Total", model.GrandTotal, model.Currency, culture, emphasized: true));
        });
    }

    private static void RenderOrderTotals(IContainer container, OrderDocumentModel model, CultureInfo culture)
    {
        container.Column(col =>
        {
            col.Item().Element(c => TotalsRow(c, "Subtotal", model.Subtotal, model.Currency, culture));
            if (model.DiscountTotal > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Discount", -model.DiscountTotal, model.Currency, culture));
            }
            col.Item().Element(c => TotalsRow(c, "Tax", model.TaxTotal, model.Currency, culture));
            if (model.ShippingCost > 0)
            {
                col.Item().Element(c => TotalsRow(c, "Shipping", model.ShippingCost, model.Currency, culture));
            }
            col.Item().PaddingTop(4).BorderTop(1).BorderColor(Slate200).PaddingTop(4).Element(c => TotalsRow(c, "Grand Total", model.GrandTotal, model.Currency, culture, emphasized: true));
        });
    }

    private static void TotalsRow(IContainer container, string label, decimal amount, string currency, CultureInfo culture, bool emphasized = false)
    {
        var labelSize = emphasized ? 11 : 9;
        var valueSize = emphasized ? 12 : 9;
        var color = emphasized ? Brand : Slate700;
        container.Row(row =>
        {
            row.RelativeItem().AlignRight().Text(label).FontSize(labelSize).FontColor(color).SemiBold();
            row.ConstantItem(100).AlignRight().PaddingLeft(8).Text($"{amount.ToString("N2", culture)} {currency}").FontSize(valueSize).FontColor(color).Bold();
        });
    }

    private static void RenderTaxBreakdown(IContainer container, IReadOnlyList<DocumentTaxBreakdown> breakdown, string currency, CultureInfo culture)
    {
        container.Column(col =>
        {
            col.Item().Text("Tax Breakdown / Vergi Dağılımı").FontSize(9).Bold().FontColor(Slate700);
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(80);
                    c.RelativeColumn();
                    c.RelativeColumn();
                });
                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Rate");
                    HeaderCell(header.Cell().AlignRight(), "Base");
                    HeaderCell(header.Cell().AlignRight(), $"Tax ({currency})");
                });
                foreach (var row in breakdown)
                {
                    BodyCell(table.Cell(), $"{row.RatePercent.ToString("0.##", culture)}%");
                    BodyCell(table.Cell().AlignRight(), row.TaxableBase.ToString("N2", culture));
                    BodyCell(table.Cell().AlignRight(), row.TaxAmount.ToString("N2", culture));
                }
            });
        });
    }

    private static void RenderFooter(IContainer container, string? paymentTerms, string? notes, string? termsAndConditions)
    {
        container.PaddingTop(10).BorderTop(1).BorderColor(Slate200).PaddingTop(8).Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(paymentTerms))
            {
                col.Item().Text($"Payment terms: {paymentTerms}").FontSize(8).FontColor(Slate700).SemiBold();
            }
            if (!string.IsNullOrWhiteSpace(notes))
            {
                col.Item().PaddingTop(2).Text(notes!).FontSize(8).FontColor(Slate500);
            }
            if (!string.IsNullOrWhiteSpace(termsAndConditions))
            {
                col.Item().PaddingTop(2).Text(termsAndConditions!).FontSize(7).FontColor(Slate500);
            }
            col.Item().PaddingTop(6).AlignCenter().Text(text =>
            {
                text.Span("Page ").FontSize(7).FontColor(Slate500);
                text.CurrentPageNumber().FontSize(7).FontColor(Slate500);
                text.Span(" / ").FontSize(7).FontColor(Slate500);
                text.TotalPages().FontSize(7).FontColor(Slate500);
            });
        });
    }

    private static void HeaderCell(IContainer container, string text)
    {
        container.Background(Slate100).BorderBottom(1).BorderColor(Slate200).PaddingVertical(4).PaddingHorizontal(4)
            .Text(text).FontSize(8).Bold().FontColor(Slate700);
    }

    private static void BodyCell(IContainer container, string text)
    {
        container.BorderBottom(1).BorderColor(Slate100).PaddingVertical(3).PaddingHorizontal(4)
            .Text(text).FontSize(9).FontColor(Slate900);
    }

    private static string ComposeAddress(string? line1, string? line2, string? city, string? state, string? postalCode, string? country)
    {
        var parts = new[] { line1, line2, city, state, postalCode, country }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(", ", parts);
    }

    private static CultureInfo ResolveCulture(string currency)
    {
        return currency switch
        {
            "TRY" => CultureInfo.GetCultureInfo("tr-TR"),
            "EUR" => CultureInfo.GetCultureInfo("de-DE"),
            "USD" => CultureInfo.GetCultureInfo("en-US"),
            _ => CultureInfo.InvariantCulture,
        };
    }
}
