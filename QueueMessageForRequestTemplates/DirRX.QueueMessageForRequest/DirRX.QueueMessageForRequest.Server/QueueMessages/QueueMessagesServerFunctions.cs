using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.QueueMessageForRequest.QueueMessages;

namespace DirRX.QueueMessageForRequest.Server
{
  partial class QueueMessagesFunctions
  {
    /// <summary>
    /// Создает АО для обработки сообщения.
    /// </summary>
    [Remote, Public]
    public virtual void CreateAsyncHandlerProcessingMessage()
    {
      var processingMessage = QueueMessageForRequest.AsyncHandlers.ProcessingMessage.Create();
      processingMessage.MessageId = _obj.Id;
      processingMessage.ExecuteAsync(DirRX.QueueMessageForRequest.QueueMessageses.Resources.ProcessingStart,
                                     DirRX.QueueMessageForRequest.QueueMessageses.Resources.ProcessingCompleted,
                                     DirRX.QueueMessageForRequest.QueueMessageses.Resources.ProcessingError,
                                     Sungero.CoreEntities.Users.Current);
    }
  }
}