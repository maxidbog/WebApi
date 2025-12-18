using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using OfficeOpenXml;

public interface IStandardNamingService
{
    public string GetStandardName(string originalName);
    public bool IsInStandartSet(string name);
}

public class StandardNamingService : IStandardNamingService
{
    private Dictionary<string, string> mapping;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StandardNamingService> _logger;


    public StandardNamingService(IConfiguration configuration, ILogger<StandardNamingService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        LoadDictionaries();
    }

    private void LoadDictionaries()
    {
        try
        {
            var excelPath = _configuration["ExcelSettings:FilePath"];

            if (!File.Exists(excelPath))
            {
                _logger.LogError($"Excel file not found: {excelPath}");
                return;
            }

            using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(excelPath)))
            {
                    ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
                    var worksheet = package.Workbook.Worksheets[0];

                    mapping = new Dictionary<string, string>();

                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    for (int row = 1; row <= rowCount; row++)
                    {
                        if (worksheet.Cells[row, 1].Value == null ||
                            worksheet.Cells[row, 2].Value == null)
                            continue;

                        var key = worksheet.Cells[row, 1].Value.ToString();
                        var value = worksheet.Cells[row, 2].Value.ToString();

                        if (!mapping.ContainsKey(key))
                        {
                            mapping.Add(key, value);
                        }
                    }
                }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dictionaries from Excel file");
        }
    }


    private HashSet<string> _ComparableCategories = new HashSet<string>
    {
        "Игровые ноутбуки",
        "Ноутбуки",
        "Смартфоны",
        "Наушники",
        "Игровые наушники",
        "Наушники и гарнитуры",
        "Смарт-часы и браслеты",
        "Смарт-часы",
        "Мобильные телефоны",
        "SIM-карты",
        "Мониторы",
        "Системные блоки",
        "Компьютеры и моноблоки",
        "Моноблоки",
        "Компьютеры",
        "Веб-камеры"
    };

    public string GetStandardName(string originalName)
    {
        if (mapping.ContainsKey(originalName.Trim()))
            return mapping[originalName.Trim()];
        return null;
    }

    public Dictionary<string, List<string>> GetGroups()
    {
        return mapping.GroupBy(kv => kv.Value)
                      .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());
    }

    public bool IsInStandartSet(string name)
    {
        return _ComparableCategories.Contains(name);
    }
}
