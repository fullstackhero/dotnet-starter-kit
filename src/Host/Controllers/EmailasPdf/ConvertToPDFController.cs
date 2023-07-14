using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.EmailasPdf;

public class ConvertToPdfController : ControllerBase
{
    [HttpPost("convert-to-pdf")]
    public static IActionResult ConvertPDF([FromBody] string htmls)
    {
        // Create a new PDF document
        Document document = new Document();

        // Create a new MemoryStream to hold the generated PDF content
        MemoryStream stream = new MemoryStream();

        // Create a PdfWriter instance to write the PDF document to the MemoryStream
        PdfWriter writer = PdfWriter.GetInstance(document, stream);

        // Open the document
        document.Open();

        // Create an HTMLWorker to parse the HTML content and add it to the document
        using (TextReader reader = new StringReader(htmls))
        {
            HTMLWorker worker = new HTMLWorker(document);
            worker.Parse(reader);
        }

        // Close the document
        document.Close();

        // Set the position of the MemoryStream back to the beginning
        stream.Position = 0;

        // Return the PDF file to the client
        var fileContentResult = new FileContentResult(stream.ToArray(), "application/pdf")
        {
            FileDownloadName = "output.pdf"
        };

        return fileContentResult;

    }
}
