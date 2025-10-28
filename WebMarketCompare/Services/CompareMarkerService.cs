using WebMarketCompare.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace WebMarketCompare.Services
{
    public class CompareMarkerService
    {
        static ChatClient client = new ChatClient(
            model: "deepseek-v3",
            credential: new ApiKeyCredential("sk-voidai-xFcSkgOmQA5ToUTKetA7xKWcScWDC_XZ6XFVWUqxgYCluJdv6vtl-5ebBs2BxInEEzcL4PqI9M5mtmKDBEDeuBUSOps1qYOd4iZ3hUIPZVCPIM9T9ptHHqgGiYAeN1ylZuirwA"),
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri("https://api.voidai.app/v1/")
            }
            );

        public static async Task<List<Product>> MarkBestCharacteristicsAsync(List<Product> products)
        {
            var charactDict = GetCharacteristicsDict(products);
            MarkBestCharacteristic(charactDict);
            return products;

        }
        private static Dictionary<string, List<(Product, string)>> GetCharacteristicsDict(List<Product> products)
        {
            var dictionary = new Dictionary<string, List<(Product, string)>> { };
            foreach (var product in products)
            {
                foreach (var characteristic in product.Characteristics)
                {
                    var character = characteristic.Value;
                    if (!dictionary.ContainsKey(character.Name))
                        dictionary.Add(character.Name, new List<(Product, string)> { });
                    dictionary[character.Name].Add((product, character.Value));
                }
            }
            return dictionary;
        }

        private static void MarkBestCharacteristic(Dictionary<string, List<(Product, string)>> CharDict)
        {
            var list = new List<string> { "Процессор", "Видеопроцессор", "Функции камеры", "Встроенные датчики", "Модуль связи WiFi", "Интерфейсы" };
            var AiString = string.Empty;
            foreach (var characteristic in CharDict)
            {
                if (CompareDirections.TryGetComparisonDirection(characteristic.Key, characteristic.Value[0].Item1.CategoryName, out var charType))
                {
                    var bests = FindBests(characteristic.Value, charType, characteristic.Key);
                    foreach (var charac in bests)
                        charac.IsBest = true;
                }
                else if (true)
                {
                    //var bests = FindBestsAI(characteristic.Value, charType, characteristic.Key);
                    //foreach (var charac in bests)
                    //    charac.IsBest = true;
                    AiString += $"{characteristic.Key} : {string.Join("; ", characteristic.Value.Select(x => x.Item2))}\n";
                }
                //Console.WriteLine(characteristic.Key + " не в списке сравнения");
            }
            MarkBestAi(CharDict, AiString);
            Console.WriteLine(AiString);

        }

        private static void MarkBestAi(Dictionary<string, List<(Product, string)>> Dict, string charString)
        {
            var headerString = @"Проанализируй список характеристик товаров. Сравни их и для каждой категории характеристик выбери единственный вариант с НАИЛУЧШИМ значением.

**Правила форматирования ответа:**
- Каждая характеристика — с новой строки.
- Формат: `Название характеристики: Значение`
- Не меняй строки
- Не включай в ответ никакие другие комментарии, пояснения или символы.

**Критерии отбора:**
1.  Характеристика должна быть объективно сравнимой (например, тактовая частота процессора, тип матрицы экрана, объем памяти). Чем выше число или новее/качественнее технология, тем лучше.
2.  Если характеристику нельзя объективно сравнить для выявления ""лучшего"" (например, цвет, материал корпуса, артикул, название операционной системы), НЕ ВКЛЮЧАЙ ее в ответ.
3.  Если все значения в категории идентичны и не имеют лучшего (например, все ""Смартфон"" или ""Нет""), НЕ ВКЛЮЧАЙ ее в ответ.
4.  Характеристики отделены '; '

**Входные данные для анализа:**";
            ChatCompletion completion = client.CompleteChat(headerString + "\n" + charString);
            Console.WriteLine(completion.Content[0].Text);
            foreach (var line in completion.Content[0].Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var split = line.Split(": ", 2);
                var name = split[0].Trim();
                var value = split[1].Trim();
                if (value == "null") continue;
                foreach (var charac in Dict[name])
                {
                    if (charac.Item2 == value)
                    {
                        charac.Item1.Characteristics[name].IsBest = true;
                    }
                }
            }
        }

        private static List<Characteristic> FindBests(List<(Product, string)> list, bool comparisonDirection, string charactName)
        {
            var DoubleList = new List<(Characteristic, double)>();
            foreach (var characteristic in list)
            {
                double value = ParseDouble(characteristic.Item2);
                DoubleList.Add((characteristic.Item1.Characteristics[charactName], value));
            }
            var sortedList = DoubleList.OrderBy(item => item.Item2).ToList();

            if (!comparisonDirection)
            {
                //Console.WriteLine(sortedList[0].Item1.Name + " лучшая " + sortedList[0].Item2);
                return sortedList.FindAll(x => x.Item2 == sortedList[0].Item2).Select(x => x.Item1).ToList();
            }
            else
                //Console.WriteLine(sortedList[sortedList.Count - 1].Item1.Name + " лучшая " + sortedList[sortedList.Count - 1].Item2);
                return sortedList.FindAll(x => x.Item2 == sortedList[sortedList.Count - 1].Item2).Select(x => x.Item1).ToList();
        }

        private static double ParseDouble(string value)
        {
            var parts = value.Split(' ');
            if (parts.Length < 1) return 0;

            if (double.TryParse(parts[0].Replace('.', ','), out var dub))
            {

                return dub;
            }

            return 0;
        }
    }
}
