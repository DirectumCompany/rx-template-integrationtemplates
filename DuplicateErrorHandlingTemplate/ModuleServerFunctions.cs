using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties;
using Sungero.Metadata;

namespace DirRX.DuplicateErrorHandling.Server
{
  public class ModuleFunctions
  {
    #region Поиск дубликатов для НОР.
    /// <summary>
    /// Поиск дубликатов для НОР.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="TIN">ИНН.</param>
    /// <param name="TRRC">КПП.</param>
    /// <returns>Дубликаты НОР.</returns>
    [Remote]
    public IQueryable<IBusinessUnit> FindingDuplicatesBusinessUnits(string name, string TIN, string TRRC)
    {
      TIN = CheckInnOrKpp(TIN, 5)?.ToLower();
      TRRC = CheckInnOrKpp(TRRC, 5)?.ToLower();
      name = name?.Trim();

      var query = BusinessUnits.GetAll();

      if (!string.IsNullOrWhiteSpace(name))
      {
        query = query.Where(c => c.Name == name);
      }
      if (!string.IsNullOrWhiteSpace(TIN) && string.IsNullOrWhiteSpace(TRRC))
      {
        query = query.Where(c => c.TIN == TIN);
      }
      if (!string.IsNullOrWhiteSpace(TIN) && !string.IsNullOrWhiteSpace(TRRC))
      {
        query = query.Where(c => c.TIN == TIN && c.TRRC == TRRC);
      }
      return query;
    }
    #endregion
    
    #region Поиск дубликатов для Контрагента.
    /// <summary>
    /// Поиск дубликатов для Контрагентов.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="TIN">ИНН.</param>
    /// <param name="TRRC">КПП.</param>
    /// <param name="PSRN">ОГРН.</param>
    /// <returns>Дубликаты Контрагента.</returns>
    [Remote]
    public IQueryable<ICompany> FindingDuplicatesCompanies(string name, string TIN, string TRRC, string PSRN)
    {
      TIN = CheckInnOrKpp(TIN, 5)?.ToLower();
      TRRC = CheckInnOrKpp(TRRC, 5)?.ToLower();
      name = name?.Trim();
      PSRN = PSRN?.Trim().ToLower();

      var query = Companies.GetAll();

      if (!string.IsNullOrWhiteSpace(name))
      {
        query = query.Where(c => c.Name == name);
      }
      if (!string.IsNullOrWhiteSpace(TIN) && string.IsNullOrWhiteSpace(TRRC))
      {
        query = query.Where(c => c.TIN == TIN);
      }
      if (!string.IsNullOrWhiteSpace(TIN) && !string.IsNullOrWhiteSpace(TRRC))
      {
        query = query.Where(c => c.TIN == TIN && c.TRRC == TRRC);
      }
      if (!string.IsNullOrWhiteSpace(PSRN))
      {
        query = query.Where(c => c.PSRN == PSRN);
      }

      return query;
    }
    
    
    #endregion
    
    #region Поиск дубликатов для Подразделения.
    /// <summary>
    /// Поиск дубликатов для Подразделения.
    /// </summary>
    /// <param name="headOffice">Головное подразделение.</param>
    /// <returns>Дубликаты подразделения.</returns>
    [Remote]
    public IQueryable<IDepartment> FindingDuplicateDepartments(string departmentId)
    {
      if (string.IsNullOrWhiteSpace(departmentId))
        throw new ArgumentException("Не все параметры заполнены");
      
      try
      {
        var department = Departments.Get(long.Parse(departmentId));
        if (department.Equals(Departments.Null))
          throw new NullReferenceException(string.Format("Данные с Id:{0} не найдены", departmentId));
        
        return Departments.GetAll(d => d.HeadOffice.Equals(department.HeadOffice) && d.Status.Equals(department.Status));
      }
      catch (Exception ex)
      {
        throw new Exception(ex.Message);
      }
    }
    #endregion
    
