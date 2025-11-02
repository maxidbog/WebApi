using static WebMarketCompare.Models.OzonApiResponse;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebMarketCompare.Models;
using static System.Net.Mime.MediaTypeNames;
using System.Net;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Microsoft.AspNetCore.Mvc;
using static System.Net.WebRequestMethods;


namespace WebMarketCompare.Services
{
    public class WBParserService : IWBParserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WBParserService> _logger;
        private readonly CookieContainer _cookieContainer;
        private ChromeOptions options = new ChromeOptions();

        public WBParserService(HttpClient httpClient, ILogger<WBParserService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cookieContainer = new CookieContainer();
            ConfigureHttpClient();
            ConfigureWebDriver();
        }

        private void ConfigureWebDriver()
        {
            options.AddArgument("--incognito");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);
        }

        private List<string> GetJsonsFromUrl(List<string> apiUrls)
        {
            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    // Убираем webdriver property
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    driver.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
                    //Выполняем JavaScript запрос к API
                    var result = new List<string>();
                    foreach (var apiUrl in apiUrls)
                    {
                        string script = $@"
                    return fetch('{apiUrl}', {{
                        method: 'GET',
                        headers: {{
                            'accept': 'application/json',
                            'content-type': 'application/json',
                            'referer': 'https://www.wildberries.ru/',
                            'user-agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36'
                        }}
                    }})
                    .then(response => response.text())
                    .then(data => data)
                    .catch(error => 'Error: ' + error);
                ";

                        driver.Navigate().GoToUrl(apiUrl);
                        // Выполняем JavaScript и получаем результат
                        string jsonResult = js.ExecuteScript(script) as string;
                        Console.WriteLine("JSON Response:");
                        //Console.WriteLine(jsonResult);
                        result.Add(jsonResult);
                        Task.Delay(10).Wait();
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return new List<string>();
                }

            }


        }

        private void ConfigureHttpClient()
        {
            // Настройка безопасности TLS
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;

            // Базовые настройки HttpClient
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Clear();

            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            _httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");

            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
            _httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"");
            _httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
            _httpClient.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
            _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
            _httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

            // Настройка для автоматической обработки cookies
            var handler = new HttpClientHandler()
            {
                CookieContainer = _cookieContainer,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseCookies = true,
                AllowAutoRedirect = true,
                UseProxy = false // Отключаем прокси для избежания дополнительных проблем
            };

            // Если используем кастомный handler, нужно пересоздать HttpClient
            // Но в нашем случае мы настроим его через DI
        }

        public async Task<Product> ParseProductAsync(string productUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(productUrl))
                {
                    throw new ArgumentException("URL не может быть пустым!");
                }

                return await ParseProductByUrlAsync(productUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге товара по URL: {Url}", productUrl);
                throw;
            }
        }

        public async Task<Product> ParseProductBySkuAsync(string Sku)
        {
            var product = new Product();

            var vol = Sku.Substring(0, Sku.Length - 5);
            var part = Sku.Substring(0, Sku.Length - 3);

            var url = "https://www.wildberries.ru/catalog/" + Sku +"/detail.aspx";
            product.ProductUrl = url;

            var imageUrl = "https://ekt-basket-cdn-01.geobasket.ru/vol" + vol + "/part" + part + "/" + Sku + "/images/big/1.webp";
            product.ImageUrl = imageUrl;


            var apiUrlCard = "https://ekt-basket-cdn-01.geobasket.ru/vol" + vol + "/part" + part + "/" + Sku + "/info/ru/card.json";
            var apiUrlDetails = "https://u-card.wb.ru/cards/v4/detail?appType=1&dest=12358514&nm=" + Sku;

            try
            {
                var jsons = GetJsonsFromUrl(new List<string> { apiUrlCard, apiUrlDetails });
                ExtractCard(jsons[0], product);
                ExtractPrice(jsons[1], product);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге товара WB: {url}", url);
                throw;
            }
        }

        public async Task<Product> ParseProductByUrlAsync(string url)
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/');
            var sku = segments[2];
            return await ParseProductBySkuAsync(sku);
        }

        private void ExtractCard(string cardJson, Product product)
        {
            var document = JsonDocument.Parse(cardJson);
            var root = document.RootElement;

            if (root.TryGetProperty("nm_id", out var nmId))
            {
                product.Article = nmId.ToString();
            }

            if (root.TryGetProperty("imt_name", out var name))
            {
                product.ProductName = name.GetString();
            }

            if (root.TryGetProperty("subj_name", out var category))
            {
                product.CategoryName = category.GetString();
            }

            if (root.TryGetProperty("options", out var options))
            {
                foreach (var option in options.EnumerateArray())
                {
                    var charname = option.GetProperty("name").GetString();
                    var charvalue = option.GetProperty("value").GetString();
                    product.Characteristics.Add(charname, new Characteristic { Category = product.CategoryName, Name = charname, Value = charvalue });
                }
            }

            //if (root.TryGetProperty("subj_root_name", out var category))
            //{
            //    product.CategoryName = category.GetString();
            //}
        }

        private void ExtractPrice(string priceJson, Product product)
        {
            var document = JsonDocument.Parse(priceJson);
            var root = document.RootElement;

            if (root.TryGetProperty("products", out var options))
            {
                var productdoc = options.EnumerateArray().First();

                if (productdoc.TryGetProperty("brand", out var brand))
                {
                    product.Brand = brand.GetString();
                }

                if (productdoc.TryGetProperty("supplier", out var supplier))
                {
                    product.SellerName = supplier.GetString();
                }

                if (productdoc.TryGetProperty("supplierRating", out var supplierRating))
                {
                    product.SellerRating = supplierRating.GetDouble();
                }

                if (productdoc.TryGetProperty("reviewRating", out var reviewRating))
                {
                    product.AverageRating = reviewRating.GetDouble();
                }

                if (productdoc.TryGetProperty("feedbacks", out var feedbacks))
                {
                    product.ReviewsCount = feedbacks.GetInt32();
                }

                if (productdoc.TryGetProperty("totalQuantity", out var totalQuantity))
                {
                    var quantity = totalQuantity.GetInt32();
                    product.StockQuantity = quantity;
                    if (quantity > 0) product.IsAvailable = true;
                }

                if (productdoc.TryGetProperty("sizes", out var sizes))
                {
                    var sizeDir = sizes.EnumerateArray().First();

                    if (sizeDir.TryGetProperty("price", out var priceDir))
                    {
                        if (priceDir.TryGetProperty("basic", out var originalPrice))
                        {
                            product.OriginalPrice = originalPrice.GetDecimal() / 100;
                        }

                        if (priceDir.TryGetProperty("product", out var currentPrice))
                        {
                            product.CurrentPrice = currentPrice.GetDecimal() / 100;
                        }
                    }
                }

                //if (productdoc.TryGetProperty("feedbacks", out var feedbacks))
                //{
                //    product.ReviewsCount = feedbacks.GetInt32();
                //}
            }
        }

        private void SetProductDataFromCharacteristics(Characteristic characteristic, Product product)
        {
            if (characteristic.Name == "Бренд")
                product.Brand = characteristic.Value;

            switch (characteristic.Name)
            {
                case "Бренд":
                    product.Brand = characteristic.Value;
                    break;
                    //    case "Бренд":
                    //        product.Brand = characteristic.Value;
                    //        break;
                    //    case "Бренд":
                    //        product.Brand = characteristic.Value;
                    //        break;
                    //    case "Бренд":
                    //        product.Brand = characteristic.Value;
                    //        break;
                    //}
                    return;
            }
        }
    }
}
