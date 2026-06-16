using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Services;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notification/pdf")]
public class PdfConfirmationController : ControllerBase
{
    private readonly IPdfConfirmationService _pdfService;

    public PdfConfirmationController(IPdfConfirmationService pdfService)
    {
        _pdfService = pdfService;
    }

    /// <summary>
    /// Downloads or previews the PDF confirmation for an appointment.
    /// </summary>
    /// <param name="appointmentId">The appointment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The PDF file or 404 if not found.</returns>
    [HttpGet("{appointmentId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPdf(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var result = await _pdfService.GetOrGeneratePdfAsync(appointmentId, cancellationToken);

        if (result is null)
        {
            return NotFound(new { Message = "PDF confirmation not found for this appointment." });
        }

        return File(result.Value.Content, "application/pdf", result.Value.FileName);
    }

    /// <summary>
    /// Manually triggers PDF generation and email delivery for an appointment.
    /// </summary>
    /// <param name="appointmentId">The appointment identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{appointmentId:guid}/generate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> GeneratePdf(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        await _pdfService.GenerateAndSendConfirmationAsync(appointmentId, cancellationToken);

        return Accepted(new { Message = "PDF confirmation generation initiated." });
    }
}
