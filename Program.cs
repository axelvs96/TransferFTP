using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Renci.SshNet;
using System.Net.FtpClient;
using System.Data;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using TransferFTP;

class Program
{
    static void Main(string[] args)
    {
        WriteLog("---------------------------------------------------------------");
        WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Starting execution. ");


        // Obtener conexion a la base de datos.
        string csvFile = ConfigurationManager.AppSettings["CsvConnectionFile"].ToString();
        
        try
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true
            };

            using (var reader = new StreamReader(csvFile))
            using (var csv = new CsvReader(reader, configuration))
            {
                csv.Context.RegisterClassMap<ConnectionDataMap>();
                var connections = new List<ConnectionData>();
                while(csv.Read())
                {
                    connections.Add(csv.GetRecord<ConnectionData>());


                    if(connections[0].Type.Equals("SFTP"))
                    {
                        using (SftpClient sftp = new SftpClient(connections[0].Url, connections[0].Port, connections[0].User, connections[0].Password))
                        {
                
                            try
                            {

                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Connecting to server {connections[0].Url}...");
                                sftp.Connect();
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Connection established {connections[0].Url}.");

                                if (connections[0].Mode.Equals("D"))
                                {
                                    DownloadSFTP(sftp, connections[0].Url, connections[0].RemoteDirectory, connections[0].LocalDirectory, connections[0].DeleteFile);
                                }
                                if (connections[0].Mode.Equals("U"))
                                {
                                    UploadSFTP(sftp, connections[0].Url, connections[0].RemoteDirectory, connections[0].LocalDirectory, connections[0].DeleteFile, connections[0].MoveBackupFolder);
                                }
                            }
                            catch (Exception e)
                            {
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. {connections[0].Url}.");
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. ERROR -- {e.ToString()}");
                            }
                            finally
                            {
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Disconnecting from server {connections[0].Url}...");
                                sftp.Disconnect();
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Disconnected from server {connections[0].Url}.");
                            }
                        }
                    }

                    if(connections[0].Type.Equals("FTP")){

                        using (FtpClient ftp = new FtpClient())
                        {
                            try
                            {
                                //Test Conexion
                                ftp.Host = connections[0].Url;
                                ftp.Credentials = new NetworkCredential(connections[0].User, connections[0].Password);
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Connecting to server {connections[0].Url}...");
                                ftp.Connect();
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Connection established {connections[0].Url}.");

                                if (connections[0].Mode.Equals("D"))
                                {
                                    DownloadFTP(ftp, connections[0].Url, connections[0].Port, connections[0].User, connections[0].Password, connections[0].RemoteDirectory, connections[0].LocalDirectory, connections[0].DeleteFile);
                                }

                                if (connections[0].Mode.Equals("U"))
                                {
                                    UploadFTP(connections[0].Url, connections[0].Port, connections[0].User, connections[0].Password, connections[0].RemoteDirectory, connections[0].LocalDirectory, connections[0].DeleteFile, connections[0].MoveBackupFolder);
                                }
                            }
                            catch (Exception e)
                            {
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. {connections[0].Url}.");
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. ERROR -- {e.ToString()}");
                            }
                            finally
                            {
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Disconnecting from server {connections[0].Url}...");
                                ftp.Disconnect();
                                WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Disconnected from server {connections[0].Url}.");
                            }
                
                        }

                
                    }          

                    connections.Clear();

                }

                
            }


        }
        catch (Exception ex)
        {

            WriteLog($"TransferFTP: {DateTime.Now.ToString()}. ERROR RETRIEVING DATA FROM CSV FILE: {csvFile}. {ex.ToString()}.");
            WriteLog($"TransferFTP: {DateTime.Now.ToString()}. {ex.ToString()}.");
            WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Execution ended.");
            WriteLog("---------------------------------------------------------------");
                
            Environment.Exit(0);
        }
        
        

        WriteLog($"TransferFTP: {DateTime.Now.ToString()}. Execution ended.");
        WriteLog("---------------------------------------------------------------");
    }


    static void WriteLog(string strValue)
    {
        try
        {
            // Obtener ruta del log
            string logPath = ConfigurationManager.AppSettings["LogPath"].ToString()+ "TransferFTP_" + DateTime.Now.ToString("yyyyMMdd") + ".log";
            StreamWriter sw;
            if (!File.Exists(logPath))
            { sw = File.CreateText(logPath); }
            else
            { sw = File.AppendText(logPath); }

            LogWrite(strValue, sw);

            sw.Flush();
            sw.Close();
        }
        catch (Exception ex)
        {
            ex.ToString();
        }
    }

    static void LogWrite(string logMessage, StreamWriter w)
    {
        w.WriteLine("{0}", logMessage);
    }





    static void DownloadFTP(FtpClient ftp, string ipServer, int portServer, string user, string password, string remoteDirectory, string localDirectory, bool deleteFile)
    {

        try
        {

            FtpListItem[] files = ftp.GetListing(remoteDirectory);

            int totalFiles = 0;


            foreach ( var file in files)
            {
                // Type: File = 0, Directory = 1, Link = 2
                if (file.Type==0 && file.Size > 0) totalFiles++;
            }

            WriteLog($"DownloadFTP - {ipServer}: {DateTime.Now.ToString()}. Files found in server {ipServer}  = {totalFiles}");

            if (totalFiles > 0)
            {
                WriteLog($"DownloadFTP - {ipServer}: {DateTime.Now.ToString()}. Starting download from {remoteDirectory} ...");

                foreach(var file in files)
                {

                    if (file.Type == 0 && file.Size>0)
                    {
                        string remoteFilePath = remoteDirectory + "/" + file.Name;
                        string localFilePath = Path.Combine(localDirectory, file.Name);

                        FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create($"ftp://{ipServer}:{portServer}/{remoteFilePath}");
                        downloadRequest.Credentials = new NetworkCredential(user, password);
                        downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                        using (FtpWebResponse downloadResponse = (FtpWebResponse)downloadRequest.GetResponse())
                        using (Stream ftpStream = downloadResponse.GetResponseStream())
                        using (Stream fileStream = File.Create(localFilePath))
                        {
                            WriteLog($"DownloadFTP - {ipServer}: {DateTime.Now.ToString()}. File {remoteDirectory}/{file.Name} ({file.Size} bytes) downloaded into {localDirectory}/{file.Name}");
                            ftpStream.CopyTo(fileStream);

                            if (deleteFile)
                            {
                                WriteLog($"DownloadFTP - {ipServer}: {DateTime.Now.ToString()}. Deleting file {remoteDirectory}/{file.Name} ({file.Size} bytes) ");
                                ftp.DeleteFile(remoteFilePath);
                            }
                            
                        }
                    }

                }

            }

        }
        catch (WebException e)
        {
            String status = ((FtpWebResponse)e.Response).StatusDescription;
            WriteLog($"DownloadFTP - {ipServer}: {DateTime.Now.ToString()}. ERROR STATUS -- {status}");
        }
        catch (Exception ex)
        {
            WriteLog($"DownloadFTP - {ipServer}: {DateTime.Now.ToString()}. ERROR -- {ex.ToString()}");
        }


    }

    static void UploadFTP(string ipServer, int portServer, string user, string password, string remoteDirectory, string localDirectory, bool deleteFile, bool moveBackup)
    {
        try
        {

            IEnumerable<FileSystemInfo> files = new DirectoryInfo(localDirectory).EnumerateFiles();
            files = files.Where(file => (file.Name != ".") && (file.Name != ".."));
            /* Se comenta esto por si se ponen filtros en la busqueda de archivos
             * && file.Name.EndsWith(fileextension));
             */

            int totalFiles = files.Count();

            WriteLog($"UploadFTP - {ipServer}: {DateTime.Now.ToString()}. Files found in folder {localDirectory} = {totalFiles}");

            if (totalFiles > 0)
            {

                WriteLog($"UploadFTP - {ipServer}: {DateTime.Now.ToString()}. Starting upload to {ipServer} ...");

                foreach (var file in files)
                {
                    string remoteFilePath = remoteDirectory + "/"+ file.Name;
                    FileInfo fi = new FileInfo(file.FullName);
                    long fileSize = fi.Length;

                    
                    FtpWebRequest uploadRequest = (FtpWebRequest)WebRequest.Create($"ftp://{ipServer}:{portServer}/{remoteFilePath}");
                    uploadRequest.Credentials = new NetworkCredential(user, password);
                    uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
                    using (Stream fileStream = File.OpenRead(localDirectory + "/"+file.Name))
                    using (Stream ftpStream = uploadRequest.GetRequestStream())
                    {
                        WriteLog($"UploadFTP - {ipServer}: {DateTime.Now.ToString()}. File {localDirectory}/{file.Name} ({fileSize} bytes) uploaded into {remoteDirectory}/{file.Name}.");
                        fileStream.CopyTo(ftpStream);

                        fileStream.Close();

                        if (moveBackup)
                        {
                            WriteLog($"UploadFTP - {ipServer}: {DateTime.Now.ToString()}. Moving file from {localDirectory}/{file.Name} ({fileSize} bytes) to {localDirectory}/BAK/{file.Name}.");
                            File.Move(localDirectory + "/" + file.Name, localDirectory + "/BAK/" + file.Name);
                        }

                        if (deleteFile)
                        {
                            WriteLog($"UploadFTP - {ipServer}: {DateTime.Now.ToString()}. Deleting file {localDirectory}/{file.Name} ({fileSize} bytes).");
                            File.Delete(localDirectory + "/" + file.Name);
                        }
                    }
                }

            }

            
        }
        catch(WebException e)
        {

            String status = ((FtpWebResponse)e.Response).StatusDescription;
            WriteLog($"UploadFTP - {ipServer}: {DateTime.Now.ToString()}. ERROR STATUS -- {status}");
        }
        catch(Exception ex)
        {
            WriteLog($"UploadFTP - {ipServer}: {DateTime.Now.ToString()}. ERROR -- {ex.ToString()}");
        }


    }





    static void DownloadSFTP(SftpClient sftp, string ipServer, string remoteDirectory, string localDirectory, bool deleteFile)
    {
        try
        {
            var files = sftp.ListDirectory(remoteDirectory);
            files = files.Where(file => (file.Name != ".") && (file.Name != "..") && (file.Attributes.Size > 0) && (!file.Attributes.IsDirectory));

            /* Se comenta esto por si se ponen filtros en la busqueda de archivos
             * && file.Name.EndsWith(fileextension));
             */

            int totalFiles = files.Count();
            WriteLog($"DownloadSFTP - {ipServer}: {DateTime.Now.ToString()}. Files found in server {ipServer}  = {totalFiles}");

            if (totalFiles > 0)
            {
                WriteLog($"DownloadSFTP - {ipServer}: {DateTime.Now.ToString()}. Starting download from {remoteDirectory} ...");

                foreach (var file in files)
                {
                    if (!file.Name.StartsWith(".") && !file.Attributes.IsDirectory )
                    {
                        string remoteFileName = file.Name;
                        using (Stream file1 = File.Create(localDirectory + "/" + remoteFileName))
                        {
                            long fileSize = file.Attributes.Size;

                            if (fileSize > 0)
                            {
                                sftp.DownloadFile(remoteDirectory + "/" + remoteFileName, file1);

                                WriteLog($"DownloadSFTP - {ipServer}: {DateTime.Now.ToString()}. File {remoteDirectory}/{remoteFileName} ({fileSize} bytes) downloaded into {localDirectory}/{remoteFileName}");


                                if (deleteFile)
                                {
                                    WriteLog($"DownloadSFTP - {ipServer}: {DateTime.Now.ToString()}. Deleting file {remoteDirectory}/{remoteFileName} ({fileSize} bytes) ");
                                    sftp.DeleteFile(remoteDirectory + "/" + remoteFileName);
                                }

                            }

                        }
                    }
                }

            }
        }
        catch (Exception ex)
        {
            WriteLog($"DownloadSFTP - {ipServer}: {DateTime.Now.ToString()}. ERROR -- {ex.ToString()}");
        }

    }


    static void UploadSFTP(SftpClient sftp, string ipServer, string remoteDirectory, string localDirectory, bool deleteFile, bool moveBackup)
    {
        try
        {
            IEnumerable<FileSystemInfo> files = new DirectoryInfo(localDirectory).EnumerateFiles();
            files = files.Where(file => (file.Name != ".") && (file.Name != ".."));
            /* Se comenta esto por si se ponen filtros en la busqueda de archivos
             * && file.Name.EndsWith(fileextension));
             */

            int totalFiles = files.Count();

            WriteLog($"UploadSFTP - {ipServer}: {DateTime.Now.ToString()}. Files found in folder {localDirectory} = {totalFiles}");

            if (totalFiles > 0)
            {
                WriteLog($"UploadSFTP - {ipServer}: {DateTime.Now.ToString()}. Starting upload to {ipServer} ...");

                foreach (FileSystemInfo file in files)
                {
                    if (!file.Name.StartsWith("."))
                    {
                        string localFileName = file.Name;
                        using (Stream file1 = File.OpenRead(localDirectory + "/" + localFileName))
                        {
                            FileInfo fi = new FileInfo(file.FullName);
                            long fileSize = fi.Length;

                            sftp.UploadFile(file1, remoteDirectory + "/" + localFileName);
                            file1.Close();

                            WriteLog($"UploadSFTP - {ipServer}: {DateTime.Now.ToString()}. File {localDirectory}/{localFileName} ({fileSize} bytes) uploaded into {remoteDirectory}/{localFileName}.");


                            //Mover a carpeta backup o eliminar el fichero
                            if (moveBackup)
                            {
                                WriteLog($"UploadSFTP - {ipServer}: {DateTime.Now.ToString()}. Moving file from {localDirectory}/{localFileName} ({fileSize} bytes) to {localDirectory}/BAK/{localFileName}.");
                                File.Move(localDirectory + "/" + localFileName, localDirectory + "/BAK/" + localFileName);
                            }
                            if (deleteFile)
                            {
                                WriteLog($"UploadSFTP - {ipServer}: {DateTime.Now.ToString()}. Deleting file {localDirectory}/{localFileName} ({fileSize} bytes) .");
                                File.Delete(localDirectory + "/" + localFileName);
                            }
                        }
                    }
                }


            }
        }
        catch (Exception ex)
        {
            WriteLog($"UploadSFTP - {ipServer}: {DateTime.Now.ToString()}. ERROR -- {ex.ToString()}");
        }

    }


}




