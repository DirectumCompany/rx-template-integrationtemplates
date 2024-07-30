using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sungero.Core;
using DirRX.SFTP.Structures.Module;

namespace DirRX.SFTP.Isolated.FTP
{
  public class IsolatedFunctions
  {
    
    /// <summary>
    /// Создает клиент для работы с SFTP.
    /// </summary>
    /// <param name="username">Имя пользователя.</param>
    /// <param name="password">Пароль.</param>
    /// <param name="url">URL SFTP.</param>
    /// <returns></returns>
    public virtual ClientSFTP CreateClientSFTP(string username, string password, string url)
    {
      return new ClientSFTP(username, password, url);
    }
    
    
  }
}