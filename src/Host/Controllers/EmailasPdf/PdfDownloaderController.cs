using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using sib_api_v3_sdk.Model;
using System.Text;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.EmailasPdf;

[Route("api/[controller]")]
[ApiController]
public class PdfDownloaderController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> SavePdf([FromBody] PdfDataModel model)
    {

        // Retrieve the PDF data and file name from the model
        var base64Data = model.PdfData;
        var fileName = model.FileName;

        // Decode the base64-encoded PDF data
        var pdfData = Convert.FromBase64String(base64Data);

        // Save the PDF file using the specified file name
        var folderName = "SecureHands"; // Replace with the desired folder name

        // Get the base directory of the project
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Combine the base directory with the folder name
        var folderPath = Path.Combine(baseDirectory, folderName);

        // Create the folder if it doesn't exist
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var filePath = Path.Combine(folderPath, fileName);

        await System.IO.File.WriteAllBytesAsync(filePath, pdfData);

        byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
        // Attach the PDF file to the SendinBlue email
        var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.sendinblue.com/v3/");
        client.DefaultRequestHeaders.Add("api-key", "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");

        var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");

        var sendSmtpEmail = new SendSmtpEmail();
        sendSmtpEmail.To = new List<SendSmtpEmailTo>();
        sendSmtpEmail.To.Add(new SendSmtpEmailTo(email: "999govindagovinda@gmail.com", name: "Govinda"));
        sendSmtpEmail.TemplateId = 6;
        sendSmtpEmail.Params = new Dictionary<string, string> { { "FirstName", "FocusLokesh" }, { "Age", "10" }, { "surname", "Doe" } };
        sendSmtpEmail.Headers = new Dictionary<string, string> { { "X-Mailin-custom", "custom_header_1:custom_value_1|custom_header_2:custom_value_2" } };
        //string fileBase64 = Convert.ToBase64String(fileBytes);
        byte[] fileBase64 = Convert.FromBase64String(Convert.ToBase64String(fileBytes));
        var attachment = new SendSmtpEmailAttachment
        {
            Content = fileBase64,
            Name = "quotation.pdf"
        };

        sendSmtpEmail.Attachment = new List<SendSmtpEmailAttachment> { attachment };
        var jsonContent = JsonConvert.SerializeObject(sendSmtpEmail);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            // Email sent successfully
            return Ok();
        }
        else
        {
            // Handle the error case
            var responseContent1 = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, responseContent1);
        }
    }
}

public class PdfDataModel
{
    public string PdfData { get; set; }
    public string FileName { get; set; }
}
