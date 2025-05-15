using System;
using System.Collections.Generic;
using ADSearchLibrary;

namespace testConsole
{
  class Program
  {
    static void Main(string[] args)
    {
      //  Условие фильтрации
      // <seealso cref="https://docs.microsoft.com/ru-ru/windows/desktop/ADSI/search-filter-syntax"/>
      string QueryStr;
      QueryStr = "((OU = OZR_Users))";
      QueryStr = "(&(objectclass=user)(objectCategory=person)(sAMAccountName=kozhevnikov_mp))";

      // Выбираемые поля
      var propertiesToQuery = new List<string>();
      propertiesToQuery.Add("samaccountname");
      //propertiesToQuery.Add("objectguid");
      //propertiesToQuery.Add("member");
      //propertiesToQuery.Add("memberof");
      //propertiesToQuery.Add("objectClass");

      var DomainName = "NT_WORK";

      // Объект для поиска в AD
      var adSearch = ADSearch.Instance;
      adSearch.NameOfDictionaryKey = "samaccountname";

      // Поиск и вывод результатов
      var res = adSearch.ExecuteQueryToAD(DomainName, QueryStr, propertiesToQuery);
      string ADLogin = null;
      if (res.Count > 0)
      {
        var e = res.GetEnumerator();
        e.MoveNext();
        var item = e.Current;
        ADLogin = item.Key;
        var iv = item.Value;
        var vr = iv["samaccountname"];

      }

      Console.WriteLine("\n============={0}==================", ADLogin);

      var i = 0;
      foreach (var item in res)
      {
        Console.WriteLine("\n---------------{0}-{1}-----------", ++i, item.Key);
        foreach (var prop in item.Value)
        {
          Console.WriteLine("{0}:{1}", prop.Key, prop.Value);
        }

      }
      Console.ReadKey();
      
    }
  }
}
