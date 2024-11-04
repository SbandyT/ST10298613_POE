using Microsoft.AspNetCore.Mvc;
using ST10298613_POE.Models;
using ST10298613_POE.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;


namespace ST10298613_POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableService _tableService;
        private readonly QueueService _queueService;
        private readonly FileService _fileService;
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
       


        public HomeController(BlobService blobService, TableService tableService, QueueService queueService, FileService fileService, ILogger<HomeController> logger, IConfiguration configuration)
        {
            _blobService = blobService;
            _tableService = tableService;
            _queueService = queueService;
            _fileService = fileService;
            _logger = logger;
            _configuration = configuration;
            
            
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file != null)
            {
                try
                {
                    using var stream = file.OpenReadStream();
                    await _blobService.UploadBlobAsync("product-images", file.FileName, stream);

                    var baseUrl = _configuration["AzureFunctions:UploadBlob"];
                    var requestUri = $"{baseUrl}&blobName={file.FileName}";
                    using var httpClient = new HttpClient();
                    var content = new StreamContent(stream);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);

                    var response = await httpClient.PostAsync(requestUri, content);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await file.CopyToAsync(memoryStream);
                            var imageData = memoryStream.ToArray();

                            // Insert image data into SQL BlobTable
                            await _blobService.InsertBlobAsync(imageData);
                        }
                    }
                    else
                    {
                        _logger.LogError($"Error submitting image data: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occurred while submitting image: {ex.Message}");
                }
            }
            else
            {
                _logger.LogError("No image file provided.");
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> AddCustomerProfile(CustomerProfile profile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _tableService.AddEntityAsync(profile);

                    var baseUrl = _configuration["AzureFunctions:StoreTableInfo"];
                    var requestUri = $"{baseUrl}&tableName=CustomerProfiles&partitionKey={profile.PartitionKey}&rowKey={profile.RowKey}&firstName={profile.FirstName}&lastName={profile.LastName}&email={profile.Email}&phoneNumber={profile.PhoneNumber}";

                    using var httpClient = new HttpClient();
                    var response = await httpClient.PostAsync(requestUri, null);

                    if (response.IsSuccessStatusCode)
                    {
                        await _tableService.InsertCustomerAsync(profile);
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        _logger.LogError($"Error submitting client info: {response.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception occurred while submitting client info: {ex.Message}");
                }
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            await _queueService.SendMessageAsync("order-processing", $"Processing order {orderId}");

            // Send data to Azure Function
            var data = new { queueName = "order-processing", message = $"Processing order {orderId}" };
            await PostDataToFunction("https://st10298613functionapp.azurewebsites.net/api/WriteToQueueMessage?code=VlVDfLEIE3WtCrvu7k8I2Dx4aEoX2XPWCrjwD4iG0gYgAzFuTJURZA%3D%3D", data);

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                await _fileService.UploadFileAsync("contracts-logs", file.FileName, stream);

                // Send data to Azure Function
                var data = new { shareName = "contracts-logs", fileName = file.FileName };
                await PostDataToFunction("https://<your-function-app-url>/api/WriteToAzureFiles", data);
            }
            return RedirectToAction("Index");
        }
        private async Task PostDataToFunction(string url, object data)
        {
            using (var client = new HttpClient())
            {
                var jsonData = JsonConvert.SerializeObject(data);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    // Log or handle the error as needed
                }
            }
        }
    }
}
