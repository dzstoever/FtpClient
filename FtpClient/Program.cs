using System;
using System.IO;
using System.IO.Compression;

using System.Net;

namespace ZipUnzip
{
    

    class Program
    {
        static void Main(string[] args)
        {
            //var zipFiles = Directory.GetFiles(@"W:\ZAPS15G", "*.zip");
            //foreach(var zipFilePath in zipFiles)
            //{
            //    string extractPath = zipFilePath.TrimEnd(new char[]{'.','z','i','p'});            
            //    using (ZipArchive archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Read))
            //        archive.ExtractToDirectory(extractPath);
            //}


            /* Create Object Instance */
            FtpClient ftpClient = new FtpClient(@"ftp://ftp.csi-international.com/", "dan", "smokey2517");

            ///* Upload a File */
            //ftpClient.Upload("Employees/dan/test.txt", @"C:\Users\dzstoever\Desktop\test.txt");

            ///* Download a File */
            //ftpClient.Download("Employees/dan/test.txt", @"C:\Users\dzstoever\Desktop\test2.txt");

            ///* Rename a File */
            //ftpClient.Rename("Employees/dan/test.txt", "test2.txt");

            ///* Delete a File */
            //ftpClient.Delete("Employees/dan/test2.txt");

            

            ///* Create a New Directory */
            //ftpClient.CreateDirectory("Employees/dan/testdir");

            /* Get the Date/Time a File was Created */
            string fileDateTime = ftpClient.GetFileCreationTime("Employees/dan/FtpUsers.xls");
            Console.WriteLine(fileDateTime);

            /* Get the Size of a File */
            string fileSize = ftpClient.GetFileSize("Employees/dan/FtpUsers.xls");
            Console.WriteLine(fileSize);

            /* Get Contents of a Directory (Names Only) */
            string[] simpleDirectoryListing = ftpClient.GetDirectoryList("/Operations");
            for (int i = 0; i < simpleDirectoryListing.Length; i++) { Console.WriteLine(simpleDirectoryListing[i]); }

            /* Get Contents of a Directory with Detailed File/Directory Info */
            string[] detailDirectoryListing = ftpClient.GetDirectoryListDetails("/Operations");
            for (int i = 0; i < detailDirectoryListing.Length; i++) 
            { Console.WriteLine(detailDirectoryListing[i]); }
            
            /* Release Resources */
            ftpClient = null;

        }
    }

    

    class FtpClient
    {
        const int bufferSize = 2048;

        string _host = null;
        string _user = null;
        string _pass = null;        
        FtpWebRequest _ftpRequest = null;
        FtpWebResponse _ftpResponse = null;
        //Stream _ftpStream = null;
        
        
        public FtpClient(string hostIP, string userName, string password) { _host = hostIP; _user = userName; _pass = password; }


