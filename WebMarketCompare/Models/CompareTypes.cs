using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class CompareTypes
{
    public static Dictionary<string, bool> characteristicsMap = new Dictionary<string, bool>
    {
        // TRUE: чем больше, тем лучше
        ["оперативная память;ram;озу;оперативка;Макс. объем карты памяти,  ГБ"] = true,
        ["память;memory;storage;встроенная память;накопитель"] = true,
        ["рейтинг;rating;оценка;звезды;stars"] = true,
        ["скидка;discount;дисконт;акция;распродажа"] = true,
        ["отзывы;reviews;количество отзывов;число отзывов"] = true,
        ["Частота обновления экрана, Гц;диагональ экрана, дюймы;диагональ;экран;screen;screen size;размер экрана;Макс. скорость видеосъемки, кадр/с"] = true,
        ["емкость аккумулятора, мач;батарея;аккумулятор;battery;емкость батареи;Время работы в режиме разговора, ч;Работа в режиме ожидания, ч"] = true,
        ["число ядер процессора;частота процессора, ггц"] = true,
        ["число физических sim-карт"] = true,
        ["разрешение основной камеры, мпикс;Количество основных камер;Разрешение фронтальной (селфи) камеры, Мпикс"] = true,
        ["Модуль связи Bluetooth"] = true,
        ["Гарантийный срок;Срок службы, лет;гарантия;warranty;срок гарантии"] = true,
        ["рейтинг продавца;seller rating;оценка продавца"] = true,
        ["хлопок;cotton;состав хлопок"] = true,
        ["шерсть;wool"] = true,

        // FALSE: чем меньше, тем лучше
        ["цена;price;стоимость;cost;розница;розничная цена"] = false,
        ["Вес товара, г;вес;weight;масса;product weight"] = false,
        ["толщина;thickness"] = false,
        ["шум;noise"] = false,
        ["потребление;потребляемая мощность"] = false,

        // NULL: не сравниваются
        //["артикул;article;код товара;vendor code"] = null,
        //["бренд;brand;марка;производитель"] = null,
        //["цвет;color;окрас;расцветка"] = null,
        //["размер;size;размер одежды"] = null,
        //["страна;country;производство"] = null,
        //["описание;description;product description"] = null
    };


    public static bool TryGetCharacteristicType(string characteristicName, out bool characteristicType)
    {
        characteristicType = false;
        var lowerName = characteristicName.ToLower().Trim();
        foreach (var elem in characteristicsMap)
        {
            var synonyms = elem.Key.ToLower().Split(';');
            if (synonyms.Contains(lowerName))
            {
                characteristicType = elem.Value;
                return true;
            }
        }

        return false; // Не найдено
    }

    //public static void Main(string[] args)
    //{
    //    Console.WriteLine(GetCharacteristicType("модуль связи Bluetooth"));
    //}
}
