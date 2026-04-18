using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text.Json;

namespace API.Services;

public sealed class QuestPdfRenderer : IPdfRenderer
{
    public byte[] RenderResumePdf(string targetCompany, string resumeJson)
    {
        var sections = BuildSections(resumeJson);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Resume").FontSize(20).Bold();
                        column.Item().Text($"Target Company: {targetCompany}").FontSize(11).FontColor(Colors.Grey.Darken2);
                    });

                page.Content()
                    .PaddingVertical(16)
                    .Column(column =>
                    {
                        if (sections.Count == 0)
                        {
                            column.Item().Text("No resume content available.");
                            return;
                        }

                        foreach (var section in sections)
                        {
                            column.Item().PaddingTop(8).Text(section.Title).FontSize(14).Bold();

                            foreach (var line in section.Items)
                            {
                                column.Item().PaddingLeft(8).Text($"• {line}");
                            }
                        }
                    });
            });
        }).GeneratePdf();
    }

    private static List<PdfSection> BuildSections(string resumeJson)
    {
        var sections = new List<PdfSection>();

        if (string.IsNullOrWhiteSpace(resumeJson))
        {
            return sections;
        }

        try
        {
            using var doc = JsonDocument.Parse(resumeJson);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return sections;
            }

            AddUserSection(root, sections);
            AddArraySection(root, "education", "Education", sections);
            AddArraySection(root, "experience", "Experience", sections);
            AddArraySection(root, "skills", "Skills", sections);
            AddArraySection(root, "projects", "Projects", sections);
            AddArraySection(root, "certifications", "Certifications", sections);

            return sections;
        }
        catch
        {
            return sections;
        }
    }

    private static void AddUserSection(JsonElement root, ICollection<PdfSection> sections)
    {
        if (!root.TryGetProperty("user", out var user) || user.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var items = user.EnumerateObject()
            .Select(property => new { Name = ToTitle(property.Name), Value = ToInlineText(property.Value) })
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{x.Name}: {x.Value}")
            .ToList();

        if (items.Count > 0)
        {
            sections.Add(new PdfSection("Profile", items));
        }
    }

    private static void AddArraySection(JsonElement root, string propertyName, string title, ICollection<PdfSection> sections)
    {
        if (!root.TryGetProperty(propertyName, out var section) || section.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var items = section.EnumerateArray()
            .Select(ToInlineText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (items.Count == 0)
        {
            return;
        }

        sections.Add(new PdfSection(title, items));
    }

    private static string ToInlineText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(ToInlineText).Where(x => !string.IsNullOrWhiteSpace(x))),
            JsonValueKind.Object => string.Join(" • ", element.EnumerateObject()
                .Select(p => new { Name = ToTitle(p.Name), Value = ToInlineText(p.Value) })
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{x.Name}: {x.Value}")),
            _ => string.Empty
        };
    }

    private static string ToTitle(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var chars = new List<char>(name.Length + 4)
        {
            char.ToUpperInvariant(name[0])
        };

        for (var i = 1; i < name.Length; i++)
        {
            var current = name[i];
            var previous = name[i - 1];

            if (char.IsUpper(current) && !char.IsWhiteSpace(previous))
            {
                chars.Add(' ');
            }

            chars.Add(current);
        }

        return new string(chars.ToArray());
    }

    private sealed record PdfSection(string Title, List<string> Items);
}
