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
            model: "gpt-5.1",
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
            MarkBestCharacteristic(charactDict, products.Count);
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

        private static void MarkBestCharacteristic(Dictionary<string, List<(Product, string)>> CharDict, int productsCount)
        {
            var AiString = string.Empty;
            foreach (var characteristic in CharDict)
            {
                if (CharDict[characteristic.Key].Count == productsCount)
                {
                    AiString += $"{characteristic.Key}:{string.Join("###", characteristic.Value.Select(x => x.Item2))}\n";
                }
                //Console.WriteLine(characteristic.Key + " не в списке сравнения");
            }
            MarkBestAi(CharDict, AiString);
            //Console.WriteLine(AiString);

        }

        private static void MarkBestAi(Dictionary<string, List<(Product, string)>> Dict, string charString)
        {
            var headerString = $"Мы сравниваем: {Dict.ElementAt(0).Value[0].Item1.CategoryName}" + @"Проанализируй список характеристик  товаров, разделенных символом ""###"". Для каждой категории характеристик сравни значения, нормализуй единицы измерения только внутренне для сравнения (например, преобразуй см в м, но не изменяй выводимое значение), и выбери единственный вариант с НАИЛУЧШИМ объективным значением (чем выше числовое значение, тем лучше; для технологий - новее/качественнее).

**Правила форматирования ответа:**
- Каждая характеристика — с новой строки.
- Формат: `Название характеристики: Значение` (используй ОРИГИНАЛЬНОЕ НАЗВАНИЕ ХАРАКТЕРИСТИКИ и лучшее значение ТОЧНО В ЕГО ИСХОДНОМ ФОРМАТЕ ИЗ ВХОДНЫХ ДАННЫХ, БЕЗ ДОБАВЛЕНИЯ, УДАЛЕНИЯ ИЛИ ИЗМЕНЕНИЯ СИМВОЛОВ, ЕДИНИЦ ИЛИ ФОРМАТА).
- Не меняй или не добавляй строки, если категория не подходит под критерии.
- Не включай в ответ никакие другие комментарии, пояснения, символы или заголовки.

**Критерии отбора:**
1. Характеристика должна быть объективно сравнимой численно или по качеству (например, мощность в Вт, диаметр в мм, длина в м). Выбирай максимальное/лучшее значение только если есть различия после внутренней нормализации.
2. Если характеристику нельзя объективно сравнить для выявления ""лучшего"" (например, цвет, материал, страна производства, тип патрона без различий), НЕ ВКЛЮЧАЙ ее в ответ.
3. Если все значения в категории идентичны (или после внутренней нормализации становятся идентичными) и нет лучшего, НЕ ВКЛЮЧАЙ ее в ответ.
4. При нормализации: используй ее только для внутреннего сравнения значений (например, для длины шнура преобразуй 200 см в 2 м мысленно, но выводи значение точно как в input для выбранного лучшего товара, без изменений).
5. Если значение содержит единицу в input (например, ""Вт"", ""мм""), сохрани его как есть в выводе; если нет - не добавляй ничего. Не предполагай и не добавляй единицы или символы (например, не добавляй "" или м, если их нет в исходном значении лучшего варианта).
6. Если несколько товаров имеют одинаковое лучшее значение (после нормализации), выбери значение из любого из них, но выводи его точно как в input для этого товара.

**Пример анализа (не включай в ответ):**
Для ""Мощность (Вт):200###100 Вт###150"" - внутренне нормализуй (предположи единицы совместимы), лучшее 200, вывод: Мощность (Вт): 200 (точно как в input).
Для идентичных или несравнимых - пропусти.

**Входные данные для анализа:**";
            Console.WriteLine("Запрос нейросети...");
            ChatCompletion completion = client.CompleteChat(headerString + "\n" + charString);
            Console.WriteLine(completion.Content[0].Text);
            foreach (var line in completion.Content[0].Text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var split = line.Split(":", 2);
                var name = split[0].Trim();
                var value = split[1].Trim();
                if (value == "null") continue;
                foreach (var charac in Dict[name])
                {
                    if (charac.Item2 == value)
                    {
                        charac.Item1.Characteristics[name].IsBest = true;
                        Console.WriteLine(name);
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
            if (products == null || products.Count < 2) return;

            var standardizer = new StandardNaming();
            var nameDict = new Dictionary<string, int>();

            foreach (var product in products)
            {
                var newProductCharacteristics = new Dictionary<string, Characteristic>();
                foreach (var characteristic in product.Characteristics)
                {
                    var standardName = standardizer.GetStandardName(characteristic.Key);
                    if (standardName != null)
                    {
                        characteristic.Value.Name = standardName;
                        newProductCharacteristics.Add(standardName, characteristic.Value);
                        nameDict[standardName] = nameDict.ContainsKey(standardName) ? nameDict[standardName] + 1 : 1;
                    }
                    else
                    {
                        newProductCharacteristics.Add(characteristic.Key, characteristic.Value);
                        nameDict[characteristic.Key] = nameDict.ContainsKey(characteristic.Key) ? nameDict[characteristic.Key] + 1 : 1;
                    }
                }
                product.Characteristics = newProductCharacteristics;
            }


            if (!products.All(x => standardizer.IsInStandartSet(x.CategoryName)))
            {
                var characteristicList = new List<List<string>>();
                var aiString = @"You are an expert in data standardization for e-commerce product attributes. Your task is to analyze the provided list of product characteristics from multiple items (all under the ""Smartphones"" category) and identify pairs of attribute names that mean exactly the same thing (identical concepts or exact synonyms, not vaguely similar, partially overlapping, or where one is a subset/superset of the other). For each such pair, suggest standardizing by replacing one with the other.
The input data is a list of products, each starting with a category line (e.g., ""1 Смартфоны""), followed by lines in the format: ""{Attribute Name} Значение: {Attribute Value}"".
Rules:

Only pair attributes that are semantically identical in meaning, representing precisely the same single concept (e.g., exact equivalents like ""Brand"" and ""Бренд"" if they refer to the precise same thing). Do not pair if they are related but distinct (e.g., do not pair authentication methods with built-in sensors or additional options, even if they overlap in values like fingerprint or face recognition, as they may include different elements; do not pair a combined dimensions attribute like ""Размеры, мм"" with individual dimension attributes like height, width, or thickness, or vice versa, as one is aggregate and the others are components, not identical concepts).
Focus solely on attribute names; ignore values unless they clearly demonstrate the names are not identical (but do not use values to force pairings).
Do not invent or assume attribute names that do not exist in the provided data.
Do not pair broader categories (e.g., ""additional options"") with specific features (e.g., sensors or authentication), even if they share some aspects.
Each original attribute to replace must appear only once in your output (no duplicates).
If no such pairs are found, output nothing.
Output strictly in the format: {Attribute to replace}###{Standard attribute to replace it with}
One pair per line.
Do not include any explanations, additional text, headers, or other content—only the pairs if applicable.
Dataset:" + "\n";
                foreach (var product in products)
                {
                    var charList = new List<string>();
                    foreach (var characteristic in product.Characteristics)
                    {
                        if (!(nameDict.ContainsKey(characteristic.Key) && nameDict[characteristic.Key] == products.Count))
                        {
                            charList.Add(characteristic.Key + " Значение: " + characteristic.Value.Value);
                        }
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

                aiString += "Задача: сгруппируй только те характеристики, которые семантически эквивалентны (означают одно и то же, несмотря на разные названия) и без значений. Например, \"Встроенная память\" (Ozon) и \"Объем встроенной памяти (Гб)\" (Wildberries) — это одна группа, потому что они про одно и то же.\r\nПравила:\r\n\r\nКаждая группа должна содержать РОВНО ПО ОДНОЙ характеристике из Ozon и одной из Wildberries. Не больше, не меньше.\r\nОдна характеристика может быть ТОЛЬКО В ОДНОЙ группе — без дубликатов.\r\nГруппируй только если сопоставление логично и прямое. Не группируй несопоставимые вещи, например, общий \"Размеры, мм\" не группируй с отдельными \"Высота предмета\", \"Ширина предмета\", \"Толщина предмета\", потому что это разные уровни детализации.\r\nЕсли нет уверенного сопоставления для характеристики, не включай её ни в какую группу.\r\nВыводи только группы, где есть сопоставление. Не пиши одиночные характеристики или пустые группы.\r\n\r\nФормат вывода: Каждая группа на новой строке, характеристики в группе разделены \"###\". Без лишнего текста, объяснений или заголовков. Только список групп.\r\nПример правильного вывода на основе примера:\r\nВстроенная память###Объем встроенной памяти (Гб)\r\nСначала подумай шаг за шагом: перечисли все возможные пары, проверь на эквивалентность, отфильтруй несопоставимые.\r\nТеперь выполни задачу.";

                Console.WriteLine("Запрос нейросети...");
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
                        try
                        {
                            if (renameDict.ContainsKey(characteristic.Key))
                            {
                                newCharacteristics[characteristic.Key].Name = renameDict[characteristic.Key];
                                var newValue = newCharacteristics[characteristic.Key];
                                newCharacteristics.Remove(characteristic.Key);
                                newCharacteristics.Add(renameDict[characteristic.Key], newValue);
                            }
                        }
                        catch { };
                    }
                    product.Characteristics = newCharacteristics;
                }
            }
        }

        static string RemoveThinkSection(string input)
        {
            int thinkEnd = input.IndexOf("</think>");
            if (thinkEnd != -1)
            {
                return input.Substring(thinkEnd + 8).TrimStart();
            }
            return input;
        }
    }
}
