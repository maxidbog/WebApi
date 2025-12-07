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
    public class OzonParserService : IOzonParserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OzonParserService> _logger;
        private readonly CookieContainer _cookieContainer;
        private ChromeOptions options = new ChromeOptions();

        public OzonParserService(HttpClient httpClient, ILogger<OzonParserService> logger)
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

                    Task.Delay(100).Wait();

                    // Сначала посещаем основную страницу
                    driver.Navigate().GoToUrl("https://www.ozon.ru/api/entrypoint-api.bx/page/json/v2?url=https://www.ozon.ru/product/2124720386");
                    Task.Delay(2000).Wait();
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
                            'referer': 'https://www.ozon.ru/',
                            'user-agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36'
                        }}
                    }})
                    .then(response => response.text())
                    .then(data => data)
                    .catch(error => 'Error: ' + error);
                ";
                        Console.WriteLine("Extracting JSON...");
                        string jsonResult = js.ExecuteScript(script) as string;
                        //Console.WriteLine(jsonResult);
                        result.Add(jsonResult);
                        Task.Delay(100).Wait();
                    }
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return new List<string>();
                }
                driver.Dispose();
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
            var url = "https://www.ozon.ru/product/" + Sku;
            return await ParseProductByUrlAsync(url);
        }

        public async Task<Product> ParseProductByUrlAsync(string url)
        {

            //var apiUrl = "https://www.ozon.ru/api/composer-api.bx/page/json/v2?url=" + url;
            //var json = GetJsonFromUrl(apiUrl);
            var product = new Product();
            product.ProductUrl = url;
            var apiUrlMain = "https://www.ozon.ru/api/composer-api.bx/page/json/v2?url=" + url;
            var apiUrlCharac = apiUrlMain + "&layout_container=pdpPage2column&layout_page_index=2";
            try
            {
                var jsons = GetJsonsFromUrl(new List<string> { apiUrlMain, apiUrlCharac});
                ExtractProductData(jsons[0], product);
                ExtractProductCharacteristics(jsons[1], product);

                return product;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге товара по Url: {url}", url);
                throw;
            }
        }

        private void ExtractProductData(string json, Product product)
        {
            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("widgetStates", out var widgetStates))
                {
                    // Название товара
                    if (widgetStates.TryGetProperty("webStickyProducts-726428-default-1", out var stickyProduct))
                    {
                        var stickyProductJson = stickyProduct.GetString();
                        if (!string.IsNullOrEmpty(stickyProductJson))
                        {
                            var productDoc = JsonDocument.Parse(stickyProductJson);
                            if (productDoc.RootElement.TryGetProperty("name", out var nameProperty))
                            {
                                product.ProductName = nameProperty.GetString();
                            }
                        }
                    }

                    // Цена и оригинальная цена
                    if (widgetStates.TryGetProperty("webPrice-3121879-default-1", out var priceElement))
                    {
                        var priceJson = priceElement.GetString();
                        if (!string.IsNullOrEmpty(priceJson))
                        {
                            var priceDoc = JsonDocument.Parse(priceJson);
                            var priceRoot = priceDoc.RootElement;

                            if (priceRoot.TryGetProperty("cardPrice", out var cardPriceProperty))
                            {
                                var value = cardPriceProperty.GetString();
                                if (int.TryParse(new string(value.Where(c => char.IsDigit(c)).ToArray()), out int cardPrice))
                                {
                                    product.CardPrice = cardPrice;
                                }
                            }

                            if (priceRoot.TryGetProperty("price", out var priceProperty))
                            {
                                var value = priceProperty.GetString();
                                if (int.TryParse(new string(value.Where(c => char.IsDigit(c)).ToArray()), out int price))
                                {
                                    product.CurrentPrice = price;
                                }
                            }

                            if (priceRoot.TryGetProperty("originalPrice", out var originalPriceProperty))
                            {
                                var value = originalPriceProperty.GetString();
                                if (int.TryParse(new string(value.Where(c => char.IsDigit(c)).ToArray()), out int originalPrice))
                                {
                                    product.OriginalPrice = originalPrice;
                                }
                            }

                            if (priceRoot.TryGetProperty("isAvailable", out var availableProperty))
                            {
                                product.IsAvailable = availableProperty.GetBoolean();
                            }
                        }
                    }

                    // Артикул (SKU)
                    if (widgetStates.TryGetProperty("webProductMainWidget-347746-default-1", out var skuElement))
                    {
                        var skuJson = skuElement.GetString();
                        if (!string.IsNullOrEmpty(skuJson))
                        {
                            var skuDoc = JsonDocument.Parse(skuJson);
                            if (skuDoc.RootElement.TryGetProperty("sku", out var sku))
                            {
                                product.Article = sku.GetString();
                            }
                        }
                    }

                    // Бренд
                    if (widgetStates.TryGetProperty("webBrand-3530421-default-1", out var brandElement))
                    {
                        var brandJson = brandElement.GetString();
                        if (!string.IsNullOrEmpty(brandJson))
                        {
                            var brandDoc = JsonDocument.Parse(brandJson);
                            var brandRoot = brandDoc.RootElement;

                            // Извлекаем бренд из content -> title -> text
                            if (brandRoot.TryGetProperty("content", out var contentElement) &&
                                contentElement.TryGetProperty("title", out var titleElement) &&
                                titleElement.TryGetProperty("text", out var textElement))
                            {
                                // text может быть массивом, берем первый элемент с content
                                if (textElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var item in textElement.EnumerateArray())
                                    {
                                        if (item.TryGetProperty("content", out var brandContent))
                                        {
                                            product.Brand = brandContent.GetString();
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Рейтинг и количество отзывов
                    if (widgetStates.TryGetProperty("webSingleProductScore-3386432-default-1", out var ratingElement))
                    {
                        var ratingJson = ratingElement.GetString();
                        if (!string.IsNullOrEmpty(ratingJson))
                        {
                            var ratingDoc = JsonDocument.Parse(ratingJson);
                            var ratingRoot = ratingDoc.RootElement;

                            // Парсим текст вида "4.8 • 609 отзывов"
                            if (ratingRoot.TryGetProperty("text", out var ratingTextProperty))
                            {
                                var ratingText = ratingTextProperty.GetString();
                                if (!string.IsNullOrEmpty(ratingText))
                                {
                                    var parts = ratingText.Split('•');
                                    if (parts.Length >= 2)
                                    {
                                        if (double.TryParse(parts[0].Trim().Replace('.', ','), out double rating))
                                        {
                                            product.AverageRating = rating;
                                        }

                                        // Извлекаем число из "609 отзывов"
                                        var reviewsText = parts[1].Trim();
                                        var digits = new string(reviewsText.Where(char.IsDigit).ToArray());
                                        if (int.TryParse(digits, out int reviewsCount))
                                        {
                                            product.ReviewsCount = reviewsCount;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Изображение
                    if (widgetStates.TryGetProperty("webGallery-3311626-default-1", out var galleryElement))
                    {
                        var galleryJson = galleryElement.GetString();
                        if (!string.IsNullOrEmpty(galleryJson))
                        {
                            var galleryDoc = JsonDocument.Parse(galleryJson);
                            if (galleryDoc.RootElement.TryGetProperty("coverImage", out var imageProperty))
                            {
                                product.ImageUrl = imageProperty.GetString();
                            }
                        }
                    }

                    // Продавец
                    if (widgetStates.TryGetProperty("webCurrentSeller-7772769-default-1", out var webCurrentSeller))
                    {
                        var sellerJson = webCurrentSeller.GetString();
                        if (!string.IsNullOrEmpty(sellerJson))
                        {
                            var sellerDoc = JsonDocument.Parse(sellerJson);
                            try
                            {
                                var seller = sellerDoc.RootElement.GetProperty("sellerCell").GetProperty("centerBlock").GetProperty("title").GetProperty("text");
                                product.SellerName = seller.ToString();
                            }
                            catch { }

                            try
                            {
                                var rating = sellerDoc.RootElement.GetProperty("rating").GetProperty("title").GetProperty("text");
                                if (double.TryParse(rating.ToString().Trim().Replace('.', ','), out double sellerRating))
                                {
                                    product.SellerRating = sellerRating;
                                }
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    throw new Exception("в файле не найден widgetStates ");
                }

                // Категория
                if (root.TryGetProperty("layoutTrackingInfo", out var layoutTrackingInfo))
                {
                    var layoutTrackingInfoJson = layoutTrackingInfo.GetString();
                    if (!string.IsNullOrEmpty(layoutTrackingInfoJson))
                    {
                        var productDoc = JsonDocument.Parse(layoutTrackingInfoJson);
                        if (productDoc.RootElement.TryGetProperty("categoryId", out var categoryId))
                        {
                            product.CategoryId = categoryId.GetDecimal().ToString();
                        }
                        if (productDoc.RootElement.TryGetProperty("categoryName", out var categoryName))
                        {
                            product.CategoryName = categoryName.GetString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при извлечении данных: {ex.Message}");
            }
        }

        private void ExtractProductCharacteristics(string json, Product product)
        {
            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("widgetStates", out var widgetStates))
                {
                    // Название товара
                    if (widgetStates.TryGetProperty("webCharacteristics-3282540-pdpPage2column-2", out var webCharacteristics))
                    {
                        var webCharacteristicsJson = webCharacteristics.GetString();
                        if (!string.IsNullOrEmpty(webCharacteristicsJson))
                        {
                            var productDoc = JsonDocument.Parse(webCharacteristicsJson);
                            var rootDir = productDoc.RootElement;

                            // Создаем список характеристик
                            var characteristics = product.Characteristics;

                            // Получаем массив характеристик
                            if (rootDir.TryGetProperty("characteristics", out var characteristicsArray))
                            {
                                foreach (var category in characteristicsArray.EnumerateArray())
                                {
                                    var categoryName = characteristicsArray.GetArrayLength() == 1 ? null : category.GetProperty("title").GetString();

                                    // Обрабатываем характеристики внутри категории
                                    if (category.TryGetProperty("short", out var shortArray))
                                    {
                                        foreach (var charItem in shortArray.EnumerateArray())
                                        {
                                            var characteristic = new Characteristic
                                            {
                                                Name = charItem.GetProperty("name").GetString()
                                            };

                                            // Получаем значения характеристики (может быть несколько)
                                            if (charItem.TryGetProperty("values", out var valuesArray))
                                            {
                                                var values = new List<string>();
                                                foreach (var valueItem in valuesArray.EnumerateArray())
                                                {
                                                    values.Add(valueItem.GetProperty("text").GetString());
                                                }
                                                characteristic.Value = string.Join(", ", values);
                                            }

                                            characteristic.Category = categoryName;

                                            characteristics.Add(characteristic.Name, characteristic);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при извлечении данных: {ex.Message}");
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
