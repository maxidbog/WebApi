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

        private string GetJsonFromUrl(string url)
        {
            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    // Убираем webdriver property
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    driver.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");

                    // Сначала посещаем основную страницу
                    driver.Navigate().GoToUrl("https://www.ozon.ru/api/entrypoint-api.bx/page/json/v2?url=https://www.ozon.ru/product/2124720386");
                    Thread.Sleep(2000);
                    Console.WriteLine("Фурри топ");
                    //Выполняем JavaScript запрос к API
                    string apiUrl = "https://www.ozon.ru/api/entrypoint-api.bx/page/json/v2?url=https://www.ozon.ru/product/2124720386";
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

                    // Выполняем JavaScript и получаем результат
                    string jsonResult = js.ExecuteScript(script) as string;
                    Console.WriteLine("JSON Response:");
                    //Console.WriteLine(jsonResult);
                    return jsonResult;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    return "идёшь нахуй";
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
                // Получаем SKU из URL
                var sku = ExtractSkuFromUrl(productUrl);
                if (string.IsNullOrEmpty(sku))
                {
                    throw new ArgumentException("Не удалось извлечь SKU из URL");
                }

                return await ParseProductBySkuAsync(sku);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге товара по URL: {Url}", productUrl);
                throw;
            }
        }

        public async Task<Product> ParseProductBySkuAsync(string sku)
        {
            var apiUrl = "https://www.ozon.ru/api/composer-api.bx/page/json/v2?url=/product/1972913384";
            var response = GetJsonFromUrl(apiUrl);

            try
            {
                // Формируем URL для API Ozon
                var ozonResponse = JsonSerializer.Deserialize<OzonApiResponse>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Console.WriteLine( ozonResponse.WidgetStates);

                return ParseProductFromWidgetStates(sku, ozonResponse.WidgetStates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при парсинге товара по SKU: {Sku} {url}", sku, apiUrl);
                throw;
            }
        }

        private Product ParseProductFromWidgetStates(string sku, WidgetStates widgetStates)
        {
            var product = new Product { Sku = sku };

            // Парсим название товара
            ParseProductName(product, widgetStates);

            // Парсим цены
            ParsePrices(product, widgetStates);

            // Парсим рейтинг и отзывы
            ParseRating(product, widgetStates);

            // Парсим характеристики
            ParseCharacteristics(product, widgetStates);

            // Парсим изображения
            ParseImages(product, widgetStates);

            // Парсим бренд
            ParseBrand(product, widgetStates);

            // Парсим URL
            ParseUrl(product, widgetStates);

            return product;
        }

        private void ParseProductName(Product product, WidgetStates widgetStates)
        {
            var headingKey = widgetStates.States.Keys.FirstOrDefault(k => k.StartsWith("webProductHeading"));
            if (headingKey != null && widgetStates.States[headingKey] is string headingJson)
            {
                try
                {
                    var heading = JsonSerializer.Deserialize<ProductHeadingWidget>(headingJson);
                    product.Name = heading?.Title;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при парсинге названия товара");
                }
            }
        }

        private void ParsePrices(Product product, WidgetStates widgetStates)
        {
            var priceKey = widgetStates.States.Keys.FirstOrDefault(k => k.StartsWith("webPrice"));
            if (priceKey != null && widgetStates.States[priceKey] is string priceJson)
            {
                try
                {
                    var priceWidget = JsonSerializer.Deserialize<PriceWidget>(priceJson);
                    if (priceWidget != null)
                    {
                        product.Price = ParsePrice(priceWidget.Price);
                        product.OriginalPrice = ParsePrice(priceWidget.OriginalPrice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при парсинге цен");
                }
            }
        }

        private void ParseRating(Product product, WidgetStates widgetStates)
        {
            // Пробуем найти рейтинг в разных виджетах
            var ratingKeys = widgetStates.States.Keys.Where(k =>
                k.StartsWith("webSingleProductScore") || k.StartsWith("webReviewProductScore"));

            foreach (var key in ratingKeys)
            {
                if (widgetStates.States[key] is string ratingJson)
                {
                    try
                    {
                        var ratingWidget = JsonSerializer.Deserialize<RatingWidget>(ratingJson);
                        if (ratingWidget != null)
                        {
                            product.Rating = ratingWidget.TotalScore;
                            product.ReviewsCount = ratingWidget.ReviewsCount;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при парсинге рейтинга");
                    }
                }
            }
        }

        private void ParseCharacteristics(Product product, WidgetStates widgetStates)
        {
            var charKey = widgetStates.States.Keys.FirstOrDefault(k => k.StartsWith("webShortCharacteristics"));
            if (charKey != null && widgetStates.States[charKey] is string charJson)
            {
                try
                {
                    var charWidget = JsonSerializer.Deserialize<CharacteristicsWidget>(charJson);
                    if (charWidget?.Characteristics != null)
                    {
                        foreach (var charItem in charWidget.Characteristics)
                        {
                            var characteristic = new Characteristic
                            {
                                Name = charItem.Title?.TextRs?.FirstOrDefault()?.Content ?? "Unknown",
                                Value = charItem.Values?.FirstOrDefault()?.Text ?? "Unknown"
                            };
                            product.Characteristics.Add(characteristic);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при парсинге характеристик");
                }
            }
        }

        private void ParseImages(Product product, WidgetStates widgetStates)
        {
            var galleryKey = widgetStates.States.Keys.FirstOrDefault(k => k.StartsWith("webGallery"));
            if (galleryKey != null && widgetStates.States[galleryKey] is string galleryJson)
            {
                try
                {
                    var galleryWidget = JsonSerializer.Deserialize<GalleryWidget>(galleryJson);
                    if (galleryWidget?.Images != null)
                    {
                        product.ImageUrls = galleryWidget.Images
                            .Where(img => !string.IsNullOrEmpty(img.Source))
                            .Select(img => img.Source)
                            .ToList();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при парсинге изображений");
                }
            }
        }

        private void ParseBrand(Product product, WidgetStates widgetStates)
        {
            var brandKey = widgetStates.States.Keys.FirstOrDefault(k => k.StartsWith("webBrand"));
            if (brandKey != null && widgetStates.States[brandKey] is string brandJson)
            {
                try
                {
                    var brandWidget = JsonSerializer.Deserialize<BrandWidget>(brandJson);
                    product.Brand = brandWidget?.Content?.Title?.Text?.FirstOrDefault()?.Content;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при парсинге бренда");
                }
            }
        }

        private void ParseUrl(Product product, WidgetStates widgetStates)
        {
            var productKey = widgetStates.States.Keys.FirstOrDefault(k => k.StartsWith("webProductMainWidget"));
            if (productKey != null && widgetStates.States[productKey] is string productJson)
            {
                try
                {
                    var productWidget = JsonSerializer.Deserialize<ProductHeadingWidget>(productJson);
                    if (productWidget != null)
                    {
                        product.Url = $"https://www.ozon.ru{productWidget.Title}";
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ошибка при парсинге URL");
                }
            }
        }

        private decimal ParsePrice(string priceStr)
        {
            if (string.IsNullOrEmpty(priceStr)) return 0;

            // Удаляем все нечисловые символы, кроме точки и запятой
            var cleanPrice = new string(priceStr.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
                .Replace(',', '.');

            return decimal.TryParse(cleanPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price)
                ? price : 0;
        }

        private string ExtractSkuFromUrl(string url)
        {
            // Извлекаем SKU из URL вида ...-2124720386/
            var match = Regex.Match(url, @"-(\d+)/?$");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
