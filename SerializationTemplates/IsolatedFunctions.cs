using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Xml.Serialization;
using System.Xml;
using Sungero.Core;
using System.Text.RegularExpressions;

namespace DirRX.Serialization.Isolated.Serialization
{
  //HACK: Обход ограничения DDS. При сериализации/десериализации необходимо ссылаться на класс, а не его интерфейс.
  #region Классы для сериализации/десериализации.
  public class BusinessUnitInfo
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string LegalName { get; set; }
    public string TIN { get; set; }
    public string TRRC { get; set; }
    public System.Collections.Generic.List<DepartmentInfo> Departments { get; set; }

  }

  public class DepartmentInfo
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Code { get; set; }
    public long BusinessUnitId { get; set; }
    public System.Collections.Generic.List<DepartmentInfo> Departments { get; set; }
  }
  
  #endregion
  
  public class IsolatedFunctions
  {
    // Для сериализации/десериализации необходимо маппить прикладные структуры в соответствующие классы и наоборот.
    #region Мапперы.
    
    /// <summary>
    /// Маппинг вспомогательных структур в прикладные.
    /// </summary>
    /// <param name="isolatedModels">Список вспомогательных структур DepartmentInfo.</param>
    /// <returns>Список прикладных структур Department.</returns>
    public System.Collections.Generic.List<Structures.Module.IDepartment> MapToDepartments(System.Collections.Generic.List<DepartmentInfo> isolatedModels)
    {
      var departmentStructures = new System.Collections.Generic.List<Structures.Module.IDepartment>();
      
      if (isolatedModels == null)
        return departmentStructures;
      
      foreach (var model in isolatedModels)
      {
        var department = Structures.Module.Department.Create();
        department.Id = model.Id;
        department.Name = model.Name;
        department.ShortName = model.ShortName;
        department.Code = model.Code;
        department.BusinessUnitId = model.BusinessUnitId;
        department.Departments = MapToDepartments(model.Departments);
        departmentStructures.Add(department);
      }
      
      return departmentStructures;
    }
    
    /// <summary>
    /// Маппинг вспомогательных структур в прикладные.
    /// </summary>
    /// <param name="isolatedModels">Список вспомогательных структур BusinessUnitInfo.</param>
    /// <returns>Список прикладных структур Department.</returns>
    public System.Collections.Generic.List<Structures.Module.IBussinessUnit> MapToBusinessUnits(System.Collections.Generic.List<BusinessUnitInfo> isolatedModels)
    {
      var businessUnitStructures = new System.Collections.Generic.List<Structures.Module.IBussinessUnit>();
      
      if (isolatedModels == null)
        return businessUnitStructures;
      
      foreach (var model in isolatedModels)
      {
        var businessUnit = Structures.Module.BussinessUnit.Create();
        businessUnit.Id = model.Id;
        businessUnit.Name = model.Name;
        businessUnit.LegalName = model.LegalName;
        businessUnit.TIN = model.TIN;
        businessUnit.TRRC = model.TRRC;
        businessUnit.Departments = MapToDepartments(model.Departments);
        businessUnitStructures.Add(businessUnit);
      }
      
      return businessUnitStructures;
    }
    
    /// <summary>
    /// Маппинг прикладных структур в вспомогательные.
    /// </summary>
    /// <param name="departments">Список прикладных структур Department.</param>
    /// <returns>Список вспомогательных структур DepartmentInfo.</returns>
    public System.Collections.Generic.List<DepartmentInfo> MapToIsolatedDepartments(System.Collections.Generic.List<Structures.Module.IDepartment> departments)
    {
      var isolatedModels = new System.Collections.Generic.List<DepartmentInfo>();
      
      if (departments == null)
        return isolatedModels;
      
      foreach (var department in departments)
      {
        var model = new DepartmentInfo();
        model.Id = department.Id;
        model.Name = department.Name;
        model.ShortName = department.ShortName;
        model.Code = department.Code;
        model.BusinessUnitId = department.BusinessUnitId;
        model.Departments = MapToIsolatedDepartments(department.Departments);
        isolatedModels.Add(model);
      }
      
      return isolatedModels;
    }
    
    /// <summary>
    /// Маппинг прикладных структур в вспомогательные.
    /// </summary>
    /// <param name="departments">Список прикладных структур BussinessUnit.</param>
    /// <returns>Список вспомогательных структур BusinessUnitInfo.</returns>
    public System.Collections.Generic.List<BusinessUnitInfo> MapToIsolatedBusinessUnits(System.Collections.Generic.List<Structures.Module.IBussinessUnit> businessUnits)
    {
      var isolatedModels = new System.Collections.Generic.List<BusinessUnitInfo>();
      
      if (businessUnits == null)
        return isolatedModels;
      
      foreach (var businessUnit in businessUnits)
      {
        var model = new BusinessUnitInfo();
        model.Id = businessUnit.Id;
        model.Name = businessUnit.Name;
        model.LegalName = businessUnit.LegalName;
        model.TIN = businessUnit.TIN;
        model.TRRC = businessUnit.TRRC;
        model.Departments = MapToIsolatedDepartments(businessUnit.Departments);
        isolatedModels.Add(model);
      }
      
      return isolatedModels;
    }
    
    #endregion
    
    #region Валидация полей структуры.
    
    //Паттерн проверки наличия специальных символов.
    private static readonly Regex specialCharactersRegex = new Regex(@"[<>#%{}|\^~[]`]");
    //Паттерн проверки наличия в строке только целых чисел.
    private static readonly Regex digitsOnlyRegex = new Regex("^[0-9]+$");
    private const string DateFormat = "yyyy-MM-dd";
    
    /// <summary>
    /// Пример валидации даты.
    /// </summary>
    /// <param name="date">Дата.</param>
    public void ValidateDate(DateTime date)
    {
      if (date > DateTime.Now)
        throw new ArgumentException("date не может быть в будущем.");
    }
    
    /// <summary>
    /// Валидация полей структуры подразделения.
    /// </summary>
    /// <param name="departments">Структура DepartmentInfo.</param>
    public void ValidateDepartments(System.Collections.Generic.List<DepartmentInfo> departments)
    {
      foreach (var department in departments)
      {
        if (department.Id < 0)
          throw new ArgumentException("Id не может быть отрицательным.");
        if (department.BusinessUnitId < 0)
          throw new ArgumentException("BusinessUnitId не может быть отрицательным.");
        if (string.IsNullOrWhiteSpace(department.Name))
          throw new ArgumentException("Name не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(department.Name))
          throw new ArgumentException("Name содержит недопустимые специальные символы.");
        if (string.IsNullOrWhiteSpace(department.ShortName))
          throw new ArgumentException("ShortName не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(department.ShortName))
          throw new ArgumentException("ShortName содержит недопустимые специальные символы.");
        
        if (department.Departments != null)
          ValidateDepartments(department.Departments);
      }
    }
    
    /// <summary>
    /// Валидация полей структуры Наших организаций.
    /// </summary>
    /// <param name="businessUnits">Структура BusinessUnitInfo.</param>
    public void ValidateBusinessUnits(System.Collections.Generic.List<BusinessUnitInfo> businessUnits)
    {
      foreach (var businessUnit in businessUnits)
      {
        if (businessUnit.Id < 0)
          throw new ArgumentException("Id не может быть отрицательным.");
        if (string.IsNullOrWhiteSpace(businessUnit.Name))
          throw new ArgumentException("Name не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(businessUnit.Name))
          throw new ArgumentException("Name содержит недопустимые специальные символы.");
        if (string.IsNullOrWhiteSpace(businessUnit.LegalName))
          throw new ArgumentException("LegalName не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(businessUnit.LegalName))
          throw new ArgumentException("LegalName содержит недопустимые специальные символы.");
        if (string.IsNullOrWhiteSpace(businessUnit.TIN))
          throw new ArgumentException("TIN не может быть пустым или содержать только пробелы.");
        if (digitsOnlyRegex.IsMatch(businessUnit.TIN))
          throw new ArgumentException("TIN содержит недопустимые специальные символы.");
        if (string.IsNullOrWhiteSpace(businessUnit.TRRC))
          throw new ArgumentException("TRRC не может быть пустым или содержать только пробелы.");
        if (digitsOnlyRegex.IsMatch(businessUnit.TRRC))
          throw new ArgumentException("TRRC содержит недопустимые специальные символы.");
        
        if (businessUnit.Departments != null)
          ValidateDepartments(businessUnit.Departments);
      }
    }
    
    /// <summary>
    /// Валидация полей структуры Подразделения.
    /// </summary>
    /// <param name="departments">Структура Department.</param>
    public void ValidateDepartments(System.Collections.Generic.List<Structures.Module.IDepartment> departments)
    {
      foreach (var department in departments)
      {
        if (department.Id < 0)
          throw new ArgumentException("Id не может быть отрицательным.");
        if (department.BusinessUnitId < 0)
          throw new ArgumentException("BusinessUnitId не может быть отрицательным.");
        if (string.IsNullOrWhiteSpace(department.Name))
          throw new ArgumentException("Name не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(department.Name))
          throw new ArgumentException("Name содержит недопустимые специальные символы.");
        if (string.IsNullOrWhiteSpace(department.ShortName))
          throw new ArgumentException("ShortName не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(department.ShortName))
          throw new ArgumentException("ShortName содержит недопустимые специальные символы.");
        
        if (department.Departments != null)
          ValidateDepartments(department.Departments);
      }
    }
    
    /// <summary>
    /// Валидация полей структуры Наших организаций.
    /// </summary>
    /// <param name="businessUnits">Структура BussinessUnit.</param>
    public void ValidateBusinessUnits(System.Collections.Generic.List<Structures.Module.IBussinessUnit> businessUnits)
    {
      foreach (var businessUnit in businessUnits)
      {
        if (businessUnit.Id < 0)
          throw new ArgumentException("Id не может быть отрицательным.");
        if (string.IsNullOrWhiteSpace(businessUnit.Name))
          throw new ArgumentException("Name не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(businessUnit.Name))
          throw new ArgumentException("Name содержит недопустимые специальные символы.");
        if (string.IsNullOrWhiteSpace(businessUnit.LegalName))
          throw new ArgumentException("LegalName не может быть пустым или содержать только пробелы.");
        if (specialCharactersRegex.IsMatch(businessUnit.LegalName))
          throw new ArgumentException("LegalName содержит недопустимые специальные символы.");
        if (string.IsNullOrWhiteSpace(businessUnit.TIN))
          throw new ArgumentException("TIN не может быть пустым или содержать только пробелы.");
        if (!digitsOnlyRegex.IsMatch(businessUnit.TIN))
          throw new ArgumentException("TIN содержит недопустимые символы.");
        if (string.IsNullOrWhiteSpace(businessUnit.TRRC))
          throw new ArgumentException("TRRC не может быть пустым или содержать только пробелы.");
        if (!digitsOnlyRegex.IsMatch(businessUnit.TRRC))
          throw new ArgumentException("TRRC содержит недопустимые символы.");
        
        if (businessUnit.Departments != null)
          ValidateDepartments(businessUnit.Departments);
      }
    }
    
    #endregion

    /// <summary>
    /// Десериализация json-строки.
    /// </summary>
    /// <param name="json">Json-строка.</param>
    /// <returns>Список структур.</returns>
    [Public]
    public System.Collections.Generic.List<Structures.Module.IBussinessUnit> DeserializeFromJson(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
        throw new ArgumentException("DeserializeFromJson. Json строка не может быть пустой или содержать только пробелы.");
      
      try
      {
        JToken.Parse(json);
      }
      catch(JsonReaderException)
      {
        throw new ArgumentException("DeserializeFromJson. Некорректный JSON формат.");
      }
      
      var result = JsonConvert.DeserializeObject<System.Collections.Generic.List<BusinessUnitInfo>>(json);
      
      if (result == null)
        throw new ArgumentException("DeserializeFromJson. Не удалось десериализовать json строку.");
      
      ValidateBusinessUnits(result);
      
      return MapToBusinessUnits(result);
    }

    /// <summary>
    /// Сериализация структуры в json строку.
    /// </summary>
    /// <param name="businessUnits">Структура.</param>
    /// <returns>Json-строка.</returns>
    [Public]
    public string SerializeToJson(System.Collections.Generic.List<Structures.Module.IBussinessUnit> businessUnits)
    {
      ValidateBusinessUnits(businessUnits);
      
      JsonSerializerSettings settings = new JsonSerializerSettings
      {
        Formatting = Newtonsoft.Json.Formatting.Indented,
        ReferenceLoopHandling = ReferenceLoopHandling.Error // Проверка рекурсивных ссылок.
          // DateFormatString = DateFormat - Формат даты.
      };
      
      return JsonConvert.SerializeObject(businessUnits);
    }
    
    /// <summary>
    /// Десериализация XML-строки.
    /// </summary>
    /// <param name="xmlString">XML-строка.</param>
    /// <returns>Список структур.</returns>
    [Public]
    public System.Collections.Generic.List<Structures.Module.IBussinessUnit> DeserializeFromXML(string xmlString)
    {
      if (string.IsNullOrWhiteSpace(xmlString))
      {
        throw new ArgumentException("DeserializeFromXML. XML строка не может быть пустой или содержать только пробелы.");
      }
      
      try
      {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xmlString);
      }
      catch (XmlException)
      {
        throw new ArgumentException("DeserializeFromXML. Некорректный XML формат.");
      }
      
      XmlSerializer xmlSerializer = new XmlSerializer(typeof(System.Collections.Generic.List<DirRX.Serialization.Isolated.Serialization.BusinessUnitInfo>));
      using (StringReader stringReader = new StringReader(xmlString))
      {
        using (XmlReader reader = XmlReader.Create(stringReader))
        {
          var result = xmlSerializer.Deserialize(reader) as System.Collections.Generic.List<DirRX.Serialization.Isolated.Serialization.BusinessUnitInfo>;
          if (result == null)
            throw new ArgumentException("DeserializeFromXML. Не удалось десериализовать xml строку.");
          
          ValidateBusinessUnits(result);
          
          return MapToBusinessUnits(result);
        }
      }
    }
    
    /// <summary>
    /// Сериализация структуры в XML строку.
    /// </summary>
    /// <param name="businessUnits">Структура.</param>
    /// <returns>XML-строка.</returns>
    [Public]
    public string SerializeToXML(System.Collections.Generic.List<Structures.Module.IBussinessUnit> businessUnits)
    {
      ValidateBusinessUnits(businessUnits);
      var models = MapToIsolatedBusinessUnits(businessUnits);
      XmlSerializer xmlSerializer = new XmlSerializer(typeof(System.Collections.Generic.List<DirRX.Serialization.Isolated.Serialization.BusinessUnitInfo>));
      using (StringWriter stringWriter = new StringWriter())
      {
        using (XmlWriter writer = XmlWriter.Create(stringWriter))
        {
          xmlSerializer.Serialize(writer, models);
          return stringWriter.ToString();
        }
      }
    }

  }
}