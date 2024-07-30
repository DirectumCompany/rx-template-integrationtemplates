using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace DirRX.QueueMessageForRequest.Server
{
  public class ModuleJobs
  {

    /// <summary>
    /// Отправка сообщений из реестра во внешнюю систему.
    /// </summary>
    public virtual void SendMessage()
    {
      var queueMessages = Functions.Module.GetMessagesQueue();
      
      foreach (var message in queueMessages)
      {
        if (message.ExecutedAsync == true)
          Functions.Module.SendMessageAsync(message);
        else
          Functions.Module.SendMessageSync(message);
      }
    }

    /// <summary>
    /// Удаление обработанных сообщений.
    /// </summary>
    public virtual void RemoveQueueMessages()
    {
      var queueMessages = Functions.Module.GetMessagesQueueForDelete();
      
      foreach (var message in queueMessages)
      {
        try
        {
          using (var nullstream = new System.IO.MemoryStream())
          {
            // вынести в отдельный метод.
            if (message.Body != null && (message.Body.Size > 0))
              message.Body.Write(nullstream);
            message.Status = QueueMessageForRequest.QueueMessages.Status.Closed;
            message.Save();
          }
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("RemoveQueueMessages.  Ошибка при удалении сообщения. Id: {0}, Текст ошибки: {1}, StackTrace: {2}",
                            message.Id, ex.Message, ex.StackTrace);
        }
      }
    }
  }
}