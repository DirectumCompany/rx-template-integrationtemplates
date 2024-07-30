using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.QueueMessageForRequestTemplates;

namespace DirRX.QueueMessageForRequest.Server
{
  public class ModuleAsyncHandlers
  {

    public virtual void SendMessage(DirRX.QueueMessageForRequest.Server.AsyncHandlerInvokeArgs.SendMessageInvokeArgs args)
    {
      Logger.DebugFormat("SendMessageAsync. Старт асинхронного обработчика");
      
      var message = QueueMessageForRequest.QueueMessageses
        .GetAll()
        .Where(m => m.Id == args.MessageId)
        .FirstOrDefault();
      
      
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
          Logger.ErrorFormat("SendMessageAsync. Не удалось заблокировать сообщение. Id: {0}", message.Id);
          args.Retry = true;
          return;
        }
      }
      catch (Exception ex)
      {
        Logger.ErrorFormat("SendMessageAsync. Ошибка при отправке сообщения. Id: {0}, Текст ошибки: {1}, StackTrace: {2}",
                           message.Id, ex.Message, ex.StackTrace);
        
        Functions.Module.HandlingErrorSend(ex, message);
      }
      finally
      {
        if (isForcedLocked)
          Locks.Unlock(message);
      }
    }

    public virtual void ProcessingMessage(DirRX.QueueMessageForRequest.Server.AsyncHandlerInvokeArgs.ProcessingMessageInvokeArgs args)
    {
      Logger.DebugFormat("ProcessingMessage. Старт асинхронного обработчика");
      
      var message = QueueMessageForRequest.QueueMessageses
        .GetAll()
        .Where(m => m.Id == args.MessageId)
        .FirstOrDefault();
      
      if (message == null)
      {
        Logger.DebugFormat("ProcessingMessage. Сообщение не найдено: id {0}", args.MessageId);
      }
      
      var isForcedLocked = false;
      
      try
      {
        isForcedLocked = Locks.TryLock(message);
        
        if (isForcedLocked)
          Functions.Module.ProcessingMessage(message);
        else
        {
          Logger.ErrorFormat("ProcessingMessage. Не удалось заблокировать сообщение. Id: {0}", args.MessageId);
          args.Retry = true;
          return;
        }
      }
      catch (Exception ex)
      {
        Functions.Module.ProcessingMessageError(ex, message);
        
        Logger.ErrorFormat("ProcessingMessage. Ошибка обработки сообщения из реестра. Id: {0}, Текст ошибки: {1}, StackTrace: {2}",
                           args.MessageId, ex.Message, ex.StackTrace);
      }
      finally
      {
        if (isForcedLocked)
          Locks.Unlock(message);
      }
    }

  }
}