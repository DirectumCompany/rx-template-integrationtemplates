using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Serialization.Server
{
  public class ModuleFunctions
  {
    
    #region Вспомогательные функции.
    
    /// <summary>
    /// Импорт из структуры.
    /// </summary>
    /// <param name="businessUnitStructures">Структура.</param>
    private static void ImportFromStructure(System.Collections.Generic.List<Structures.Module.IBussinessUnit> businessUnitStructures)
    {
      foreach (var businessUnitStruct in businessUnitStructures)
      {
        var businessUnit = Sungero.Company.BusinessUnits.GetAll(x => x.Id == businessUnitStruct.Id).FirstOrDefault();
        
        if (businessUnit == null)
          businessUnit = Sungero.Company.BusinessUnits.Create();
        
        businessUnit.Name = businessUnitStruct.Name;
        businessUnit.LegalName = businessUnitStruct.LegalName;
        businessUnit.TIN = businessUnitStruct.TIN;
        businessUnit.TRRC = businessUnitStruct.TRRC;
        businessUnit.Save();
        
        var headDepartmentsStructures = businessUnitStruct.Departments;
        foreach (var headDepartmentStruct in headDepartmentsStructures)
        {
          var headDepartment = Sungero.Company.Departments.GetAll(x => x.Id == headDepartmentStruct.Id).FirstOrDefault();
          if (headDepartment == null)
            headDepartment = Sungero.Company.Departments.Create();
          
          headDepartment.Name = headDepartmentStruct.Name;
          headDepartment.ShortName = headDepartmentStruct.ShortName;
          headDepartment.Code = headDepartmentStruct.Code;
          headDepartment.BusinessUnit = businessUnit;
          headDepartment.Save();
          
          var departmentsStructures = headDepartmentStruct.Departments;
          foreach (var departmentStruct in departmentsStructures)
          {
            var department = Sungero.Company.Departments.GetAll(x => x.Id == departmentStruct.Id).FirstOrDefault();
            if (department == null)
              department = Sungero.Company.Departments.Create();
            
            department.Name = departmentStruct.Name;
            department.ShortName = departmentStruct.ShortName;
            department.Code = departmentStruct.Code;
            department.BusinessUnit = businessUnit;
            department.HeadOffice = headDepartment;
            department.Save();
          }
        }
      }
    }
    
    /// <summary>
    /// Подготовка данных для сериализации.
    /// </summary>
    /// <returns>Список структур.</returns>
    private static System.Collections.Generic.List<Structures.Module.IBussinessUnit> PrepareStructureToSerialize()
    {
      var businessUnitStructures = new System.Collections.Generic.List<Structures.Module.IBussinessUnit>();
      
      var businessUnits = Sungero.Company.BusinessUnits.GetAll();
      
      foreach (var businessUnit in businessUnits)
      {
        var businessUnitStruct = Structures.Module.BussinessUnit.Create();
        businessUnitStruct.Id = businessUnit.Id;
        businessUnitStruct.Name = businessUnit.Name;
        businessUnitStruct.LegalName = businessUnit.LegalName;
        businessUnitStruct.TIN = businessUnit.TIN;
        businessUnitStruct.TRRC = businessUnit.TRRC;
        
        var headDepartmentStructures = new System.Collections.Generic.List<Structures.Module.IDepartment>();
        var headDepartments = Sungero.Company.Departments.GetAll(x => Sungero.Company.BusinessUnits.Equals(x.BusinessUnit, businessUnit) && x.HeadOffice == null);
        
        foreach (var headDepartment in headDepartments)
        {
          var headDepartmentStruct = Structures.Module.Department.Create();
          headDepartmentStruct.Id = headDepartment.Id;
          headDepartmentStruct.Name = headDepartment.Name;
          headDepartmentStruct.ShortName = headDepartment.ShortName;
          headDepartmentStruct.Code = headDepartment.Code;
          headDepartmentStruct.BusinessUnitId = headDepartment.BusinessUnit.Id;
          
          var departmentStructures = new System.Collections.Generic.List<Structures.Module.IDepartment>();
          var departments = Sungero.Company.Departments.GetAll(x => Sungero.Company.Departments.Equals(x.HeadOffice, headDepartment));
          
          foreach (var department in departments)
          {
            var departmentStruct = Structures.Module.Department.Create();
            departmentStruct.Id = department.Id;
            departmentStruct.Name = department.Name;
            departmentStruct.ShortName = department.ShortName;
            departmentStruct.Code = department.Code;
            departmentStruct.BusinessUnitId = department.BusinessUnit.Id;
            
            departmentStructures.Add(departmentStruct);
          }
          
          headDepartmentStruct.Departments = departmentStructures;
          headDepartmentStructures.Add(headDepartmentStruct);
        }
        
        businessUnitStruct.Departments = headDepartmentStructures;
        businessUnitStructures.Add(businessUnitStruct);
      }
      
      return businessUnitStructures;
    }
    
    #endregion
    
    #region Работа с Json.
    
    /// <summary>
    /// Экспорт в Json.
    /// </summary>
    [Remote]
    public virtual void ExportToJson()
    {
      var businessUnitStructures = PrepareStructureToSerialize();
      var result = string.Empty;
      try
      {
        result = IsolatedFunctions.Serialization.SerializeToJson(businessUnitStructures);
        Logger.DebugFormat("ExportToJson. Экспорт в Json:\n{0}", result);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ExportToJson. Ошибка:{0} \nStackTrace:{1}", ex.Message, ex.StackTrace);
      }
      
    }
    
    /// <summary>
    /// Импорт из Json строки.
    /// </summary>
    /// <param name="json">Строка json.</param>
    [Remote]
    public virtual void ImportFromJson(string json)
    {
      try
      {
        var businessUnitStructures = IsolatedFunctions.Serialization.DeserializeFromJson(json);
        ImportFromStructure(businessUnitStructures);
        Logger.DebugFormat("ImportFromJson. Данные успешно сохранены.");
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ImportFromJson. json:{0}. Ошибка: {1}\nStackTrace:{2}", json, ex.Message, ex.StackTrace);
      }
    }
    
    #endregion
    
    #region Работа с XML.
    
    /// <summary>
    /// Экспорт в XML.
    /// </summary>
    [Remote]
    public virtual void ExportToXML()
    {
      var businessUnitStructures = PrepareStructureToSerialize();      
      var result = string.Empty;
      try
      {
        result = IsolatedFunctions.Serialization.SerializeToXML(businessUnitStructures);
        Logger.DebugFormat("ExportToXML. Экспорт в XML:\n{0}", result);
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ExportToXML. Ошибка:{0} \nStackTrace:{1}", ex.Message, ex.StackTrace);
      }
      
    }
    
    /// <summary>
    /// Импорт из XML строки.
    /// </summary>
    /// <param name="xml">Строка XML.</param>
    [Remote]
    public virtual void ImportFromXML(string xml)
    {
      try
      {
        var businessUnitStructures = IsolatedFunctions.Serialization.DeserializeFromXML(xml);
        ImportFromStructure(businessUnitStructures);
        Logger.DebugFormat("ImportFromXML. Данные успешно сохранены.");
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("ImportFromXML. xml:{0}. Ошибка: {1}\nStackTrace:{2}", xml, ex.Message, ex.StackTrace);
      }
    }
    
    #endregion

  }
}