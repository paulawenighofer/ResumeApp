using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ResumeApp.Services;

public sealed class PdfService : IPdfService
{
    private const int MaxCharactersPerLine = 88;
    private const int MaxLinesPerPage = 45;

    public async Task<PdfExportResult> CreatePdfFromJsonAsync(string jsonContent, string fileNameWithoutExtension, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonContent);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNameWithoutExtension);

        JsonNode.Parse(jsonContent);

        var flattenedLines = FlattenJson(jsonContent);
        if (flattenedLines.Count == 0)
        {
            flattenedLines.Add("No content available.");
        }

        var pages = Paginate(flattenedLines);
        var pdfBytes = BuildPdf(pages);

        var safeFileName = string.Concat(
            fileNameWithoutExtension
                .Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch))
            .Trim();

        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "resume";
        }

        var fileName = $"{safeFileName}.pdf";
        var filePath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);

        await File.WriteAllBytesAsync(filePath, pdfBytes, cancellationToken);
        return new PdfExportResult(filePath, fileName);
    }

    private static List<List<string>> Paginate(IReadOnlyList<string> lines)
    {
        var pages = new List<List<string>>();

        for (var i = 0; i < lines.Count; i += MaxLinesPerPage)
        {
            pages.Add(lines.Skip(i).Take(MaxLinesPerPage).ToList());
        }

        return pages;
    }

    private static List<string> FlattenJson(string jsonContent)
    {
        using var document = JsonDocument.Parse(jsonContent);
        var lines = new List<string>();
        AppendElement(lines, document.RootElement, "Resume Data", 0);
        return lines;
    }

    private static void AppendElement(List<string> lines, JsonElement element, string label, int depth)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (!string.IsNullOrWhiteSpace(label))
                {
                    lines.Add(FormatLine(depth, label));
                }

                foreach (var property in element.EnumerateObject())
                {
                    AppendElement(lines, property.Value, ToTitleCase(property.Name), depth + 1);
                }

                break;
            case JsonValueKind.Array:
                lines.Add(FormatLine(depth, label));

                var index = 1;
                foreach (var item in element.EnumerateArray())
                {
                    var itemLabel = item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array
                        ? $"{label} Item {index}"
                        : $"{index}.";

                    AppendElement(lines, item, itemLabel, depth + 1);
                    index++;
                }

                break;
            case JsonValueKind.String:
                AddWrappedText(lines, depth, label, element.GetString() ?? string.Empty);
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                AddWrappedText(lines, depth, label, element.ToString());
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                AddWrappedText(lines, depth, label, "N/A");
                break;
        }
    }

    private static void AddWrappedText(List<string> lines, int depth, string label, string value)
    {
        var prefix = string.IsNullOrWhiteSpace(label) ? string.Empty : $"{label}: ";
        var normalized = NormalizeText(value);
        var wrapped = WrapText(prefix + normalized, MaxCharactersPerLine - (depth * 2));

        foreach (var line in wrapped)
        {
            lines.Add(FormatLine(depth, line, rawContent: true));
        }
    }

    private static string NormalizeText(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\n', ' ')
            .Trim();
    }

    private static IEnumerable<string> WrapText(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield return string.Empty;
            yield break;
        }

        maxLength = Math.Max(20, maxLength);
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length == 0)
            {
                currentLine.Append(word);
                continue;
            }

            if (currentLine.Length + 1 + word.Length <= maxLength)
            {
                currentLine.Append(' ').Append(word);
                continue;
            }

            yield return currentLine.ToString();
            currentLine.Clear();
            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
        {
            yield return currentLine.ToString();
        }
    }

    private static string FormatLine(int depth, string content, bool rawContent = false)
    {
        var indent = new string(' ', depth * 2);
        return rawContent ? indent + content : indent + content.Trim();
    }

    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var withSpaces = input.Replace("_", " ", StringComparison.Ordinal);
        var builder = new StringBuilder();

        for (var i = 0; i < withSpaces.Length; i++)
        {
            var current = withSpaces[i];
            if (i > 0 && char.IsUpper(current) && withSpaces[i - 1] != ' ')
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(builder.ToString().ToLowerInvariant());
    }

    private static byte[] BuildPdf(IReadOnlyList<List<string>> pages)
    {
        var objects = new List<string>();
        var pageObjectNumbers = new List<int>();

        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add("<< /Type /Pages /Kids [ ] /Count 0 >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

        foreach (var pageLines in pages)
        {
            var contentObjectNumber = objects.Count + 2;
            var pageObjectNumber = objects.Count + 1;
            pageObjectNumbers.Add(pageObjectNumber);

            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentObjectNumber} 0 R >>");
            objects.Add(BuildContentStream(pageLines));
        }

        objects[1] = $"<< /Type /Pages /Kids [{string.Join(' ', pageObjectNumbers.Select(n => $"{n} 0 R"))}] /Count {pageObjectNumbers.Count} >>";

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(builder.Length);
            var objectNumber = i + 1;
            builder.Append(objectNumber).Append(" 0 obj\n");
            builder.Append(objects[i]).Append('\n');
            builder.Append("endobj\n");
        }

        var xrefPosition = builder.Length;
        builder.Append("xref\n");
        builder.Append("0 ").Append(objects.Count + 1).Append('\n');
        builder.Append("0000000000 65535 f \n");

        for (var i = 1; i < offsets.Count; i++)
        {
            builder.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture))
                .Append(" 00000 n \n");
        }

        builder.Append("trailer\n");
        builder.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        builder.Append("startxref\n");
        builder.Append(xrefPosition).Append('\n');
        builder.Append("%%EOF");

        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static string BuildContentStream(IReadOnlyList<string> pageLines)
    {
        var stream = new StringBuilder();
        stream.Append("BT\n/F1 11 Tf\n50 790 Td\n14 TL\n");

        var firstLine = true;
        foreach (var line in pageLines)
        {
            var safeLine = EscapePdfText(ToPdfSafeText(line));

            if (!firstLine)
            {
                stream.Append("T*\n");
            }

            stream.Append('(').Append(safeLine).Append(") Tj\n");
            firstLine = false;
        }

        stream.Append("ET");
        var streamText = stream.ToString();

        return $"<< /Length {Encoding.ASCII.GetByteCount(streamText)} >>\nstream\n{streamText}\nendstream";
    }

    private static string ToPdfSafeText(string input)
    {
        var builder = new StringBuilder(input.Length);

        foreach (var ch in input)
        {
            builder.Append(ch switch
            {
                >= (char)32 and <= (char)126 => ch,
                '\t' => ' ',
                _ => '?'
            });
        }

        return builder.ToString();
    }

    private static string EscapePdfText(string input)
    {
        return input
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
