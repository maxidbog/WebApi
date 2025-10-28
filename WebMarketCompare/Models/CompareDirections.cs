using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CharacteristicConfig
{
    public bool GlobalDirection { get; set; }
    public Dictionary<string, bool> CategoryOverrides { get; set; }

    public CharacteristicConfig(bool _GlobalDirection, Dictionary<string, bool> _CategoryOverrides)
    {
        GlobalDirection = _GlobalDirection;
        CategoryOverrides = _CategoryOverrides;
    }
    public CharacteristicConfig(bool _GlobalDirection)
    {
        GlobalDirection = _GlobalDirection;
        CategoryOverrides = null;
    }
}

public class CompareDirections
{
    public static Dictionary<string, CharacteristicConfig> characteristicsMap = new Dictionary<string, CharacteristicConfig>
    {
        // TRUE: чем больше, тем лучше
        ["Оперативная память"] = new CharacteristicConfig(true),
        ["Макс. объем карты памяти,  ГБ"] = new CharacteristicConfig(true),
        ["Встроенная память"] = new CharacteristicConfig(true),
        ["Частота обновления экрана, Гц"] = new CharacteristicConfig(true),
        ["Макс. частота обновления, Гц"] = new CharacteristicConfig(true),
        ["Разрешение экрана"] = new CharacteristicConfig(true),
        ["Диагональ экрана, дюймы"] = new CharacteristicConfig(true),
        ["Макс. скорость видеосъемки, кадр/с"] = new CharacteristicConfig(true),
        ["Емкость аккумулятора, мАч"] = new CharacteristicConfig(true),
        ["Время работы в режиме разговора, ч"] = new CharacteristicConfig(true),
        ["Работа в режиме ожидания, ч"] = new CharacteristicConfig(true),
        ["Число ядер процессора"] = new CharacteristicConfig(true),
        ["Частота процессора, ГГц"] = new CharacteristicConfig(true),
        ["Число физических SIM-карт"] = new CharacteristicConfig(true),
        ["Разрешение основной камеры, Мпикс"] = new CharacteristicConfig(true),
        ["Количество основных камер"] = new CharacteristicConfig(true),
        ["Разрешение фронтальной (селфи) камеры, Мпикс"] = new CharacteristicConfig(true),
        ["Модуль связи Bluetooth"] = new CharacteristicConfig(true),
        ["Гарантийный срок"] = new CharacteristicConfig(true),
        ["Срок службы, лет"] = new CharacteristicConfig(true),
        ["Версия Android"] = new CharacteristicConfig(true),
        ["Качество видео"] = new CharacteristicConfig(true),
        ["Степень защиты"] = new CharacteristicConfig(true),
        ["Число портов USB Type-C"] = new CharacteristicConfig(true),
        ["Число портов Thunderbolt"] = new CharacteristicConfig(true),
        ["Яркость, кд/м2"] = new CharacteristicConfig(true),
        ["Тип памяти"] = new CharacteristicConfig(true),
        ["Возможность расширения RAM, до"] = new CharacteristicConfig(true),
        ["Слоты RAM-памяти"] = new CharacteristicConfig(true),
        ["Количество SSD"] = new CharacteristicConfig(true),
        ["Разрешение Web-камеры"] = new CharacteristicConfig(true),
        ["Общий объем SSD, ГБ"] = new CharacteristicConfig(true),
        ["Видеопамять"] = new CharacteristicConfig(true),
        ["Число портов HDMI"] = new CharacteristicConfig(true),
        ["Число портов USB Type-A 3.2 Gen 1"] = new CharacteristicConfig(true),
        ["Число портов USB Type-A 3.2 Gen 2"] = new CharacteristicConfig(true),
        ["Кол-во элементов аккумулятора"] = new CharacteristicConfig(true),
        ["Гарантия OZON"] = new CharacteristicConfig(true),
        ["Емкость аккумулятора, Втч"] = new CharacteristicConfig(true),
        ["Время автономной работы, ч"] = new CharacteristicConfig(true),
        ["Версия Windows"] = new CharacteristicConfig(true),
        ["Сетевая карта"] = new CharacteristicConfig(true),
        ["Число портов USB 2.0"] = new CharacteristicConfig(true),
        ["Количество HDD"] = new CharacteristicConfig(true),


        ["Вес товара, г"] = new CharacteristicConfig(false, new Dictionary<string, bool>
        {
            ["Конфеты"] = true,
        })
    };


    public static bool TryGetComparisonDirection(string characteristicName, string categoryName, out bool comparisonDirection)
    {
        comparisonDirection = false;
        if (characteristicsMap.TryGetValue(characteristicName, out CharacteristicConfig value))
        {
            if (value.CategoryOverrides != null && value.CategoryOverrides.ContainsKey(categoryName))
                comparisonDirection = value.CategoryOverrides[categoryName];
            comparisonDirection = value.GlobalDirection;
            return true;
        }
        return false; // Не найдено
    }
}
