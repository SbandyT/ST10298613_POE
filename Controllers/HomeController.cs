using Microsoft.AspNetCore.Mvc;
using ST10298613_POE.Models;
using ST10298613_POE.Services;
using System.Diagnostics;
using System.Threading.Tasks;

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
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomerProfile(CustomerProfile profile)
        {
            if (ModelState.IsValid)
            {
                await _tableService.AddEntityAsync(profile);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            await _queueService.SendMessageAsync("order-processing", $"Processing order {orderId}");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file != null)
            {
                using var stream = file.OpenReadStream();
                await _fileService.UploadFileAsync("contracts-logs", file.FileName, stream);
            }
            return RedirectToAction("Index");
        }
    }
}
