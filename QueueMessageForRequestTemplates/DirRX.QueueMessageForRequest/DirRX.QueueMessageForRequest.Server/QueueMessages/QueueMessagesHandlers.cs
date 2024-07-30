using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using DirRX.QueueMessageForRequest.QueueMessages;

namespace DirRX.QueueMessageForRequest
{
  partial class QueueMessagesServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      base.BeforeSave(e);
      
      e.Params.AddOrUpdate(Constants.Module.FirstSave, _obj.State.IsInserted);
    }

    public override void AfterSave(Sungero.Domain.AfterSaveEventArgs e)
    {
      base.AfterSave(e);
      
      var isFirstSave = false;
      if (e.Params.TryGetValue(Constants.Module.FirstSave, out isFirstSave) && isFirstSave)
      {
        var async = AsyncHandlers.ProcessingMessage.Create();
        async.MessageId = _obj.Id;
        async.ExecuteAsync();
      }
    }
  }

}