using System.Net;
using WebMarketCompare.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем HttpClient и сервисы
builder.Services.AddHttpClient<IOzonParserService, OzonParserService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        CookieContainer = new CookieContainer(),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        UseCookies = true,
        AllowAutoRedirect = true,
        UseProxy = false,
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

builder.Services.AddHttpClient<IWBParserService, WBParserService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        CookieContainer = new CookieContainer(),
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        UseCookies = true,
        AllowAutoRedirect = true,
        UseProxy = false,
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Настройка CORS для фронтенда
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("chrome-extension://ogkkecobghockcmhlfaokpoalniollbh")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();