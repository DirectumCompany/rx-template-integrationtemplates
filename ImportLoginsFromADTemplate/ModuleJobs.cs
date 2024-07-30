using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ImportAD.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Закрытие учетных записей.
    /// </summary>
    public virtual void CloseLogins()
    {
      Logger.Debug("CloseLogins. Фоновый процесс запущен.");

      var searchResult = Functions.Module.ExecuteQueryToAD("ADHost", "ADDomain", "ADUserName", "ADUserPas", "ADPort", "ADUseSsl", "ADDelOU");
      
      var empNumberAttributeList = searchResult.Select(x => x.Value["employeenumber"]).ToList();
      var loginsFromAD = searchResult.Where(x => empNumberAttributeList.Contains(x.Value["employeenumber"])).ToList();
      Logger.DebugFormat("CloseLogins. Найдено {0} учетных записей с атрибутом employeenumber.", loginsFromAD.Count);

      var count = 0;
      var takeCount = 1000;
      var errors = new System.Text.StringBuilder();
      while (count < empNumberAttributeList.Count)
      {
        var empNumberAttributeBatchList = empNumberAttributeList.Skip(count).Take(takeCount).ToList();
        var employees = Sungero.Company.Employees.GetAll()
          .Where(x => !string.IsNullOrWhiteSpace(x.PersonnelNumber))
          .Where(x => empNumberAttributeBatchList.Contains(x.PersonnelNumber))
          .ToList();

        count = count + empNumberAttributeBatchList.Count;
        Logger.DebugFormat("CloseLogins. {0}/{1}. Найдено {2} сотрудников.", count, empNumberAttributeList.Count, employees.Count);

        foreach (var employee in employees.OrderByDescending(x => x.Id))
        {
          var error = string.Empty;
          var personnelNumber = employee.PersonnelNumber;
          
          var logins = loginsFromAD.FindAll(x => x.Value["employeenumber"] == personnelNumber);
          if (logins.Count == 0)
            error = string.Format("Учетная запись пользователя с employeenumber = {0} не найдена.", personnelNumber);
          if (logins.Count > 1)
            error = string.Format("Найдено несколько учетных записей с employeenumber = {0}.", personnelNumber);
          
          if (string.IsNullOrEmpty(error))
          {
            var samAccountName = string.Empty;
            try
            {
              var login = logins.Single().Value;
              samAccountName = login["samaccountname"];
              Functions.Module.CloseLogin(samAccountName);
            }
            catch(Exception ex)
            {
              Logger.ErrorFormat("CloseLogins. {0}. ", ex, samAccountName);
              error = string.Format("При обновлении учетной записи сотрудника {0} возникла ошибка: {1}", Hyperlinks.Get(employee), ex.Message);
            }
          }
          
          if (!string.IsNullOrEmpty(error))
            errors.AppendLine(error);
        }
      }
      
      if (errors.Length > 0)
        throw AppliedCodeException.Create(errors.ToString());
    }
    

    /// <summary>
    /// Синхронизация учетных записей из Active Directory.
    /// </summary>
    public virtual void UpdateOrCreateLogins()
    {
      Logger.Debug("UpdateOrCreateLogins. Фоновый процесс запущен.");
      
      var searchResult = Functions.Module.ExecuteQueryToAD("ADHost", "ADDomain", "ADUserName", "ADUserPas", "ADPort", "ADUseSsl", "ADSCon");
      
      var empNumberAttributeList = searchResult.Select(x => x.Value["employeenumber"]).ToList();
      var loginsFromAD = searchResult.Where(x => empNumberAttributeList.Contains(x.Value["employeenumber"])).ToList();
      Logger.DebugFormat("UpdateOrCreateLogins. Найдено {0} учетных записей с атрибутом employeenumber.", loginsFromAD.Count);

      var count = 0;
      var takeCount = 1000;
      var errors = new System.Text.StringBuilder();
      while (count < empNumberAttributeList.Count)
      {
        var empNumberAttributeBatchList = empNumberAttributeList.Skip(count).Take(takeCount).ToList();
        var employees = Sungero.Company.Employees.GetAll()
          .Where(x => !string.IsNullOrWhiteSpace(x.PersonnelNumber))
          .Where(x => empNumberAttributeBatchList.Contains(x.PersonnelNumber))
          .ToList();

        count = count + empNumberAttributeBatchList.Count;
        Logger.DebugFormat("UpdateOrCreateLogins. {0}/{1}. Найдено {2} сотрудников.", count, empNumberAttributeList.Count, employees.Count);

        foreach (var employee in employees.OrderByDescending(x => x.Id))
        {
          var error = string.Empty;
          var personnelNumber = employee.PersonnelNumber;
          var logins = loginsFromAD.FindAll(x => x.Value["employeenumber"] == personnelNumber);
          if (logins.Count == 0)
            error = string.Format("Учетная запись пользователя с employeenumber = {0} не найдена.", personnelNumber);
          if (logins.Count > 1)
            error = string.Format("Найдено несколько учетных записей с employeenumber = {0}.", personnelNumber);
          
          if (string.IsNullOrEmpty(error))
          {
            var samAccountName = string.Empty;
            try
            {
              var login = logins.Single().Value;
              samAccountName = login["samaccountname"];
              Functions.Module.UpdateOrCreateLogin(samAccountName, employee);
            }
            catch(Exception ex)
            {
              Logger.ErrorFormat("UpdateOrCreateLogins. {0}. ", ex, samAccountName);
              error = string.Format("При обновлении учетной записи сотрудника {0} возникла ошибка: {1}", Hyperlinks.Get(employee), ex.Message);
            }
          }
          
          if (!string.IsNullOrEmpty(error))
            errors.AppendLine(error);
        }
      }
      
      if (errors.Length > 0)
        throw AppliedCodeException.Create(errors.ToString());
    }

  }
}