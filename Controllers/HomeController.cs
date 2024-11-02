using Microsoft.AspNetCore.Mvc;
using ST10298613_POE.Models;
using ST10298613_POE.Services;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;


namespace ST10298613_POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableService _tableService;
        private readonly QueueService _queueService;
        private readonly FileService _fileService;

        public HomeController(BlobService blobService, TableService tableService, QueueService queueService, FileService fileService)
        {
            _blobService = blobService;
            _tableService = tableService;
            _queueService = queueService;
            _fileService = fileService;
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
                using var stream = file.OpenReadStream();
                await _blobService.UploadBlobAsync("product-images", file.FileName, stream);

                // Send data to Azure Function
                var data = new { containerName = "product-images", blobName = file.FileName };
                await PostDataToFunction("https://st10298613functionapp.azurewebsites.net/api/UploadBlob?code=Icc5XsxFVaFVbiVqkpKTSjhyJCstHASGjwkv7d7ywod0AzFu3RBG7g%3D%3D", data);
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> AddCustomerProfile(CustomerProfile profile)
        {
            if (ModelState.IsValid)
            {
                await _tableService.AddEntityAsync(profile);

                // Send data to Azure Function
                var data = new
                {
                    tableName = "CustomerProfiles",
                    partitionKey = profile.PartitionKey,
                    rowKey = profile.RowKey,
                    data = JsonConvert.SerializeObject(profile)
                };
                await PostDataToFunction("https://st10298613functionapp.azurewebsites.net/api/StoreTableInfo?code=EHP0iPW2tCn8gfkovdrwFg2RJTULXcf7sxSihc9geoWUAzFuR0OGAA%3D%3D", data);
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
