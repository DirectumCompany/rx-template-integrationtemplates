using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.ImportAD.Server
{
  public class ModuleFunctions
  {
    
    /// <summary>
    /// Получить значение параметра из Sungero_Docflow_Params.
    /// </summary>
    /// <param name="paramName">Наименование параметра.</param>
    /// <returns>Значение параметра. Тип: bool.</returns>
    [Public, Remote(IsPure = true)]
    public virtual bool? GetDocflowParamsBoolValue(string paramName)
    {
      var paramValue = Sungero.Docflow.PublicFunctions.Module.GetDocflowParamsValue(paramName);
      if (!(paramValue is DBNull) || paramValue == null || !(paramValue is bool))
        return null;
      
      return Convert.ToBoolean(paramValue);
    }
    
    #region Работа с Active Directory.
    
    /// <summary>
    /// Валидация параметров подключения к Active Directory.
    /// </summary>
    /// <param name="adHost">Хост.</param>
    /// <param name="adDomain">Домен.</param>
    /// <param name="adUserName">Имя пользователя.</param>
    /// <param name="adUserPass">Пароль.</param>
    /// <param name="adPort">Порт.</param>
    /// <param name="adUseSsl">Признак использования SSL.</param>
    /// <param name="searchContainer">Область поиска.</param>
    /// <returns>Текст ошибки.</returns>
    public virtual string ValidateADConnectionParams(string adHost, string adDomain, string adUserName, string adUserPass,
                                                     int adPort, bool? adUseSsl, string searchContainer)
    {
      var settingValidation = string.Empty;
      
      if (string.IsNullOrWhiteSpace(adHost))
        settingValidation += "Не указана настройка хоста AD. ";
      if (string.IsNullOrWhiteSpace(adDomain))
        settingValidation += "Не указана настройка домена. ";
      if (string.IsNullOrWhiteSpace(adUserName))
        settingValidation += "Не указана настройка учетной записи. ";
      if (string.IsNullOrWhiteSpace(adUserPass))
        settingValidation += "Не указана настройка пароля учетной записи. ";
      if (adPort == null)
        settingValidation += "Не указана настройка порта хоста AD. ";
      if (!adUseSsl.HasValue)
        settingValidation += "Не указана настройка признака использования SSL. ";
      if (string.IsNullOrWhiteSpace(searchContainer))
        settingValidation += "Не указана область поиска. ";
      
      return settingValidation;
    }
    
    /// <summary>
    /// Выполнить запрос к Active Directory.
    /// </summary>
    /// <param name="adHostKey">Ключ параметра Хост.</param>
    /// <param name="adDomainKey">Ключ параметра Домен.</param>
    /// <param name="adUserNameKey">Ключ параметра Имя пользователя.</param>
    /// <param name="adUserPassKey">Ключ параметра Пароль.</param>
    /// <param name="adPortKey">Ключ параметра Порт.</param>
    /// <param name="adUseSslKey">Ключ параметра Признак использования SSL.</param>
    /// <param name="searchContainerKey">Ключ параметра Область поиска.</param>
    /// <returns>Результат запроса.</returns>
    public virtual System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> ExecuteQueryToAD(string adHostKey, string adDomainKey,
                                                                                                                                         string adUserNameKey, string adUserPassKey,
                                                                                                                                         string adPortKey, string adUseSslKey, string searchContainerKey)
    {
      var settingValidation = string.Empty;
      
      //Для примера настройки подключения берутся из таблицы DocflowParams. При копировании вы можете брать настройки из других удобных вам источников.
      var adHost = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue(adHostKey);
      var adDomain = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue(adDomainKey);
      var adUserName = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue(adUserNameKey);
      var adUserPass = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue(adUserPassKey);
      var adPort = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsIntegerValue(adPortKey);
      var adUseSsl = PublicFunctions.Module.Remote.GetDocflowParamsBoolValue(adUseSslKey);
      var searchContainer = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue(searchContainerKey);
      
      settingValidation = Functions.Module.ValidateADConnectionParams(adHost, adDomain, adUserName, adUserPass, adPort, adUseSsl, searchContainer);
      
      if (!string.IsNullOrWhiteSpace(settingValidation.Trim()))
      {
        Logger.ErrorFormat("ExecuteQueryToAD. Ошибка: {0}", settingValidation);
        return null;
      }
      
      // Получить всех пользователей с заполненным атрибутом "employeenumber" в AD.
      var query = "(&(objectCategory=user)(employeenumber=*))";

      // Выбираемые поля
      var propertiesToQuery = new List<string>();
      propertiesToQuery.Add("samaccountname");
      propertiesToQuery.Add("employeenumber");

      // Объекты для поиска в AD
      var searchAD = ADSearch.Search.Instance;
      searchAD.NameOfDictionaryKey = "samaccountname";
      
      try
      {
        var domainUserName = adDomain + @"\" + adUserName;
        return searchAD.ExecuteQueryToAD(adHost, domainUserName, adUserPass, adPort, adUseSsl.Value, searchContainer, query, propertiesToQuery);
      }
      catch(Exception ex)
      {
        Logger.ErrorFormat("ExecuteQueryToAD. Ошибка: {0} StackTrace: {1}", ex.Message, ex.StackTrace);
        return null;
      }
    }
    
    #endregion
    
    /// <summary>
    /// Получить или создать учетную запись.
    /// </summary>
    /// <param name="loginName"></param>
    /// <returns></returns>
    [Remote]
    public virtual ILogin GetOrCreateLogin(string loginName)
    {
      var login = Logins.GetAll(x => x.LoginName == loginName).FirstOrDefault();
      var domainName = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue("ADDomain");
      if (login == null && !string.IsNullOrEmpty(domainName))
      {
        login = Logins.Create();
        login.LoginName = domainName + @"\" + loginName;
        login.TypeAuthentication = Sungero.CoreEntities.Login.TypeAuthentication.Windows;
        login.Save();
      }
      
      return login;
    }
    
    /// <summary>
    /// Закрыть учетную запись.
    /// </summary>
    /// <param name="adAccountName">Имя учетной записи из Active Directory.</param>
    public virtual void CloseLogin(string adAccountName)
    {
      var rxLogin = Sungero.CoreEntities.Logins.GetAll(x => x.LoginName == adAccountName).FirstOrDefault();

      if (rxLogin != null)
      {
        rxLogin.Status = Sungero.CoreEntities.DatabookEntry.Status.Closed;
        Logger.DebugFormat("CloseLogin. Учетная запись с ИД {0} закрыта.", rxLogin.Id);
      }
      
      if (rxLogin.State.IsChanged)
        rxLogin.Save();
    }
    
    /// <summary>
    /// Обновить учетную запись сотрудника.
    /// </summary>
    /// <param name="adAccountName">Имя учетной записи из Active Directory.</param>
    /// <param name="employee">Сотрудник.</param>
    public virtual void UpdateOrCreateLogin(string adAccountName, Sungero.Company.IEmployee employee)
    {
      var rxLogin = Functions.Module.GetOrCreateLogin(adAccountName);
      if (rxLogin.Status == Sungero.CoreEntities.DatabookEntry.Status.Closed)
        rxLogin.Status = Sungero.CoreEntities.DatabookEntry.Status.Active;

      if (rxLogin != null && !Sungero.CoreEntities.Logins.Equals(employee.Login, rxLogin))
      {
        employee.Login = rxLogin;
        Logger.DebugFormat("UpdateOrCreateLogin. Учетная запись сотрудника с ИД {0} обновлена.", employee.Id);
      }
      
      if (employee.State.IsChanged)
        employee.Save();
    }
    
  }
  
}