    #region Поиск дубликатов для Сотрудника.
    /// <summary>
    /// Поиск дубликатов для Сотрудника по персоне.
    /// </summary>
    /// <param name="person">Персона.</param>
    /// <param name="personnelNumber">Табельный номер.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="jobTitle">Должность.</param>
    /// <param name="email">Эл. почта.</param>
    /// <param name="phone">Рабочий телефон.</param>
    /// <returns>Дубликаты сотрудника.</returns>
    [Remote]
    public IQueryable<IEmployee> FindingDuplicateEmployees(IPerson person, string personnelNumber, IDepartment department, IJobTitle jobTitle, string email, string phone)
    {
      if (person.Equals(People.Null) || string.IsNullOrWhiteSpace(personnelNumber) || department.Equals(Departments.Null) ||
          (jobTitle.Equals(JobTitles.Null) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone)))
        throw new ArgumentException("Не все параметры заполнены");
      
      return Employees.GetAll(e => !e.Person.Equals(People.Null) && e.Person.Equals(person) && e.PersonnelNumber == personnelNumber && e.Department.Equals(department) &&
                              (e.JobTitle.Equals(jobTitle) || e.Email == email || e.Phone == phone));
    }
    
    /// <summary>
    /// Поиск дубликатов для Сотрудника по ФИО.
    /// </summary>
    /// <param name="lastName">Фамилия.</param>
    /// <param name="firstName">Имя.</param>
    /// <param name="middleName">Отчество.</param>
    /// <param name="personnelNumber">Табельный номер.</param>
    /// <param name="department">Подразделение.</param>
    /// <param name="jobTitle">Должность.</param>
    /// <param name="email">Эл. почта.</param>
    /// <param name="phone">Рабочий телефон.</param>
    /// <returns>Дубликаты сотрудника.</returns>
    [Remote]
    public IQueryable<IEmployee> FindingDuplicateEmployees(string lastName, string firstName, string middleName, string personnelNumber, IDepartment department, IJobTitle jobTitle, string email, string phone)
    {
      if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(middleName) ||
          string.IsNullOrWhiteSpace(personnelNumber) || department.Equals(Departments.Null) ||
          (jobTitle.Equals(JobTitles.Null) && string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone)))
        throw new ArgumentException("Не все параметры заполнены");
      
      return Employees.GetAll(e => !e.Person.Equals(People.Null)  &&  e.Person.FirstName == firstName && e.Person.MiddleName == middleName && e.Person.LastName == lastName &&
                              e.PersonnelNumber == personnelNumber && e.Department.Equals(department) &&
                              (e.JobTitle.Equals(jobTitle) || e.Email == email || e.Phone == phone));
    }
    #endregion
    
    #region Методы для проверки ИНН и КПП.
    /// <summary>
    /// Проверка ИНН и КПП на "мусор".
    /// </summary>
    /// <param name="checkValue">Переданное значение ИНН или КПП, которое надо проверить.</param>
    /// <param name="minValueLenght">Длина строки, которая считается "мусором".</param>
    /// <description>"Мусором" считается: длина 5 или меньше символов или все символы одинаковые (0,1, -, .), значения "DELETED" или "НЕ ПРИМЕНИМО".</description>
    /// <returns>Если значение прошло проверку, то возвращается переданная в checkValue строка c удаленными ведущими пробелами, иначе возвращается пустая строка.</returns>
    [Public]
    public string CheckInnOrKpp(string checkValue, int minValueLenght)
    {
      if (!string.IsNullOrEmpty(checkValue))
      {
        checkValue = checkValue.Trim();
        
        // Длина, которая считается "мусором" или меньше символов.
        if (checkValue.Length < minValueLenght)
          return string.Empty;
        // Значения "DELETED" или "НЕ ПРИМЕНИМО".
        if (checkValue ==  "DELETED" ||
            checkValue ==  "НЕ ПРИМЕНИМО")
          return string.Empty;
        
        // Все символы одинаковые.
        var checkValueArray = checkValue.ToCharArray();
        var uniqueValue = checkValueArray[0];
        var isUniqueValue = true;
        foreach (var charValue in checkValueArray)
        {
          if (charValue != uniqueValue)
          {
            isUniqueValue = false;
            break;
          }
        }
        
        if (isUniqueValue &&
            (uniqueValue == '0' ||
             uniqueValue == '1' ||
             uniqueValue == '-' ||
             uniqueValue == '.'))
          return string.Empty;
      }
      else
        return string.Empty;
      
      return checkValue;
    }

    /// <summary>
    /// Проверка ИНН и КПП на валидность.
    /// </summary>
    /// <param name="inn">Переданное значение ИНН которое надо проверить.</param>
    /// <returns>Если значение прошло проверку, то возвращается True, если надейны ощибки - то False.</returns>
    public static bool IsValidINN(string inn)
    {
      if (string.IsNullOrEmpty(inn))
        return false;

      // Удаляем пробелы и проверяем длину ИНН
      inn = inn.Trim();
      if (inn.Length != 10 && inn.Length != 12)
        return false;

      // Преобразуем строку в массив целых чисел
      if (!inn.All(char.IsDigit))
        return false;

      var digits = inn.Select(c => int.Parse(c.ToString())).ToArray();

      if (digits.Length == 10)
      {
        return ValidateINN10(digits);
      }
      else if (digits.Length == 12)
      {
        return ValidateINN12(digits);
      }

      return false;
    }
    
    /// <summary>
    /// Проверка 10-ти значных ИНН.
    /// </summary>
    /// <param name="digits">Последовательность значений.</param>
    /// <returns>Если значение прошло проверку, то возвращается True, если надейны ощибки - то False.</returns>
    private static bool ValidateINN10(int[] digits)
    {
      // Весовые коэффициенты для ИНН из 10 цифр
      int[] coefficients = { 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      int controlSum = 0;
      for (int i = 0; i < coefficients.Length; i++)
      {
        controlSum += digits[i] * coefficients[i];
      }
      int controlDigit = controlSum % 11;
      if (controlDigit > 9)
      {
        controlDigit %= 10;
      }
      return controlDigit == digits[9];
    }
    
    /// <summary>
    /// Проверка 12-ти значных ИНН.
    /// </summary>
    /// <param name="digits">Последовательность значений.</param>
    /// <returns>Если значение прошло проверку, то возвращается True, если надейны ощибки - то False.</returns>
    private static bool ValidateINN12(int[] digits)
    {
      // Весовые коэффициенты для 11-й цифры ИНН из 12 цифр
      int[] coefficients1 = { 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      int controlSum1 = 0;
      for (int i = 0; i < coefficients1.Length; i++)
      {
        controlSum1 += digits[i] * coefficients1[i];
      }
      int controlDigit1 = controlSum1 % 11;
      if (controlDigit1 > 9)
      {
        controlDigit1 %= 10;
      }

      // Весовые коэффициенты для 12-й цифры ИНН из 12 цифр
      int[] coefficients2 = { 3, 7, 2, 4, 10, 3, 5, 9, 4, 6, 8 };
      int controlSum2 = 0;
      for (int i = 0; i < coefficients2.Length; i++)
      {
        controlSum2 += digits[i] * coefficients2[i];
      }
      int controlDigit2 = controlSum2 % 11;
      if (controlDigit2 > 9)
      {
        controlDigit2 %= 10;
      }

      return controlDigit1 == digits[10] && controlDigit2 == digits[11];
    }

    /// <summary>
    /// Проверка КПП на валидность.
    /// </summary>
    /// <param name="inn">Переданное значение КПП которое надо проверить.</param>
    /// <returns>Если значение прошло проверку, то возвращается True, если надейны ощибки - то False.</returns>
    public static bool IsValidKPP(string kpp)
    {
      if (string.IsNullOrEmpty(kpp))
        return false;

      // Удаляем пробелы и проверяем длину КПП
      kpp = kpp.Trim();
      if (kpp.Length != 9)
        return false;

      // Проверяем, что первые 4 символа являются цифрами (код налогового органа)
      for (int i = 0; i < 4; i++)
      {
        if (!char.IsDigit(kpp[i]))
          return false;
      }

      // Проверяем, что следующие 2 символа являются цифрами (причина постановки на учет)
      for (int i = 4; i < 6; i++)
      {
        if (!char.IsDigit(kpp[i]))
          return false;
      }

      // Проверяем, что последние 3 символа являются цифрами (порядковый номер)
      for (int i = 6; i < 9; i++)
      {
        if (!char.IsDigit(kpp[i]))
          return false;
      }

      // Дополнительная проверка на корректность причины постановки на учет
      int reasonCode = int.Parse(kpp.Substring(4, 2));
      if (reasonCode < 1 || reasonCode > 99)
        return false;

      return true;
    }

    #endregion
  }
}
