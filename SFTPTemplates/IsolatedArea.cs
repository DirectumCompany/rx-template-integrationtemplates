using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Sungero.Core;

namespace DirRX.SFTP.Isolated.FTP
{
  public class ClientSFTP
  {
    private string UserName;
    private string Password;
    private string Url;

    /// <summary>
    /// Экземпляр FTP клиента
    /// </summary>
    public ClientSFTP(string userName, string password, string url)
    {
      this.UserName = userName;
      this.Password = password;
      this.Url = url;
    }

    /// <summary>
    /// Получение файла
    /// </summary>
    /// <param name="path">Путь к файлу.</param>
    /// <param name="status">Статус запроса</param>
    /// <param name="extension">Расширение файла</param>
    /// <returns>Данные файла в Stream</returns>
    public virtual Stream DownloadFile(string path, out string status, out string extension)
    {
      var pathFull = Url + path;
      var request = CreateRequest(WebRequestMethods.Ftp.DownloadFile, pathFull);
      FtpWebResponse response = (FtpWebResponse)request.GetResponse();
      status = response.StatusDescription;
      Stream responseStream = response.GetResponseStream();
      extension = Path.GetExtension(path);
      return responseStream;
    }

    /// <summary>
    /// Создание запроса
    /// </summary>
    /// <param name="method">FTP метод</param>
    /// <param name="method">Путь к файлу.</param>
    /// <returns>Запрос</returns>
    public virtual FtpWebRequest CreateRequest(string method, string path)
    {
      var r = (FtpWebRequest)WebRequest.Create(path);
      r.Credentials = new NetworkCredential(UserName, Password);
      r.Method = method;
      return r;
    }
  }
}