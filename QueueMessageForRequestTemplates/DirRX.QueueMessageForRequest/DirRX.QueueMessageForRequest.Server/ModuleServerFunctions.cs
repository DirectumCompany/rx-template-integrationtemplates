using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.QueueMessageForRequestTemplates;

namespace DirRX.QueueMessageForRequest.Server
{
  public class ModuleFunctions
  {
    /// <summary>
    /// Отправка сообщения синхронно.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    public virtual void SendMessageSync(QueueMessageForRequest.IQueueMessages message)
    {
      var isForcedLocked = false;
      
      try
      {
        isForcedLocked = Locks.TryLock(message);
        
        if (isForcedLocked)
        {
          Functions.Module.SendMessage(message);
        }
        else
        {
          Logger.ErrorFormat("SendMessageSync. Не удалось заблокировать сообщение. Id: {0}", message.Id);
          return;
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("SendMessageSync. Ошибка при отправке сообщения. Id: {0}, Текст ошибки: {1}, StackTrace: {2}",
                           message.Id, ex.Message, ex.StackTrace);
        
        Functions.Module.HandlingErrorSend(ex, message);
      }
      finally
      {
        if (isForcedLocked)
          Locks.Unlock(message);
      }
    }
    
    /// <summary>
    /// Отправка сообщения асинхронно.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    public virtual void SendMessageAsync(QueueMessageForRequest.IQueueMessages message)
    {
      var sendMessage = QueueMessageForRequest.AsyncHandlers.SendMessage.Create();
      sendMessage.MessageId = message.Id;
      sendMessage.ExecuteAsync();
    }
    
    /// <summary>
    /// Возвращает реестр сообщений.
    /// </summary>
    /// <returns>Реестр сообщений.</returns>
    public virtual IQueryable<QueueMessageForRequest.IQueueMessages> GetMessagesQueue()
    {
      return QueueMessageForRequest.QueueMessageses
        .GetAll()
        .Where(q => q.Status == QueueMessageForRequest.QueueMessages.Status.Active &&
               q.ProcessingStatus == QueueMessageForRequest.QueueMessages.ProcessingStatus.NotProcessed);
    }
    
    /// <summary>
    /// Возвращает реестр устаревших сообщений для последующего удаления.
    /// </summary>
    /// <returns>Реестр сообщений.</returns>
    public virtual IQueryable<QueueMessageForRequest.IQueueMessages> GetMessagesQueueForDelete()
    {
      // Количество дней в месяце.
      var countDayInMount = DateTime.DaysInMonth(Calendar.Today.Year, Calendar.Today.Month);
      
      return QueueMessageForRequest.QueueMessageses
        .GetAll()
        .Where(q => q.LastUpdate < Calendar.Today.AddDays(-countDayInMount) &&
               q.Status == QueueMessageForRequest.QueueMessages.Status.Active);
      
    }
    
    /// <summary>
    /// Обработка сообщения, если возникла ошибка.
    /// </summary>
    /// <param name="ex">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    public virtual void HandlingErrorSend(System.Exception ex, QueueMessageForRequest.IQueueMessages message)
    {
      // Логика по обработке сообщения, если возникла ошибка.
    }
    
    /// <summary>
    /// Метод для отправки сообщения во внешнюю систему.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    public virtual void SendMessage(QueueMessageForRequest.IQueueMessages message)
    {
      // Логика по отправке сообщений во внешнюю систему.
    }
    
    /// <summary>
    /// Обработка сообщения, если возникла ошибка.
    /// </summary>
    /// <param name="ex">Исключение.</param>
    /// <param name="message">Сообщение.</param>
    public virtual void ProcessingMessageError(System.Exception ex, QueueMessageForRequest.IQueueMessages message)
    {
      // Логика по обработке сообщений, если возникла ошибка.
    }
    
    /// <summary>
    /// Обработка сообщения.
    /// </summary>
    /// <param name="message"></param>
    public virtual void ProcessingMessage(QueueMessageForRequest.IQueueMessages message)
    {
      // Логика по обработке сообщений.
    }
    
    /// <summary>
    /// Метод для формирования и отправки ответа.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <returns>Строка в виде ответа.</returns>
    public virtual string CreateAnswer(QueueMessageForRequest.IQueueMessages message)
    {
      return string.Empty;
    }
    
    /// <summary>
    /// Создание записи в справочнике очереди сообщений.
    /// </summary>
    /// <param name="processingStatus">Статус работы.</param>
    /// <param name="lastUpdate">Дата изменения.</param>
    /// <returns>Записи в справочнике очереди сообщений.</returns>
    public virtual QueueMessageForRequest.IQueueMessages CreateMessage(string name,
                                                                       Sungero.Core.Enumeration processingStatus,
                                                                       DateTime lastUpdate,
                                                                       byte[] body)
    {
      var message = QueueMessageForRequest.QueueMessageses.Create();
      message.Name = name;
      message.ProcessingStatus = processingStatus;
      message.LastUpdate = lastUpdate;
      
      using (var stream = new MemoryStream(body))
        message.Body.Write(stream);
      
      message.Save();
      
      return message;
    }
    
    /// <summary>
    /// Метод для создания сообщения и его обработки асинхронно.
    /// </summary>
    /// <param name="name">Имя.</param>
    /// <param name="body">Тело файла.</param>
    /// <returns>Ответ.</returns>
    [Public(WebApiRequestType = RequestType.Post)]
    public virtual string CreateMessageQueue(string name, byte[] body)
    {
      var message = Functions.Module.CreateMessage(name,
                                                   QueueMessageForRequest.QueueMessages.ProcessingStatus.NotProcessed,
                                                   Calendar.Today,
                                                   body);
      
      return Functions.Module.CreateAnswer(message);
    }
  }
}