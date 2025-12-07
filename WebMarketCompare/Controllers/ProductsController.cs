using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using WebMarketCompare.Models;
using WebMarketCompare.Services;

namespace WebMarketCompare.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IOzonParserService _ozonParserService;
        private readonly IWBParserService _wbParserService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IOzonParserService ozonParserService, ILogger<ProductsController> logger, IWBParserService wbParserService)
        {
            _ozonParserService = ozonParserService;
            _logger = logger;
            _wbParserService = wbParserService;
        }

        [HttpGet("by-url")]
        public async Task<ActionResult<Product>> GetProductByUrl([FromQuery] string url)
        {
            var product = new Product();
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return BadRequest("URL обязателен");
                }

                product = await GetProductByUrlAsync(url);

                if (product == null) return BadRequest("Данный маркетплейс не поддерживается");

                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении товара по URL: {Url}", url);
                return StatusCode(500, "Ошибка при получении данных о товаре");
            }
        }

        [HttpGet("by-sku-ozon/{sku}")]
        public async Task<ActionResult<Product>> GetProductBySku(string sku)
        {
            try
            {
                //if (string.IsNullOrEmpty(sku))
                //{
                //    return BadRequest("SKU обязателен");
                //}

                var product = await _ozonParserService.ParseProductBySkuAsync(sku);
                return Ok(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении товара по SKU: {Sku}", sku);
                return StatusCode(500, "Ошибка при получении данных о товаре блять");
            }
        }

        [HttpPost("batch-sku-ozon")]
        public async Task<ActionResult<List<Product>>> GetProductsBatch([FromBody] List<string> skus)
        {
            try
            {
                if (skus == null || !skus.Any())
                {
                    return BadRequest("Список SKU обязателен");
                }

                var tasks = skus.Select(sku => _ozonParserService.ParseProductBySkuAsync(sku));
                var products = await Task.WhenAll(tasks);

                return Ok(products.Where(p => p != null).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при пакетном получении товаров");
                return StatusCode(500, "Ошибка при получении данных о товарах");
            }
        }

        [HttpPost("batch-url")]
        public async Task<ActionResult<List<Product>>> GetProductsBatchUrl([FromBody] List<string> urls)
        {
            try
            {
                if (urls == null || !urls.Any())
                {
                    return BadRequest("Список URL обязателен");
                }

                var tasks = urls.Select(url => GetProductByUrlAsync(url));
                var products = await Task.WhenAll(tasks);

                return Ok(products.Where(p => p != null).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при пакетном получении товаров");
                return StatusCode(500, "Ошибка при получении данных о товарах");
            }
        }

        [HttpPost("compare")]
        public async Task<ActionResult<List<Product>>> CompareProducts ([FromBody] List<Product> products)
        {
            try
            {
                if (products == null)
                {
                    return BadRequest("Список товаров обязателен");
                }
                return Ok(CompareMarkerService.MarkBestCharacteristicsAsync(products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при пакетном получении товаров");
                Console.WriteLine(ex);
                return StatusCode(500, "Ошибка при получении данных о товарах");
            }
        }

        private async Task<Product> GetProductByUrlAsync (string url)
        {
            var uri = new Uri(url);

            switch (uri.Authority)
            {
                case "www.ozon.ru":
                    {
                        return await _ozonParserService.ParseProductAsync(url);
                        break;
                    }

                case "www.wildberries.ru":
                    {
                        return await _wbParserService.ParseProductAsync(url);
                        break;
                    }

                default:
                    return null;
            }
        }
    }
}
