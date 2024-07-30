using System;
using Sungero.Core;

namespace DirRX.QueueMessageForRequest.Constants
{
  public static class Module
  {

    /// <summary>
    /// Константа определяющая максимальное количество повторов.
    /// </summary>
    public const int MaxCountRetries = 5;
    
    /// <summary>
    /// Константа для пердачи информации, что было первое сохранение.
    /// </summary>
    public const string FirstSave = "FirstSave";

  }
}