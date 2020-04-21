using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;

namespace Com.MobileSolutions
{
    class Server
    {
        public int contador = 0;

        public void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            //GetListLocal();
            List<FileName> sourceFileList = new List<FileName>();
            List<FileName> targetFileList = new List<FileName>();

            string targetURI = ConfigurationManager.AppSettings["Destination"];
            string user = ConfigurationManager.AppSettings["User"];
            string pass = ConfigurationManager.AppSettings["Pass"];
            string sourceURI = ConfigurationManager.AppSettings["Server"];

            getFileLists(sourceURI, user, pass, sourceFileList, targetURI,  targetFileList);

            CheckLists(sourceFileList, targetFileList);

            targetFileList.Sort();
            foreach (var file in sourceFileList)
            {
                try
                {
                    if (contador < 5)
                    {
                        Thread th = new Thread(() => CopyFile(file.fName, sourceURI, user, pass, targetURI));
                        th.Start();
                        contador++;   
                    } else if(contador < 10)
                    {

                    }
                }
                catch
                {
                    Console.WriteLine("There was move error with : " + file.fName);
                }
            }

        }

        public class FileName : IComparable<FileName>
        {
            public string fName { get; set; }
            public long Size { get; set; }
            public int CompareTo(FileName other)
            {
                return fName.CompareTo(other.fName);
            }
        }
        /// <summary>
        /// Se compara los archivos de cada carpeta para no repertir archivos 
        /// </summary>
        /// <param name="sourceFileList"></param>
        /// <param name="targetFileList"></param>
        public static void CheckLists(List<FileName> sourceFileList, List<FileName> targetFileList)
        {
            for (int i = 0; i < sourceFileList.Count; i++)
            {
                if (targetFileList.BinarySearch(sourceFileList[i]) > 0)
                {
                    sourceFileList.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceURI"></param>
        /// <param name="User"></param>
        /// <param name="pass"></param>
        /// <param name="sourceFileList"></param>
        /// <param name="targetURI"></param>
        /// <param name="targetUser"></param>
        /// <param name="targetPass"></param>
        /// <param name="targetFileList"></param>
        public static void getFileLists(string sourceURI, string user, string pass, List<FileName> sourceFileList, string targetURI,  List<FileName> targetFileList)
        {
            string line = "";
            FtpWebRequest sourceRequest;
            sourceRequest = (FtpWebRequest)WebRequest.Create(sourceURI);
            sourceRequest.Credentials = new NetworkCredential(user, pass);
            sourceRequest.Method = WebRequestMethods.Ftp.ListDirectory;

            sourceRequest.UseBinary = true;
            sourceRequest.KeepAlive = false;
            sourceRequest.Timeout = -1;
            sourceRequest.UsePassive = true;
            FtpWebResponse sourceRespone = (FtpWebResponse)sourceRequest.GetResponse();
            //Se crer una lista (fileList) de cada nombre de archivo
            using (Stream responseStream = sourceRespone.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    line = reader.ReadLine();
                    while (line != null)
                    {
                        var SizeFile = GetFileSize(sourceURI, line, user, pass);
                        //byte mybyt = Convert.ToByte(SizeFile);
                        var fileName = new FileName
                        {
                            fName = line,
                            Size = SizeFile
                    };
                        sourceFileList.Add(fileName);
                        Console.WriteLine(fileName.Size);

                        line = reader.ReadLine();
                    }
                }
            }
            /////////////Target FileList
            FtpWebRequest targetRequest;
            targetRequest = (FtpWebRequest)WebRequest.Create(targetURI);
            targetRequest.Credentials = new NetworkCredential(user, pass);
            targetRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            targetRequest.UseBinary = true;
            targetRequest.KeepAlive = false;
            targetRequest.Timeout = -1;
            targetRequest.UsePassive = true;
            FtpWebResponse targetResponse = (FtpWebResponse)targetRequest.GetResponse();
            //Creates a list(fileList) of the file names
            using (Stream responseStream = targetResponse.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(responseStream))
                {

                    line = reader.ReadLine();
                    while (line != null)
                    {
                        var fileName = new FileName
                        {
                            fName = line
                        };
                        targetFileList.Add(fileName);
                        line = reader.ReadLine();
                    }
                }
            }
        }

        public void CopyFile(string fileName, string sourceURI, string user, string pass, string targetURI)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(sourceURI + fileName);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(user, pass);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                DownloadFileLocal(fileName, sourceURI, user, pass);

                if (Upload(fileName, ToByteArray(responseStream), targetURI, user, pass))
                {
                    DeleteFile(fileName, sourceURI, user, pass);
                }

                responseStream.Close();
            }
            catch
            {
                Console.WriteLine("There was an error with :" + fileName);
            }
        }
        public static void DownloadFileLocal(string fileName, string sourceURI, string user, string pass)
        {
            try
            {
                string localPath = ConfigurationManager.AppSettings["Local"];
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(sourceURI + fileName);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(user, pass);
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream responseStream = response.GetResponseStream();
                FileStream writeStream = new FileStream(localPath + fileName, FileMode.Create);

                int Length = 2048;
                Byte[] buffer = new Byte[Length];
                int bytesRead = responseStream.Read(buffer, 0, Length);

                while (bytesRead > 0)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                    bytesRead = responseStream.Read(buffer, 0, Length);
                }

                responseStream.Close();
                writeStream.Close();
                request = null;
                response = null;

            }
            catch
            {
                Console.WriteLine("There was an error with :" + fileName);
            }
        }

        public static Byte[] ToByteArray(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            byte[] chunk = new byte[4096];
            int bytesRead;
            while ((bytesRead = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                ms.Write(chunk, 0, bytesRead);
            }

            return ms.ToArray();
        }

        public static bool Upload(string FileName, byte[] Image, string targetURI, string user, string pass)
        {
            try
            {
                FtpWebRequest clsRequest = (FtpWebRequest)WebRequest.Create(targetURI + FileName);
                clsRequest.Credentials = new NetworkCredential(user, pass);
                clsRequest.Method = WebRequestMethods.Ftp.UploadFile;
                Stream clsStream = clsRequest.GetRequestStream();
                clsStream.Write(Image, 0, Image.Length);
                clsStream.Close();
                clsStream.Dispose();
                //Console.WriteLine("Carga correcta");

                return true;
            }
            catch
            {
                Console.WriteLine("Catch upload" + FileName);

                return false;
            }
        }

        public void DeleteFile(string FileName, string sourceURI, string user, string pass)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(sourceURI + FileName);
                request.Method = WebRequestMethods.Ftp.DeleteFile;
                request.Credentials = new NetworkCredential(user, pass);

                FtpWebResponse responseFileDelete = (FtpWebResponse)request.GetResponse();
                //Console.WriteLine("Delete " + FileName);
                contador--;
                Console.WriteLine("Delete Contador" + contador);

            }
            catch
            {
            }
        }
        public static void GetListLocal()
        {
            try
            {
                string localPath = ConfigurationManager.AppSettings["Local"];

                string[] files = Directory.GetFiles(localPath);

                foreach (string filepath in files)
                {
                    string fileName = Path.GetFileName(filepath);
                    Console.WriteLine(fileName);
                }
            }
            catch
            {
            }
        }

        public static long GetFileSize(string sourceURI, string FileName, string user, string pass)
        {
            try
            {
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(sourceURI + FileName);

                request.Credentials = new NetworkCredential(user, pass);
                request.Method = WebRequestMethods.Ftp.GetFileSize;

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    return response.ContentLength;

                }  
            }
            catch (Exception ex)
            {

                if (ex.Message.Contains("File unavailable")) return -1;
                throw;
            }
        }


    }
}
