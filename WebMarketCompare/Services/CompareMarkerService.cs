using WebMarketCompare.Models;

namespace WebMarketCompare.Services
{
    public class CompareMarkerService
    {
        public static async Task<List<Product>> MarkBestCharacteristicsAsync(List<Product> products)
        {
            var charactDict = GetCharacteristicsDict(products);
            MarkBestCharacteristic(charactDict);
            return products;

        }
        private static Dictionary<string, List<(Characteristic, string)>> GetCharacteristicsDict(List<Product> products)
        {
            var dictionary = new Dictionary<string, List<(Characteristic, string)>> { };
            foreach (var product in products)
            {
                foreach (var characteristic in product.Characteristics)
                {
                    foreach (var character in characteristic.Characteristics)
                    {
                        if (!dictionary.ContainsKey(character.Name))
                            dictionary.Add(character.Name, new List<(Characteristic, string)> { });
                        dictionary[character.Name].Add((character, character.Value));
                        character.IsBest = null;
                    }
                }
            }
            return dictionary;
        }

        private static void MarkBestCharacteristic(Dictionary<string, List<(Characteristic, string)>> CharDict)
        {
            foreach (var characteristic in CharDict)
            {
                if (CompareTypes.TryGetCharacteristicType(characteristic.Key, out var charType))
                {
                    var bests = FindBests(characteristic.Value, charType);
                    foreach (var charac in bests)
                        charac.IsBest = true;
                }
                //else
                //Console.WriteLine(characteristic.Key + " не в списке сравнения");
            }
        }

        private static List<Characteristic> FindBests(List<(Characteristic, string)> list, bool charType)
        {
            var DoubleList = new List<(Characteristic, double)>();
            foreach (var characteristic in list)
            {
                double value = ParseDouble(characteristic.Item2);
                DoubleList.Add((characteristic.Item1, value));
            }
            var sortedList = DoubleList.OrderBy(item => item.Item2).ToList();

            if (!charType)
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
