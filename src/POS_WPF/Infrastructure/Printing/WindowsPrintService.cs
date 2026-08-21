using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;

namespace POS_WPF.Infrastructure.Printing;

public sealed class WindowsPrintService : IReceiptPrinter, IDocumentPrinter, ILabelPrinter
{
    public Task PrintAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default)
        => PrintVisualAsync(request, BuildDocument(request.Content), cancellationToken);

    public Task PrintLabelAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default)
        => PrintVisualAsync(request, BuildDocument(request.Content), cancellationToken);

    private static FlowDocument BuildDocument(string content)
    {
        var document = new FlowDocument { FontFamily = new FontFamily("Segoe UI"), FontSize = 10, PagePadding = new Thickness(12) };
        foreach (var line in content.Split(['\r', '\n'], StringSplitOptions.None)) document.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0) });
        return document;
    }

    private static Task PrintVisualAsync(PrintDocumentRequest request, FlowDocument document, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var server = new LocalPrintServer();
        var queue = string.IsNullOrWhiteSpace(request.PrinterName) ? server.DefaultPrintQueue : server.GetPrintQueue(request.PrinterName);
        var writer = PrintQueue.CreateXpsDocumentWriter(queue);
        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        writer.Write(paginator);
        return Task.CompletedTask;
    }
}
