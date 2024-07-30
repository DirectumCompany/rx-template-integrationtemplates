using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using NpoComputer.DCX.Common;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DirRX.DiadocCustomDocumentFilterTemplates.Module.Exchange.Server
{
  partial class ModuleFunctions
  {
    
    /// <summary>
    /// Получить или создать документ из сервиса обмена.
    /// </summary>
    /// <param name="document">Документ из сообщения.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="serviceCounterpartyId">Id контрагента в сервисе обмена.</param>
    /// <param name="isIncoming">Признак документа от контрагента.</param>
    /// <param name="messageDate">Дата сообщения.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Документ RX.</returns>
    protected override Sungero.Docflow.IOfficialDocument GetOrCreateNewExchangeDocument(NpoComputer.DCX.Common.IDocument document, Sungero.Parties.ICounterparty sender,
                                                                                        string serviceCounterpartyId, bool isIncoming,
                                                                                        DateTime messageDate, Sungero.ExchangeCore.IBoxBase box)
    {
      Logger.DebugFormat("Exchange. Начало обработки GetOrCreateNewExchangeDocument box Id {0}.", box.Id);
      var exchangeDoc = Sungero.Docflow.OfficialDocuments.Null;
      
      var exchangeDocumentInfo =  Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ServiceEntityId);
      
      if (exchangeDocumentInfo != null)
        exchangeDoc = exchangeDocumentInfo.Document;
      
      if (exchangeDoc == null)
      {
        var documentFullName = CommonLibrary.FileUtils.NormalizeFileName(document.FileName);
        var documentName = System.IO.Path.GetFileNameWithoutExtension(documentFullName).TrimEnd('.');
        documentName = Sungero.Exchange.PublicFunctions.Module.Remote.GetValidFileName(documentName);
        var newInfo = GetOrCreateExchangeInfoWithoutDocument(document, sender, serviceCounterpartyId, isIncoming, messageDate, box);
        var convertedDocument = document as NpoComputer.DCX.Common.Document;
        var documentComment = string.IsNullOrEmpty(document.Comment) ? string.Empty : Resources.DocumentCommentFormat(document.Comment).ToString();

        if (string.IsNullOrEmpty(documentName))
          documentName = documentFullName;
        var taxDocumentClassifierCode = string.Empty;
        var functionUTD = string.Empty;

        // Неформализованный документ.
        if (document.DocumentType == DocumentType.Nonformalized)
        {
          Logger.DebugFormat("Exchange. Документ неформализованный.");
          exchangeDoc = this.CreateExchangeDocument(newInfo, sender, box, documentName, documentComment);
          ProcessNonformalizedDocument(document);
        }
        else
        {
          var taxDocumentClassifier = GetTaxDocumentClassifier(document);
          taxDocumentClassifierCode = taxDocumentClassifier.TaxDocumentClassifierCode;
          functionUTD = taxDocumentClassifier.TaxDocumentClassifierFunction;
          ProcessFormalizedDocument(document);
        }

        // Товарная накладная.
        if (document.DocumentType == DocumentType.Waybill &&
            taxDocumentClassifierCode != Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.GoodsTransferSeller &&
            taxDocumentClassifierCode != Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller)
        {
          var waybill = Sungero.FinancialArchive.PublicFunctions.Module.CreateWaybillDocument(documentComment, box, sender, newInfo);
          
          var documentInfo = this.GetInfoFromXML(document, sender);
          
          SetDocumentTotalAmount(waybill, documentInfo);
          
          exchangeDoc = waybill;
        }

        // Cчет-фактура.
        if (document.DocumentType == DocumentType.Invoice ||
            document.DocumentType == DocumentType.InvoiceCorrection ||
            document.DocumentType == DocumentType.InvoiceCorrectionRevision ||
            document.DocumentType == DocumentType.InvoiceRevision ||
            document.DocumentType == DocumentType.GeneralTransferSchfSeller ||
            document.DocumentType == DocumentType.GeneralTransferSchfCorrectionSeller ||
            document.DocumentType == DocumentType.GeneralTransferSchfCorrectionRevisionSeller ||
            document.DocumentType == DocumentType.GeneralTransferSchfRevisionSeller)
        {
          exchangeDoc = this.CreateTaxInvoice(convertedDocument, newInfo, sender, isIncoming, box);
        }

        // Акт.
        if (document.DocumentType == DocumentType.Act &&
            taxDocumentClassifierCode !=  Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.WorksTransferSeller &&
            taxDocumentClassifierCode != Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller)
        {
          var statement = Sungero.FinancialArchive.PublicFunctions.Module.CreateContractStatementDocument(documentComment, box, sender, newInfo);
          
          var documentInfo = this.GetInfoFromXML(document, sender);
          
          SetDocumentTotalAmount(statement, documentInfo);
          
          exchangeDoc = statement;
        }

        // Универсальный передаточный документ.
        var universalDocumentTaxInvoiceAndBasicTypes = new List<NpoComputer.DCX.Common.DocumentType>()
        {
          DocumentType.GeneralTransferSchfDopSeller,
          DocumentType.GeneralTransferSchfDopRevisionSeller,
          DocumentType.GeneralTransferSchfDopCorrectionSeller,
          DocumentType.GeneralTransferSchfDopCorrectionRevisionSeller
        };
        var universalDocumentBasicTypes = new List<NpoComputer.DCX.Common.DocumentType>()
        {
          DocumentType.GeneralTransferDopSeller,
          DocumentType.GeneralTransferDopRevisionSeller,
          DocumentType.GeneralTransferDopCorrectionSeller,
          DocumentType.GeneralTransferDopCorrectionRevisionSeller
        };
        
        var isUTD155ByXmlContent = taxDocumentClassifierCode == Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller155 &&
          functionUTD == Sungero.Exchange.Constants.Module.FunctionUTDDop;
        
        var isUTDByXmlContent = taxDocumentClassifierCode == Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.UniversalTransferDocumentSeller &&
          functionUTD == Sungero.Exchange.Constants.Module.FunctionUTDDop;
        
        var isUTDCorrectionByXmlContent = taxDocumentClassifierCode == Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.UniversalCorrectionDocumentSeller &&
          functionUTD == Sungero.Exchange.Constants.Module.FunctionUTDDopCorrection;
        
        if (universalDocumentTaxInvoiceAndBasicTypes.Contains(document.DocumentType.Value) ||
            universalDocumentBasicTypes.Contains(document.DocumentType.Value) ||
            isUTD155ByXmlContent || isUTDByXmlContent || isUTDCorrectionByXmlContent)
        {
          exchangeDoc = this.CreateUniversalTransferDocument(convertedDocument, newInfo, sender, box, universalDocumentTaxInvoiceAndBasicTypes);
        }
        
        // ДПРР.
        if (document.DocumentType == DocumentType.WorksTransferSeller ||
            document.DocumentType == DocumentType.WorksTransferRevisionSeller ||
            taxDocumentClassifierCode == Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.WorksTransferSeller)
        {
          exchangeDoc = this.CreateContractStatementDocument(convertedDocument, newInfo, sender, box);
        }
        
        // ДПТ.
        if (document.DocumentType == DocumentType.GoodsTransferSeller ||
            document.DocumentType == DocumentType.GoodsTransferRevisionSeller ||
            taxDocumentClassifierCode == Sungero.Exchange.PublicConstants.Module.TaxDocumentClassifier.GoodsTransferSeller)
        {
          exchangeDoc = this.CreateWaybillDocument(convertedDocument, newInfo, sender, box);
        }

        if (isIncoming)
          exchangeDoc.ExternalApprovalState = Sungero.Docflow.OfficialDocument.ExternalApprovalState.Signed;
        else
          exchangeDoc.InternalApprovalState = Sungero.Docflow.OfficialDocument.InternalApprovalState.Signed;
        
        newInfo.Document = exchangeDoc;
        
        if (Sungero.ExchangeCore.DepartmentBoxes.Is(box))
          exchangeDoc.Department = Sungero.ExchangeCore.PublicFunctions.BoxBase.GetDepartment(box);
        
        // Сбрасываем статус эл. обмена, чтобы при создании версии не сбрасывался статус согласования с КА.
        exchangeDoc.ExchangeState = null;

        this.CreateExchangeDocumentVersion(convertedDocument, newInfo, exchangeDoc, sender, isIncoming, box, documentFullName);
        
        newInfo.Save();
      }
      Logger.DebugFormat("Exchange. Конец обработки GetOrCreateNewExchangeDocument box Id {0}.", box.Id);
      return exchangeDoc;
    }
    
    /// <summary>
    /// Обработать формализованный документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void ProcessFormalizedDocument(NpoComputer.DCX.Common.IDocument document)
    {
      Logger.Debug("ProcessFormalizedDocument. Начало обработки документа.");
      // Реализация обработки формализованного документа.
    }
    
    /// <summary>
    /// Обработать неформализованный документ.
    /// </summary>
    /// <param name="document">Документ.</param>
    public virtual void ProcessNonformalizedDocument(NpoComputer.DCX.Common.IDocument document)
    {
      Logger.Debug("ProcessNonformalizedDocument. Начало обработки документа.");
      // Реализация обработки неформализованного документа.
    }
    
    /// <summary>
    /// Получить или создать информацию о документе из серивиса обмена (Копия из базового слоя).
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="sender">Отправитель.</param>
    /// <param name="serviceCounterpartyId">Id контрагента в сервисе обмена.</param>
    /// <param name="isIncoming">Признак документа от контрагента.</param>
    /// <param name="messageDate">Дата сообщения.</param>
    /// <param name="box">Абонентский ящик обмена.</param>
    /// <returns>Информация о документе.</returns>
    public static Sungero.Exchange.IExchangeDocumentInfo GetOrCreateExchangeInfoWithoutDocument(NpoComputer.DCX.Common.IDocument document, Sungero.Parties.ICounterparty sender,
                                                                                                string serviceCounterpartyId, bool isIncoming,
                                                                                                DateTime messageDate, Sungero.ExchangeCore.IBoxBase box)
    {
      Logger.DebugFormat("Exchange. Начало обработки GetOrCreateExchangeInfoWithoutDocument box Id {0}.", box.Id);
      var info = Sungero.Exchange.PublicFunctions.ExchangeDocumentInfo.GetExDocumentInfoByExternalId(box, document.ServiceEntityId);
      if (info != null)
        return info;
      
      var newInfo = Sungero.Exchange.ExchangeDocumentInfos.Create();
      newInfo.Box = box;
      newInfo.RootBox = Sungero.ExchangeCore.PublicFunctions.BoxBase.GetRootBox(box);
      newInfo.ServiceDocumentId = document.ServiceEntityId;
      newInfo.MessageType = isIncoming ? Sungero.Exchange.ExchangeDocumentInfo.MessageType.Incoming : Sungero.Exchange.ExchangeDocumentInfo.MessageType.Outgoing;
      newInfo.ServiceMessageId = document.ServiceMessageId;
      newInfo.Counterparty = sender;
      newInfo.ServiceCounterpartyId = serviceCounterpartyId;
      newInfo.MessageDate = Sungero.Docflow.PublicFunctions.Module.ToTenantTime(messageDate);
      newInfo.NeedSign = document.NeedSign;
      Logger.DebugFormat("Exchange. Конец обработки GetOrCreateExchangeInfoWithoutDocument box Id {0}. Create ExchangeDocumentInfo Id {1}.", box.Id, newInfo.Id);
      return newInfo;
    }
    
    /// <summary>
    /// Задать общую сумму (Копия из базового слоя).
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <param name="documentInfo">Информация о документе.</param>
    private static void SetDocumentTotalAmount(Sungero.Docflow.IAccountingDocumentBase document, Sungero.Exchange.Structures.Module.FormalizedDocumentXML documentInfo)
    {
      if (!string.IsNullOrEmpty(documentInfo.CurrencyCode))
      {
        var currency = Sungero.Commons.Currencies.GetAll().Where(x => x.NumericCode == documentInfo.CurrencyCode).FirstOrDefault();
        
        if (currency != null)
        {
          document.Currency = currency;
          document.TotalAmount = documentInfo.TotalAmount;
        }
      }
    }
    
    /// <summary>
    /// Получить классификатор налоговых документов (Копия из базового слоя).
    /// </summary>
    /// <param name="document">Документ.</param>
    /// <returns>Классификатор.</returns>
    private static Sungero.Exchange.Structures.Module.ITaxDocumentClassifier GetTaxDocumentClassifier(IDocument document)
    {
      return Sungero.Exchange.PublicFunctions.Module.GetTaxDocumentClassifierByContent(new System.IO.MemoryStream(document.Content));
    }
    
    /// <summary>
    /// Поиск номера PO в документе.
    /// </summary>
    /// <param name="exchangeDocument">Документ.</param>
    /// <returns>Номер PO.</returns>
    [Remote(IsPure=true), Public]
    public string FindPurchaseOrderInDocument(Sungero.Docflow.IOfficialDocument exchangeDocument)
    {
      Logger.DebugFormat("FindPurchaseOrderInDocument. ИД документа {0}. Начало.", exchangeDocument.Id);
      var purchaseOrderNumber = string.Empty;
      
      var accountingDocument = Sungero.Docflow.AccountingDocumentBases.As(exchangeDocument);
      
      if (accountingDocument != null && !string.IsNullOrWhiteSpace(accountingDocument.PurchaseOrderNumber))
        return accountingDocument.PurchaseOrderNumber;
      
      // Если не удалось извлечь РО, тогда нужно проверить поле "Примечание".
      if(string.IsNullOrWhiteSpace(accountingDocument.PurchaseOrderNumber))
      {
        var purchaseOrderString = string.Empty;
        var documentNote = exchangeDocument.Note != null ?
          exchangeDocument.Note.ToUpper() :
          string.Empty;
        Logger.DebugFormat("FindPurchaseOrderInDocument. ИД документа {0}. Поиск PO в поле Примечание {1}.", exchangeDocument.Id, documentNote);
        if (!string.IsNullOrWhiteSpace(documentNote))
        {
          // Правила поиска номера РО.
          var replacePatterns = new List<Structures.Module.IReplacePattern>
          {
            Structures.Module.ReplacePattern.Create(Constants.Module.POVariants.Process1.Pattern,
                                                    Constants.Module.POVariants.Process1.ReplaceWith),
            Structures.Module.ReplacePattern.Create(Constants.Module.POVariants.Process2.Pattern,
                                                    Constants.Module.POVariants.Process2.ReplaceWith)
          };
          
          // Проверить в строке наличие необходимого указателя, либо найти номер РО.
          var purchaseOrderFormat = string.Empty;
          if (documentNote.IndexOf(Constants.Module.POCommentFormat.EngVersion) > -1)
            purchaseOrderFormat = Constants.Module.POCommentFormat.EngVersion;
          else if (documentNote.IndexOf(Constants.Module.POCommentFormat.RusVersion) > -1)
            purchaseOrderFormat = Constants.Module.POCommentFormat.RusVersion;
          else
          {
            foreach (var replacePattern in replacePatterns)
            {
              purchaseOrderString = documentNote
                .Split(' ')
                .Where(s => Regex.Match(s.Trim(), replacePattern.Pattern).Success)
                .FirstOrDefault();
              
              if(!string.IsNullOrWhiteSpace(purchaseOrderString))
                break;
            }
          }
          
          Logger.DebugFormat("FindPurchaseOrderInDocument. ИД документа {0}. purchaseOrderFormat {1} purchaseOrderString {2}.", exchangeDocument.Id, purchaseOrderFormat, purchaseOrderString);
          if (!string.IsNullOrEmpty(purchaseOrderFormat) && string.IsNullOrEmpty(purchaseOrderString))
          {
            var purchaseOrderIndex = documentNote.IndexOf(purchaseOrderFormat) + purchaseOrderFormat.Length;
            
            // Найти в строке индекс знаков-разделителей или знаков-пробелов.
            var endPurchaseOrderIndex = documentNote
              .Select((c, index) => { if (index > purchaseOrderIndex && (char.IsSeparator(c) || char.IsWhiteSpace(c))) return index; return -1; })
              .FirstOrDefault(i => i != -1);
            
            purchaseOrderString = endPurchaseOrderIndex != 0
              ? documentNote.Substring(purchaseOrderIndex, endPurchaseOrderIndex - purchaseOrderIndex)
              : documentNote.Substring(purchaseOrderIndex);
            
            // Проверить есть ли в полученной строке знаки-препинания после указателя PO.
            endPurchaseOrderIndex = purchaseOrderString
              .Select((c, index) => { if (char.IsPunctuation(c)) return index; return -1; })
              .FirstOrDefault(i => i != -1);
            
            if (endPurchaseOrderIndex != 0)
              purchaseOrderString = purchaseOrderString.Substring(0, endPurchaseOrderIndex);
            
            foreach (var replacePattern in replacePatterns)
              purchaseOrderString = Regex.Replace(purchaseOrderString, replacePattern.Pattern, replacePattern.ReplaceWith);
          }
          
          Logger.DebugFormat("FindPurchaseOrderInDocument. ИД документа {0}. Конец обработки. purchaseOrderString {1}.", exchangeDocument.Id, purchaseOrderString);
          return string.IsNullOrEmpty(purchaseOrderString) ? purchaseOrderString : purchaseOrderString.Trim();
        }
      }
      return purchaseOrderNumber.Trim();
    }
    
    protected override Sungero.Docflow.IOfficialDocument CreateWaybillDocument(Document document, Sungero.Exchange.IExchangeDocumentInfo info, Sungero.Parties.ICounterparty sender, Sungero.ExchangeCore.IBoxBase box)
    {
      var result = base.CreateWaybillDocument(document, info, sender, box);
      
      // Ваша реализация.
      
      return result;
    }
    
    protected override Sungero.Docflow.IOfficialDocument CreateContractStatementDocument(Document document, Sungero.Exchange.IExchangeDocumentInfo info, Sungero.Parties.ICounterparty sender, Sungero.ExchangeCore.IBoxBase box)
    {
      var result = base.CreateContractStatementDocument(document, info, sender, box);
      
      // Ваша реализация.
      
      return result;
    }
    
    protected override Sungero.Docflow.IOfficialDocument CreateUniversalTransferDocument(Document document, Sungero.Exchange.IExchangeDocumentInfo info, Sungero.Parties.ICounterparty sender, Sungero.ExchangeCore.IBoxBase box, List<DocumentType> universalDocumentTaxInvoiceAndBasicTypes)
    {
      var result = base.CreateUniversalTransferDocument(document, info, sender, box, universalDocumentTaxInvoiceAndBasicTypes);
      
      // Ваша реализация.
      
      return result;
    }
    
    protected override Sungero.Docflow.IOfficialDocument CreateTaxInvoice(Document document, Sungero.Exchange.IExchangeDocumentInfo info, Sungero.Parties.ICounterparty sender, bool isIncomingMessage, Sungero.ExchangeCore.IBoxBase box)
    {
      var result = base.CreateTaxInvoice(document, info, sender, isIncomingMessage, box);
      
      // Ваша реализация.
      
      return result;
    }
    
  }
}