using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.QueueMessageForRequest.QueueMessages;

namespace DirRX.QueueMessageForRequest.Client
{
  partial class QueueMessagesActions
  {


    public virtual bool CanProcessingMessage(Sungero.Domain.Client.CanExecuteActionArgs e)
    {
      return true;
    }

    public virtual void ProcessingMessage(Sungero.Domain.Client.ExecuteActionArgs e)
    {
      PublicFunctions.QueueMessages.Remote.CreateAsyncHandlerProcessingMessage(_obj);
    }
  }

}