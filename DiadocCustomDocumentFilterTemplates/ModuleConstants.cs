using System;
using Sungero.Core;

namespace DirRX.DiadocCustomDocumentFilterTemplates.Module.Exchange.Constants
{
  public static class Module
  {
    
    /// <summary>
    /// Формат PO в комментариях неформализованных документов.
    /// </summary>
    public static class POCommentFormat
    {
      /// <summary>
      /// Формат PO в комментарии сервиса обмена на русском.
      /// </summary>
      public const string RusVersion = "РО:";
      
      /// <summary>
      /// Формат PO в комментарии сервиса обмена на английском.
      /// </summary>
      public const string EngVersion = "PO:";
    }
    
    /// <summary>
    /// Факт "Номер РО"
    /// </summary>
    public static class POVariants
    {
      /// <summary>
      /// Варинат 1 (процесс AP)
      /// </summary>
      public static class Process1
      {
        // Паттерн.
        public const string Pattern = @"^(\D{1})([0-9]{8})(\S+)";
        
        // Новое значение.
        public const string ReplaceWith = "P$2R";
      }
      
      /// <summary>
      /// Варинат 2 (процесс Rebates)
      /// </summary>
      public static class Process2
      {
        // Паттерн.
        public const string Pattern = @"^(\D{2})([0-9]{7})(\S*)";
        
        // Новое значение.
        public const string ReplaceWith = "RA$2";
      }
    }
  }
}