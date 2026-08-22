namespace POS_WPF.Infrastructure.Printing;

public sealed record PrintDocumentRequest(string PrinterName, string Content, string? PaperSize = null);

public interface IReceiptPrinter
{
    Task PrintAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default);
}

public interface ILabelPrinter
{
    Task PrintLabelAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default);
}

public interface IDocumentPrinter
{
    Task PrintAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default);
}
