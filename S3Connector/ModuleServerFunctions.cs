using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.Docflow.OfficialDocument;
using System.Text;
using Sungero.Domain;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;

namespace DirRX.S3Connector.Server
{
  public class ModuleFunctions
  {
    
    /// <summary>
    /// Скачивание файла из AWS S3.
    /// </summary>
    /// <param name="AWSpath">Путь к файлу в AWS S3.</param>
    /// <param name="sedId">СЭД ИД.</param>
    /// <returns>Новый путь к файлу.</returns>
    [Remote, Public]
    public string FSDownloadFile(string AWSpath, string sedId)
    {
      //Для примера настройки подключения берутся из таблицы DocflowParams. При копировании вы можете брать настройки из других удобных вам источников.
      var fsServiceURL = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue("fsServiceURL");
      var fsAccessKey = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue("fsAccessKey");
      var fsSecretKey = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue("fsSecretKey");
      var fsBusketName = Sungero.Docflow.PublicFunctions.Module.Remote.GetDocflowParamsStringValue("fsBusketName");
      
      //Вырезаем имя файла из полного AWS пути
      if (!string.IsNullOrWhiteSpace(fsServiceURL) && !string.IsNullOrWhiteSpace(fsAccessKey) &&
          !string.IsNullOrWhiteSpace(fsSecretKey) && !string.IsNullOrWhiteSpace(fsBusketName) && !string.IsNullOrWhiteSpace(AWSpath))
      {
        string fileName = AWSpath.Split(Path.DirectorySeparatorChar).Last();
        string fname = string.Concat(Calendar.Now.ToString("hhmmss"), "_", fileName);
        string dirPath = Path.Combine(Path.GetTempPath(), DirRX.S3Connector.Resources.VersionPathDir, sedId);
        //Создать папку если она отсутствует
        Directory.CreateDirectory(dirPath);
        string newFilePath = Path.Combine(dirPath, fname);
        try
        {
          Logger.Debug(string.Format("Start AWS S3 Downloading SED = {0} Link = {1}", sedId, AWSpath));
          //Создаем подключение к AWS S3, скачиваем файл
          AmazonS3Config newClientConfig = new AmazonS3Config();
          newClientConfig.ServiceURL = fsServiceURL;
          newClientConfig.ForcePathStyle = true;
          //Свойство подключения AWS S3: Версия цифровой подписи
          newClientConfig.SignatureVersion = "v4";
          AmazonS3Client s3Client = new AmazonS3Client(fsAccessKey, fsSecretKey, newClientConfig);
          GetObjectRequest request = new GetObjectRequest();
          request.BucketName = fsBusketName;
          request.Key = AWSpath;
          GetObjectResponse response = s3Client.GetObject(request);
          Logger.Debug("HTTP Status Code = " + response.HttpStatusCode.ToString());
          response.WriteResponseStreamToFile(newFilePath);
          response.Dispose();
          Logger.Debug("AWS S3 Download Complete");
        }
        catch (Exception ex)
        {
          Logger.ErrorFormat("FSDowloadFile. Ошибка: {0}", ex.Message);
          return string.Empty;
        }
        if (File.Exists(newFilePath))
          return newFilePath;
        else
          return string.Empty;
      }
      else
        return string.Empty;
    }

  }
}