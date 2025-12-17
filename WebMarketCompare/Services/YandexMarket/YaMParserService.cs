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
using WebMarketCompare.Services.Wildberries;
using OpenQA.Selenium.Support.UI;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Html;


namespace WebMarketCompare.Services.YandexMarket
{
    public class YaMParserService : IYaMParserService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WBParserService> _logger;
        private readonly CookieContainer _cookieContainer;
        private ChromeOptions options = new ChromeOptions();

        public YaMParserService(HttpClient httpClient, ILogger<WBParserService> logger)
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

        private List<string> GetHtmlsFromUrl(List<string> pageUrls)
        {
            var result = new List<string>();

            using (var driver = new ChromeDriver(options))
            {
                try
                {
                    IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                    driver.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
                    driver.ExecuteScript("Object.defineProperty(navigator, 'userAgent', {get: () => 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36'})");

                    foreach (var pageUrl in pageUrls)
                    {
                        try
                        {
                            Console.WriteLine($"Загружаем страницу: {pageUrl}");

                            driver.Navigate().GoToUrl(pageUrl);

                            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                            wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));

                            Task.Delay(1000).Wait();

                            string pageHtml = driver.PageSource;
                            result.Add(pageHtml);

                            Console.WriteLine($"Страница загружена, размер HTML: {pageHtml.Length} символов");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при загрузке страницы {pageUrl}: {ex.Message}");
                            result.Add($"Error loading {pageUrl}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Общая ошибка: {ex.Message}");
                    return new List<string>();
                }
            }
            return result;
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

        public async Task<Product> ParseProductByUrlAsync(string url)
        {
            var product = new Product();
            product.ProductUrl = url;
            var html = GetHtmlsFromUrl(new List<string> { url })[0];
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            ParseCategory(htmlDoc, product);
            ParsePrice(htmlDoc, product);
            ParseSpecs(htmlDoc, product);
            return product;
        }

        private void ParseCategory(HtmlDocument htmlDoc, Product product)
        {
            var json = htmlDoc.DocumentNode.SelectSingleNode("//div[@id=\"/content/page/fancyPage/defaultPage/commonEcommerce/ecommerce\"]").SelectSingleNode(".//noframes").InnerText;
            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("collections", out var collections))
                {
                    if (collections.TryGetProperty("ecommerce", out var ecommerce))
                    {
                        var ecommerceItem = ecommerce.EnumerateObject().First().Value;
                        if (ecommerceItem.TryGetProperty("category", out var category))
                        {
                            product.CategoryName = category.GetString();
                        }
                        if (ecommerceItem.TryGetProperty("brand", out var brand))
                        {
                            product.Brand = brand.GetString();
                        }
                    }

                }
            }
            catch
            { }
        }

        private void ParsePrice(HtmlDocument htmlDoc, Product product)
        {
            var pricePath = "//div[@id='/content/page/fancyPage/defaultPage/mainDO/price/priceOffer']";
            var priceDivParent = htmlDoc.DocumentNode.SelectSingleNode(pricePath);
            var jsonPath = ".//noframes";
            var noframes = priceDivParent.SelectSingleNode(jsonPath);
            var json = noframes.InnerHtml;

            try
            {
                product.AverageRating = Convert.ToDouble(htmlDoc.DocumentNode.SelectSingleNode("//span[@data-auto='ratingValue']").InnerHtml.Replace('.', ','));
            }
            catch { Console.WriteLine("Не удалось получить рейтинг"); }

            try
            {
                product.SellerRating = Convert.ToDouble(htmlDoc.DocumentNode.SelectSingleNode("//span[@data-auto=\"shop-info-rating\"]").SelectSingleNode(".//span").InnerText.Replace('.', ','));
            }
            catch { Console.WriteLine("Не удалось получить рейтинг продавца"); }

            try
            {
                product.ReviewsCount = Convert.ToInt32(htmlDoc.DocumentNode.SelectSingleNode("//span[@data-auto=\"ratingCount\"]").InnerText.Split().First().Trim(new char[] { '(', ')' }).Replace('.', ','));
            }
            catch { Console.WriteLine("Не удалось получить количество отзывов"); }

            
            try
            {
                product.ImageUrl = htmlDoc.DocumentNode.SelectSingleNode("//div[@data-auto=\"image-gallery-nav-item\"]").SelectSingleNode(".//article").SelectSingleNode(".//img").GetAttributeValue("src", "");
            }
            catch { Console.WriteLine("Не удалось получить фотографию"); }



            try
            {
                var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("collections", out var collections))
                {
                    if (collections.TryGetProperty("buyOption", out var buyOption))
                    {
                        var buyOptionItem = buyOption.EnumerateObject().First().Value;
                        if (buyOptionItem.TryGetProperty("title", out var title))
                        {
                            product.ProductName = title.GetString();
                        }
                        if (buyOptionItem.TryGetProperty("skuId", out var skuId))
                        {
                            product.Article = skuId.GetString();
                        }
                        if (buyOptionItem.TryGetProperty("categoryId", out var categoryId))
                        {
                            product.CategoryId = categoryId.GetString();
                        }
                        if (buyOptionItem.TryGetProperty("price", out var price))
                        {
                            product.CurrentPrice = price.GetProperty("value").GetInt32();
                        }
                        if (buyOptionItem.TryGetProperty("businessName", out var businessName))
                        {
                            product.SellerName = businessName.GetString();
                        }
                        if (buyOptionItem.TryGetProperty("basePrice", out var basePrice))
                        {
                            product.OriginalPrice = basePrice.GetProperty("value").GetInt32();
                            product.IsAvailable = true;
                        }
                    }
                }
            }
            catch
            { }
        }

        private void ParseSpecs(HtmlDocument htmlDoc, Product product)
        {
            var specPath = "//div[@data-auto='specs-list-fullExtended']";
            var specDivParent = htmlDoc.DocumentNode.SelectSingleNode(specPath);
            var singleSpecPath = ".//span[@data-auto='product-spec']";
            var specDivs = specDivParent.SelectNodes(singleSpecPath);
            foreach (var specDiv in specDivs)
            {
                var parentNode = specDiv.ParentNode.ParentNode.ParentNode;
                var spanPath = ".//span";
                var specSpans = parentNode.SelectNodes(spanPath);
                var name = specSpans[0].InnerHtml.Trim();
                var value = specSpans[1].InnerHtml.Trim();
                //Console.WriteLine(name + value);
                product.Characteristics.Add(name, new Characteristic() { Name = name, Value = value, Category = product.CategoryName });
            }
        }
    }
}
