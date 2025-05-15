using System;
using System.Collections.Generic;
using System.DirectoryServices;

namespace ADSearchLibrary
{
  public class ADSearch
  {
    #region Поля и свойства

    private static ADSearch instance;

    /// <summary>
    /// Экземпляр объекта.
    /// </summary>
    /// <seealso>Паттерн Singleton.</seealso>
    public static ADSearch Instance
    {
      get { return instance ?? (instance = new ADSearch()); }
    }

    #endregion

    /// <summary> 
    /// Имя атрибута в AD, значение которого будет ключом для словаря с результатами поиска.
    /// По умолчанию - employeeid (табельный номер).
    /// </summary>
    public string NameOfDictionaryKey { get; set; } = "employeeid";

    /// <summary>
    /// Выполнить LDAP запрос к Active Directory.
    /// </summary>
    /// <param name="domainName">Имя домена.</param>
    /// <param name="queryStr">LDAP-фильтр.</param>
    /// <param name="propertiesToQuery">Выгружаемые атрибуты AD.</param>
    /// <returns>Словарь, где ключ - табельный номер, значение - словарь с именами и значениями атрибутов AD.</returns>
    /// <seealso cref="https://docs.microsoft.com/ru-ru/windows/desktop/ADSI/search-filter-syntax"/>
    public Dictionary<string, Dictionary<string, string>> ExecuteQueryToAD(string domainName, string queryStr, List<string> propertiesToQuery)
    {
      var result = new Dictionary<string, Dictionary<string, string>>();
      string ldapAddress = string.Format("LDAP://{0}", domainName);
      
      using (DirectoryEntry currentDomain = new DirectoryEntry(ldapAddress, null, null, AuthenticationTypes.None))
      {
        using (DirectorySearcher searcher = new DirectorySearcher(currentDomain))
        {
          #region Настойки поиска. 
          // <seealso cref="https://docs.microsoft.com/ru-ru/dotnet/api/system.directoryservices.directorysearcher"/>
          searcher.Asynchronous = true;                                     // Асинхронный поиск.
          searcher.CacheResults = false;                                    // Локальное хранение результатов поиска. Установить false чтобы не кэшировать результирующий набора на клиентский компьютер. Но в этом случае не получится дважды получить результаты (например Count + foreach).
          searcher.Filter = queryStr;                                       // LDAP-фильтр.
          searcher.PageSize = 999;                                          // Число возвращаемых объектов при поиске с постраничным выводом (если 0 - постраничный поиск не используется).
          searcher.SizeLimit = 999;                                         // Максимальное количество объектов, возвращаемых сервером при поиске. Если выполняется условие 0 < PageSize <= SizeLimit, то будут выгружены все записи соответствующие условию (за счет выполнения N-раз постраничных загрузок). Иначе будет возвращено SizeLimit результатов. Максимум 1000.
          searcher.ServerTimeLimit = TimeSpan.FromSeconds(30);              // Максимальное время, затрачиваемое сервером при поиске. По умолчанию - 120 секунд. 
          searcher.SearchScope = SearchScope.Subtree;                       // Поиск по всему дереву в AD.
          searcher.PropertiesToLoad.AddRange(propertiesToQuery.ToArray());  // Выгружаемые атрибуты AD.

          // Добавить к выгружаемым полям, поле которое является ключевым для результирующего словаря.
          if (!searcher.PropertiesToLoad.Contains(this.NameOfDictionaryKey))
            searcher.PropertiesToLoad.Add(this.NameOfDictionaryKey);
          #endregion

          // Выполнить поиск.
          SearchResultCollection resultCollection = searcher.FindAll();
          // Значение из переменной resultCollection можно прочитать только один раз (при условии что CacheResults = false), то есть после вызова метода .Count перестанет работать foreach.

          if (resultCollection != null)
          {
            foreach (SearchResult searchResult in resultCollection)
            {
              // Переменные "Ключ" и "Значение" для результирующего словаря.
              var tempKey = string.Empty;
              var tempDictionary = new Dictionary<string, string>();

              // Считать все атрибуты. Результат уже ограничен списком выгружаемых атрибутов.
              foreach (string propertyKey in searchResult.Properties.PropertyNames)
              {
                // Здесь propertyKey возвращается в нижнем регистре https://social.technet.microsoft.com/Forums/en-US/3402b48d-b840-4e93-bbf8-8cecda098573/userprincipalname-not-available-to-variable-when-using-directoryservicesdirectorysearcher?forum=winserverpowershell
                ResultPropertyValueCollection valueCollection = searchResult.Properties[propertyKey];
                foreach (object propertyValue in valueCollection)
                {
                  if (propertyKey == this.NameOfDictionaryKey)
                    tempKey = propertyValue.ToString();

                  string convertedValue = string.Empty;
                  /// Данные могут быть представлены как строкой, так и числом или массивом байтов.
                  /// Массив байт (например подпись) кодируется в строку base64.
                  /// Остальные типы (int32, int64, datetime...) преобразуются в строку.
                  var typeName = propertyValue.GetType().ToString().ToLower();
                  if (typeName.Contains("byte[]"))
                    convertedValue = Convert.ToBase64String((byte[])propertyValue);
                  else
                    convertedValue = propertyValue.ToString();
                  
                  // TODO при наличии нескольких значений: 1.выдавать исключение, 2.вместо строки использовать список для значений или 3.ввести уникальный разделитель.
                  if (tempDictionary.ContainsKey(propertyKey))
                    tempDictionary[propertyKey] = tempDictionary[propertyKey] + ", " + convertedValue;  // Атрибуты с одинаковыми именами объединяются в одну строку.
                  else
                    tempDictionary.Add(propertyKey, convertedValue);
                }
              }

              if (string.IsNullOrWhiteSpace(tempKey))
                throw new Exception(string.Format("Не удалось добавить новый элемент в словарь, так как передан пустой ключ \"{0}\".", tempKey));

              if (result.ContainsKey(tempKey))
                throw new Exception(string.Format("В словаре уже есть элемент с ключом \"{0}\". Вероятно выбранное ключевое поле \"{1}\" не является уникальным.", tempKey, NameOfDictionaryKey));

              result.Add(tempKey, tempDictionary);
            }
          }
        }
      }

      return result;
    }
  }
}