        public void Upload(string remoteFile, string localFile)
        {
            try
            {
                /* Create an FTP Request */
                _ftpRequest = (FtpWebRequest)FtpWebRequest.Create(_host + "/" + remoteFile);
                /* Log in to the FTP Server with the User Name and Password Provided */
                _ftpRequest.Credentials = new NetworkCredential(_user, _pass);
                /* When in doubt, use these options */
                _ftpRequest.UseBinary = true;
                _ftpRequest.UsePassive = true;
                _ftpRequest.KeepAlive = true;
                /* Specify the Type of FTP Request */
                _ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;
                /* Establish Return Communication with the FTP Server */
                using (var ftpStream = _ftpRequest.GetRequestStream())
                {
                    /* Open a File Stream to Read the File for Upload */
                    FileStream localFileStream = new FileStream(localFile, FileMode.Create);
                    /* Buffer for the Downloaded Data */
                    byte[] byteBuffer = new byte[bufferSize];
                    int bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);
                    /* Upload the File by Sending the Buffered Data Until the Transfer is Complete */
                    try
                    {
                        while (bytesSent != 0)
                        {
                            ftpStream.Write(byteBuffer, 0, bytesSent);
                            bytesSent = localFileStream.Read(byteBuffer, 0, bufferSize);
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    /* Resource Cleanup */
                    localFileStream.Close();
                }//_ftpStream.Close();
                _ftpRequest = null;
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            return;
        }

        public void Download(string remoteFile, string localFile)
        {
            var ftpRequest = BuildFtpRequest(_host + "/" + remoteFile,
                    WebRequestMethods.Ftp.DownloadFile);
                
                var ftpResponse = (FtpWebResponse)_ftpRequest.GetResponse();                
                using (var ftpStream = _ftpResponse.GetResponseStream())
                {
                    /* Open a File Stream to Write the Downloaded File */
                    FileStream localFileStream = new FileStream(localFile, FileMode.Create);
                    /* Buffer for the Downloaded Data */
                    byte[] byteBuffer = new byte[bufferSize];
                    int bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
                    /* Download the File by Writing the Buffered Data Until the Transfer is Complete */
                    try
                    {
                        while (bytesRead > 0)
                        {
                            localFileStream.Write(byteBuffer, 0, bytesRead);
                            bytesRead = ftpStream.Read(byteBuffer, 0, bufferSize);
                        }
                    }
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    /* Resource Cleanup */
                    localFileStream.Close();
                }// _ftpStream.Close();
                _ftpResponse.Close();
                _ftpRequest = null;
            
        }
        
        public void Delete(string remoteFile)
        {
             var ftpRequest = BuildFtpRequest(_host + "/" + remoteFile,
                    WebRequestMethods.Ftp.DeleteFile);

            var ftpResponse = (FtpWebResponse)_ftpRequest.GetResponse();
            ftpResponse.Close();
            ftpResponse.Dispose();
            ftpRequest = null;            
        }

        public void Rename(string currentFileNameAndPath, string newFileName)
        {
            var ftpRequest = BuildFtpRequest(_host + "/" + currentFileNameAndPath,
                    WebRequestMethods.Ftp.Rename);               
            ftpRequest.RenameTo = newFileName;
               
            var ftpResponse = (FtpWebResponse)_ftpRequest.GetResponse();
            ftpResponse.Close();
            ftpResponse.Dispose();
            ftpRequest = null;            
        }
        
        public void CreateDirectory(string newDirectory)
        {
            var ftpRequest = BuildFtpRequest(_host + "/" + newDirectory,
                    WebRequestMethods.Ftp.MakeDirectory);
            
            var ftpResponse = (FtpWebResponse)_ftpRequest.GetResponse();
            ftpResponse.Close();
            ftpResponse.Dispose();
            ftpRequest = null;                        
        }


        public string GetFileCreationTime(string fileName)
        {
            string fileInfo = null;
            var ftpRequest = BuildFtpRequest(_host + "/" + fileName,
                    WebRequestMethods.Ftp.GetDateTimestamp);
            // Read the Full Response Stream    
            using (var ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
            using (var ftpStream = _ftpResponse.GetResponseStream())
            using (var ftpReader = new StreamReader(ftpStream))
            { fileInfo = ftpReader.ReadToEnd(); }
                
            ftpRequest = null;
            return fileInfo;            
        }

        public string GetFileSize(string fileName)
        {
            string fileInfo = null;
            var ftpRequest = BuildFtpRequest(_host + "/" + fileName,
                    WebRequestMethods.Ftp.GetFileSize);
            // Read the Full Response Stream    
            using (var ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
            using (var ftpStream = _ftpResponse.GetResponseStream())
            using (var ftpReader = new StreamReader(ftpStream))
            {
                while (ftpReader.Peek() != -1)
                    fileInfo = ftpReader.ReadToEnd();
            }
            _ftpRequest = null;
            return fileInfo;            
        }

        public string[] GetDirectoryList(string directory)
        {// File/Folder Name Only
            string directoryRaw = null;
            var ftpRequest = BuildFtpRequest(_host + "/" + directory,
                    WebRequestMethods.Ftp.ListDirectory);
            // Read Each Line of the Response and Append a Pipe                 
            using (var ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
            using (var ftpStream = _ftpResponse.GetResponseStream())
            using (var ftpReader = new StreamReader(ftpStream))
            {
                while (ftpReader.Peek() != -1)
                { directoryRaw += ftpReader.ReadLine() + "|"; }
            }
            ftpRequest = null;
            return directoryRaw.Split("|".ToCharArray());
        }
        
        public string[] GetDirectoryListDetails(string directory)
        {// Name, Size, Created, etc.
            string directoryRaw = null;
            var ftpRequest = BuildFtpRequest(_host + "/" + directory,
                    WebRequestMethods.Ftp.ListDirectoryDetails);            
            // Read Each Line of the Response and Append a Pipe                 
            using (var ftpResponse = (FtpWebResponse)ftpRequest.GetResponse())
            using (var ftpStream = _ftpResponse.GetResponseStream())
            using (var ftpReader = new StreamReader(ftpStream))
            {
                while (ftpReader.Peek() != -1)
                { directoryRaw += ftpReader.ReadLine() + "|"; }
            }
            ftpRequest = null;
            return directoryRaw.Split("|".ToCharArray()); 
        }


        private FtpWebRequest BuildFtpRequest(string uri, string ftpCommand)
        {
            //var ftpRequest = (FtpWebRequest)WebRequest.Create(uri);
            var ftpRequest = (FtpWebRequest)FtpWebRequest.Create(uri);
            ftpRequest.Credentials = new NetworkCredential(_user, _pass);
            ftpRequest.UseBinary = true;
            ftpRequest.UsePassive = true;
            ftpRequest.KeepAlive = true;
            ftpRequest.Method = ftpCommand;
            return ftpRequest;
        }

    } 
}
