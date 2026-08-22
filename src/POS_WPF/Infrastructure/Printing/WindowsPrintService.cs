using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Infrastructure.Printing;

public sealed class WindowsPrintService(IDbContextFactory<AppDbContext> dbFactory) : IReceiptPrinter, IDocumentPrinter, ILabelPrinter
{
    public async Task PrintAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var content = await AddConfiguredTaxToReceiptAsync(request.Content, cancellationToken);
        await PrintVisualAsync(request with { Content = content }, BuildDocument(content), cancellationToken);
    }

    public Task PrintLabelAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default)
        => PrintVisualAsync(request, BuildDocument(request.Content), cancellationToken);

    private async Task<string> AddConfiguredTaxToReceiptAsync(string content, CancellationToken cancellationToken)
    {
        var lines = content.Split(['\r', '\n'], StringSplitOptions.None).ToList();
        var number = lines.FirstOrDefault(x => x.StartsWith("S-", StringComparison.OrdinalIgnoreCase))?.Trim();
        if (string.IsNullOrWhiteSpace(number)) return content;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var sale = await db.Sales.AsNoTracking().SingleOrDefaultAsync(x => x.Number == number, cancellationToken);
        if (sale is null || sale.Tax <= 0m) return content;

        var totalIndex = lines.FindIndex(x => x.TrimStart().StartsWith("TOTAL:", StringComparison.OrdinalIgnoreCase));
        if (totalIndex < 0) return content;

        lines.Insert(totalIndex, $"TAX: {sale.Tax:N2}");
        return string.Join(Environment.NewLine, lines);
    }

    private static FlowDocument BuildDocument(string content)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 10,
            PagePadding = new Thickness(12)
        };

        foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.None))
            document.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0) });

        return document;
    }

    private static Task PrintVisualAsync(PrintDocumentRequest request, FlowDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var server = new LocalPrintServer();
        var queue = string.IsNullOrWhiteSpace(request.PrinterName)
            ? server.DefaultPrintQueue
            : server.GetPrintQueue(request.PrinterName);
        var writer = PrintQueue.CreateXpsDocumentWriter(queue);
        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        writer.Write(paginator);
        return Task.CompletedTask;
    }
}
