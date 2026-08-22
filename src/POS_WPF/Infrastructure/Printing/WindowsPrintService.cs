using System.Printing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;

namespace POS_WPF.Infrastructure.Printing;

public sealed class WindowsPrintService(IDbContextFactory<AppDbContext> dbFactory) : IReceiptPrinter, IDocumentPrinter, ILabelPrinter
{
    public async Task PrintAsync(PrintDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var content = await AddConfiguredTaxToReceiptAsync(request.Content, cancellationToken);
        await PrintVisualAsync(request with { Content = content }, BuildDocument(content), cancellationToken);
        await TryOpenCashDrawerAsync(request.PrinterName, request.Content, cancellationToken);
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

    private async Task TryOpenCashDrawerAsync(string printerName, string receiptContent, CancellationToken cancellationToken)
    {
        var number = receiptContent.Split(['\r', '\n'], StringSplitOptions.None)
            .FirstOrDefault(x => x.StartsWith("S-", StringComparison.OrdinalIgnoreCase))?.Trim();
        if (string.IsNullOrWhiteSpace(number)) return;

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            var sale = await db.Sales.AsNoTracking()
                .Include(x => x.Payments)
                .SingleOrDefaultAsync(x => x.Number == number, cancellationToken);
            if (sale is null || !sale.Payments.Any(x => string.Equals(x.Method, "Cash", StringComparison.OrdinalIgnoreCase))) return;

            var server = new LocalPrintServer();
            var queue = string.IsNullOrWhiteSpace(printerName)
                ? server.DefaultPrintQueue
                : server.GetPrintQueue(printerName);

            // Standard ESC/POS cash-drawer pulse: pin 2, 25ms ON / 250ms OFF.
            RawPrinter.Send(queue.Name, new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA });
        }
        catch
        {
            // Drawer hardware must never turn a successfully printed sale into a failed sale.
        }
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

    private static class RawPrinter
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class DocInfo
        {
            public string DocName = "POS Cash Drawer";
            public string? OutputFile;
            public string DataType = "RAW";
        }

        [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter(string name, out nint printer, nint defaults);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(nint printer);

        [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int StartDocPrinter(nint printer, int level, [In] DocInfo docInfo);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(nint printer);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(nint printer);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(nint printer);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(nint printer, byte[] buffer, int count, out int written);

        public static void Send(string printerName, byte[] data)
        {
            if (!OpenPrinter(printerName, out var printer, 0)) return;
            try
            {
                var doc = new DocInfo();
                if (StartDocPrinter(printer, 1, doc) == 0) return;
                try
                {
                    if (!StartPagePrinter(printer)) return;
                    try { WritePrinter(printer, data, data.Length, out _); }
                    finally { EndPagePrinter(printer); }
                }
                finally { EndDocPrinter(printer); }
            }
            finally { ClosePrinter(printer); }
        }
    }
}
