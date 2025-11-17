using WebMarketCompare.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.RegularExpressions;

namespace WebMarketCompare.Services
{
    public class CompareMarkerService
    {
        static ChatClient client = new ChatClient(
            model: "deepseek-v3.1",
            credential: new ApiKeyCredential("sk-voidai-xFcSkgOmQA5ToUTKetA7xKWcScWDC_XZ6XFVWUqxgYCluJdv6vtl-5ebBs2BxInEEzcL4PqI9M5mtmKDBEDeuBUSOps1qYOd4iZ3hUIPZVCPIM9T9ptHHqgGiYAeN1ylZuirwA"),
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri("https://api.voidai.app/v1/")
            }
            );

        public static async Task<List<Product>> MarkBestCharacteristicsAsync(List<Product> products)
        {
            StandardProducts(products);
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
            var AiString = string.Empty;
            foreach (var characteristic in CharDict)
            {
                if (CompareDirections.TryGetComparisonDirection(characteristic.Key, characteristic.Value[0].Item1.CategoryName, out var charType))
                {
                    var bests = FindBests(characteristic.Value, charType, characteristic.Key);
                    foreach (var charac in bests)
                    {
                        Console.WriteLine( charac.Name + charac.Value);
                        charac.IsBest = true;
                    }
                }
                else if (true)
                {
                    //var bests = FindBestsAI(characteristic.Value, charType, characteristic.Key);
                    //foreach (var charac in bests)
                    //    charac.IsBest = true;
                    AiString += $"{characteristic.Key} : {string.Join(";#;", characteristic.Value.Select(x => '"' + x.Item2 + '"'))}\n";
                }
                //Console.WriteLine(characteristic.Key + " не в списке сравнения");
            }
            try
            {
                MarkBestAi(CharDict, AiString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при работе нейросети: {ex.Message}");
            }
            //Console.WriteLine(AiString);

        }

        private static void MarkBestAi(Dictionary<string, List<(Product, string)>> Dict, string charString)
        {
            var headerString = @"Проанализируй список характеристик товаров. Сравни их и для каждой категории характеристик выбери единственный вариант с НАИЛУЧШИМ значением.

**Правила форматирования ответа:**
- Каждая характеристика — с новой строки.
- Ввод: Название характеристики: ""Значение1"";#;""Значение2""....;#;""Значениеn"".
- Формат: `Название характеристики: Значение`, например: Процессор: Snapdragon.
- Не меняй строки.
- Не включай в ответ никакие другие комментарии, пояснения или символы.

**Критерии отбора:**
1.  Характеристика должна быть объективно сравнимой (например, тактовая частота процессора, тип матрицы экрана, объем памяти). Чем выше число или новее/качественнее технология, тем лучше.
2.  Если характеристику нельзя объективно сравнить для выявления ""лучшего"" (например, цвет, материал корпуса, артикул, название операционной системы), НЕ ВКЛЮЧАЙ ее в ответ.
3.  Если все значения в категории идентичны и не имеют лучшего (например, все ""Смартфон"" или ""Нет""), НЕ ВКЛЮЧАЙ ее в ответ.
4.  Характеристики отделены ';#;'

**Входные данные для анализа:**";
            ChatCompletion completion = client.CompleteChat(headerString + "\n" + charString);
            foreach (var content in completion.Content)
                Console.WriteLine(content.Text + "\n");
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
                        Console.WriteLine(name);
                        charac.Item1.Characteristics[name].IsBest = true;
                    }
                }
            }
        }

        private static List<Characteristic> FindBests(List<(Product, string)> list, bool comparisonDirection, string charactName)
        {
            var DoubleList = new List<(Characteristic, double)>();
            foreach (var product in list)
            {
                var characteristic = product.Item1.Characteristics[charactName];
                double value = ParseDouble(characteristic.Value);
                DoubleList.Add((characteristic, value));
            }
            var sortedList = DoubleList.OrderBy(item => item.Item2).ToList();
            var result = new List<Characteristic>();
            if (!comparisonDirection)
            {
                //Console.WriteLine(sortedList[0].Item1.Name + " лучшая " + sortedList[0].Item2);
                result = sortedList.FindAll(x => x.Item2 == sortedList[0].Item2).Select(x => x.Item1).ToList();
            }
            else
                //Console.WriteLine(sortedList[sortedList.Count - 1].Item1.Name + " лучшая " + sortedList[sortedList.Count - 1].Item2);
                result = sortedList.FindAll(x => x.Item2 == sortedList[sortedList.Count - 1].Item2).Select(x => x.Item1).ToList();
            return result;
        }

        private static double ParseDouble(string value)
        {
            var digits = Regex.Replace(value, @"[^\d.,]", "");

            if (double.TryParse(digits.Replace('.', ','), out var dub))
            {

                return dub;
            }

            return 0;
        }

        private static void StandardProducts(List<Product> products)
        {
            var characteristicList = new List<List<string>>();
            var aiString = @"Ты анализируешь характеристики товаров с двух платформ: Ozon и Wildberries. У тебя есть списки характеристик и их значений — один для Ozon, другой для Wildberries. Вот они:" + "\n";
            foreach (var product in products)
            {
                var charList = new List<string>();
                foreach (var characteristic in product.Characteristics)
                {
                    charList.Add(characteristic.Key + " Значение: " + characteristic.Value.Value);
                }
                characteristicList.Add(charList);
            }
            for (int i = 1; i < characteristicList.Count + 1; i++)
            {
                aiString += $"{i} {products[i - 1].CategoryName} \n";
                foreach (var characteristic in characteristicList[i - 1])
                {
                    aiString += $"{characteristic}\n";
                }
                aiString += "\n";
            }

            aiString += "Задача: сгруппируй только те характеристики, которые семантически эквивалентны (означают одно и то же, несмотря на разные названия) и без значений. Например, \"Встроенная память\" (Ozon) и \"Объем встроенной памяти (Гб)\" (Wildberries) — это одна группа, потому что они про одно и то же.\r\nПравила:\r\n\r\nКаждая группа должна содержать ровно по одной характеристике из Ozon и одной из Wildberries. Не больше, не меньше.\r\nОдна характеристика может быть только в одной группе — без дубликатов.\r\nГруппируй только если сопоставление логично и прямое. Не группируй несопоставимые вещи, например, общий \"Размеры, мм\" не группируй с отдельными \"Высота предмета\", \"Ширина предмета\", \"Толщина предмета\", потому что это разные уровни детализации.\r\nЕсли нет уверенного сопоставления для характеристики, не включай её ни в какую группу.\r\nВыводи только группы, где есть сопоставление. Не пиши одиночные характеристики или пустые группы.\r\n\r\nФормат вывода: Каждая группа на новой строке, характеристики в группе разделены \"###\". Без лишнего текста, объяснений или заголовков. Только список групп.\r\nПример правильного вывода на основе примера:\r\nВстроенная память###Объем встроенной памяти (Гб)\r\nСначала подумай шаг за шагом: перечисли все возможные пары, проверь на эквивалентность, отфильтруй несопоставимые.\r\nТеперь выполни задачу.";

            ChatCompletion completion = client.CompleteChat(aiString);
            var response = completion.Content[0].Text;
            Console.WriteLine(response);

            var renameDict = new Dictionary<string, string>();
            foreach (var line in completion.Content[0].Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split("###");
                foreach (var part in parts.Skip(1))
                {
                    renameDict[part.Trim()] = parts[0];
                }
            }

            foreach (var product in products.Skip(1))
            {
                var newCharacteristics = new Dictionary<string, Characteristic>(product.Characteristics);
                foreach (var characteristic in product.Characteristics)
                {
                    if (renameDict.ContainsKey(characteristic.Key))
                    {
                        newCharacteristics[characteristic.Key].Name = renameDict[characteristic.Key];
                        var newValue = newCharacteristics[characteristic.Key];
                        newCharacteristics.Remove(characteristic.Key);
                        newCharacteristics.Add(renameDict[characteristic.Key], newValue);
                    }
                }
                product.Characteristics = newCharacteristics;
            }

        }
    }
}
