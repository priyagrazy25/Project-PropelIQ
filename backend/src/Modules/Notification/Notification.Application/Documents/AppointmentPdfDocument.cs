using Notification.Application.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Notification.Application.Documents;

public sealed class AppointmentPdfDocument : IDocument
{
    private readonly AppointmentConfirmationInfo _info;

    // Platform branding colors
    private static readonly string PrimaryColor = "#1E40AF";
    private static readonly string SecondaryColor = "#3B82F6";
    private static readonly string TextColor = "#1F2937";
    private static readonly string LightGray = "#F3F4F6";
    private static readonly string BorderColor = "#D1D5DB";

    public AppointmentPdfDocument(AppointmentConfirmationInfo info)
    {
        _info = info;
    }

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Appointment Confirmation - {_info.PatientFullName}",
        Author = "Unified Patient Access Platform",
        Subject = "Appointment Confirmation",
        CreationDate = DateTime.UtcNow
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontSize(11).FontColor(TextColor));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item()
                        .Text("Unified Patient Access")
                        .Bold()
                        .FontSize(20)
                        .FontColor(PrimaryColor);

                    col.Item()
                        .Text("Healthcare Platform")
                        .FontSize(10)
                        .FontColor(SecondaryColor);
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item()
                        .Text("APPOINTMENT")
                        .Bold()
                        .FontSize(14)
                        .FontColor(PrimaryColor);

                    col.Item()
                        .Text("CONFIRMATION")
                        .Bold()
                        .FontSize(14)
                        .FontColor(PrimaryColor);
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(2).LineColor(PrimaryColor);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            // Greeting
            column.Item().PaddingBottom(15).Text(text =>
            {
                text.Span("Dear ").FontSize(12);
                text.Span(_info.PatientFullName).Bold().FontSize(12);
                text.Span(",").FontSize(12);
            });

            column.Item().PaddingBottom(10).Text(
                "Your appointment has been confirmed. Please find the details below:")
                .FontSize(11);

            // Appointment Details Table
            column.Item().PaddingBottom(20).Element(ComposeAppointmentDetails);

            // Prep Notes Section
            if (!string.IsNullOrWhiteSpace(_info.PrepNotes))
            {
                column.Item().PaddingBottom(20).Element(ComposePrepNotes);
            }

            // Important Information
            column.Item().Element(ComposeImportantInfo);
        });
    }

    private void ComposeAppointmentDetails(IContainer container)
    {
        container.Border(1).BorderColor(BorderColor).Column(column =>
        {
            // Header row
            column.Item()
                .Background(PrimaryColor)
                .Padding(10)
                .Text("Appointment Details")
                .Bold()
                .FontSize(13)
                .FontColor(Colors.White);

            // Detail rows
            ComposeDetailRow(column, "Provider", $"Dr. {_info.ProviderName}");

            if (!string.IsNullOrWhiteSpace(_info.ProviderSpecialty))
            {
                ComposeDetailRow(column, "Specialty", _info.ProviderSpecialty);
            }

            ComposeDetailRow(column, "Date",
                _info.AppointmentDateTime.ToString("dddd, MMMM dd, yyyy"));

            ComposeDetailRow(column, "Time",
                _info.AppointmentDateTime.ToString("h:mm tt"));

            ComposeDetailRow(column, "Duration",
                $"{_info.DurationMinutes} minutes");

            ComposeDetailRow(column, "Type", _info.AppointmentType);

            if (!string.IsNullOrWhiteSpace(_info.Location))
            {
                ComposeDetailRow(column, "Location", _info.Location);
            }

            ComposeDetailRow(column, "Confirmation #",
                _info.AppointmentId.ToString("N")[..8].ToUpperInvariant());
        });
    }

    private static void ComposeDetailRow(ColumnDescriptor column, string label, string value)
    {
        column.Item().BorderBottom(1).BorderColor(BorderColor).Row(row =>
        {
            row.ConstantItem(140)
                .Background(LightGray)
                .Padding(8)
                .Text(label)
                .Bold()
                .FontSize(10);

            row.RelativeItem()
                .Padding(8)
                .Text(value)
                .FontSize(10);
        });
    }

    private void ComposePrepNotes(IContainer container)
    {
        container.Border(1).BorderColor(BorderColor).Column(column =>
        {
            column.Item()
                .Background(SecondaryColor)
                .Padding(10)
                .Text("Preparation Notes")
                .Bold()
                .FontSize(13)
                .FontColor(Colors.White);

            column.Item()
                .Padding(12)
                .Text(_info.PrepNotes!)
                .FontSize(10)
                .LineHeight(1.5f);
        });
    }

    private static void ComposeImportantInfo(IContainer container)
    {
        container.Background(LightGray).Border(1).BorderColor(BorderColor).Padding(12).Column(column =>
        {
            column.Item()
                .PaddingBottom(8)
                .Text("Important Information")
                .Bold()
                .FontSize(12)
                .FontColor(TextColor);

            var items = new[]
            {
                "Please arrive 15 minutes before your scheduled appointment time.",
                "Bring a valid photo ID and your insurance card.",
                "If you need to cancel or reschedule, please contact us at least 24 hours in advance.",
                "For urgent medical concerns, please call our emergency line or visit the nearest emergency room."
            };

            foreach (var item in items)
            {
                column.Item().PaddingBottom(4).Row(row =>
                {
                    row.ConstantItem(15).Text("•").FontSize(10);
                    row.RelativeItem().Text(item).FontSize(9).LineHeight(1.4f);
                });
            }
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(BorderColor);

            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem()
                    .Text($"Generated on {DateTime.UtcNow:MMMM dd, yyyy} at {DateTime.UtcNow:h:mm tt} UTC")
                    .FontSize(8)
                    .FontColor("#9CA3AF");

                row.RelativeItem().AlignRight()
                    .Text("Unified Patient Access Platform")
                    .FontSize(8)
                    .FontColor("#9CA3AF");
            });

            column.Item().PaddingTop(4).AlignCenter()
                .Text("This is an automated confirmation. Please do not reply to this email.")
                .FontSize(7)
                .FontColor("#9CA3AF");
        });
    }
}
