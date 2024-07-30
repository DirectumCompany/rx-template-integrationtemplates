using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.Serialization.Structures.Module
{

  /// <summary>
  /// Структура подразделения.
  /// </summary>
  [Public(Isolated = true)]
  partial class Department
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Code { get; set; }
    public long BusinessUnitId { get; set; }
    public System.Collections.Generic.List<DirRX.Serialization.Structures.Module.IDepartment> Departments { get; set; }
  }

  /// <summary>
  /// Структура НОР.
  /// </summary>
  [Public(Isolated = true)]
  partial class BussinessUnit
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public string LegalName { get; set; }
    public string TIN { get; set; }
    public string TRRC { get; set; }
    public System.Collections.Generic.List<DirRX.Serialization.Structures.Module.IDepartment> Departments { get; set; }
  }
  
}