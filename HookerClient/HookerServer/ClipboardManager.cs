using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HookerServer
{
    class ClipboardManager
    {
        #region Variables
        Object content;
        string ZIP_FILE_PATH = HookerClient.AmbrUtils.ZIP_FILE_PATH; //temporary zip file received by the client
        string ZIP_EXTRACTED_FOLDER = HookerClient.AmbrUtils.ZIP_EXTRACTED_FOLDER; //folder containing the files received from the client
        string CB_FILES_DIRECTORY_PATH =HookerClient.AmbrUtils.CB_FILES_DIRECTORY_PATH; //folder that will be zipped 
        string ZIP_FILE_NAME_AND_PATH = HookerClient.AmbrUtils.ZIP_FILE_NAME_AND_PATH;//zip file to be sent to the client
        #endregion

        #region Core

        public ClipboardManager() { }
        
        public ClipboardManager(Object content)
        {
            this.content = content; 
        }

        public void setData()
        {
            Type t = this.content.GetType();
            try
            {
                if ( t== typeof(String)) //text
                {
                     Clipboard.SetText((String)this.content);
                }
                else if (t == typeof(ZipArchive)) //set of files
                {
                    //extraction  already been done
                    Clipboard.Clear();
                    System.Collections.Specialized.StringCollection files = HookerClient.AmbrUtils.getFileNames(ZIP_EXTRACTED_FOLDER + @"/CBFILES/"); //add all files to list
                    foreach (DirectoryInfo dir in new DirectoryInfo(ZIP_EXTRACTED_FOLDER + @"/CBFILES/").GetDirectories())
                    {
                        files.Add(dir.FullName);
                    }
                    if (files != null && files.Count > 0)
                    {
                        Clipboard.SetFileDropList(files);
                    }
                }
                else if (t == typeof(BitmapImage)) //image
                {
                    Clipboard.SetImage((BitmapImage)content);
                }
                else if (t == typeof(Stream)) //audio is represented as stream
                {
                    Clipboard.SetAudio((Stream)content);
                }
                Console.WriteLine("La clipboard è stata settata");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void sendClipBoardFaster(TcpClient client)
        {
            byte[] content = new byte[0]; //byte array that will contain the clipboard
            byte[] sizeInBytes = new byte[4]; //byte array that will contain the size

            if (Clipboard.ContainsText())
            {
                content= HookerClient.AmbrUtils.ObjectToByteArray(Clipboard.GetText());
            }
            else if (Clipboard.ContainsFileDropList())
            {
                //Creates a new, blank zip file to work with - the file will be
                //finalized when the using 
                if (Directory.Exists(CB_FILES_DIRECTORY_PATH))
                        Directory.Delete(CB_FILES_DIRECTORY_PATH, true);
                if (File.Exists(ZIP_FILE_NAME_AND_PATH))
                    File.Delete(ZIP_FILE_NAME_AND_PATH);
                Directory.CreateDirectory(CB_FILES_DIRECTORY_PATH);
                foreach (String filepath in Clipboard.GetFileDropList())
                {
                    FileAttributes attr = File.GetAttributes(filepath);//get attribute to know if it's a file or folder
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {//Its a directory
                        DirectoryInfo diSource = new DirectoryInfo(filepath);
                        System.IO.Directory.CreateDirectory(CB_FILES_DIRECTORY_PATH + diSource.Name);
                            
                        DirectoryInfo diDst = new DirectoryInfo(CB_FILES_DIRECTORY_PATH + diSource.Name);
                        HookerClient.AmbrUtils.CopyFilesRecursively(diSource, diDst);                  
                    }else{
                        //Its a file
                        String dstFilePath = CB_FILES_DIRECTORY_PATH+Path.GetFileName(filepath);
                        System.IO.File.Copy(filepath, dstFilePath);
                    }
                       
                }
                ZipFile.CreateFromDirectory(CB_FILES_DIRECTORY_PATH, ZIP_FILE_NAME_AND_PATH, CompressionLevel.Fastest, true);
                FileInfo info = new FileInfo(ZIP_FILE_NAME_AND_PATH);
                Console.WriteLine("Dimensione del file zip : " + info.Length +" bytes");
                if (info.Length > 1024 * 1024 * 200) //limite a 200 mega
                {
                    MessageBoxResult result = MessageBox.Show("Sei sicuro di voler trasferire " + info.Length + " bytes?");
                    if (result == MessageBoxResult.No || result == MessageBoxResult.Cancel)
                        Console.WriteLine("Can't send more than 200 Mega Bytes");
                        return;
                }
                content = File.ReadAllBytes(ZIP_FILE_NAME_AND_PATH); 
            }
            else if (Clipboard.ContainsImage())
            {
                //content = imageToByteArray(Clipboard.GetImage());
                content = HookerClient.AmbrUtils.bitmapSourceToByteArray(Clipboard.GetImage());
            }
            else if (Clipboard.ContainsAudio())
            {
                content = HookerClient.AmbrUtils.audioSourceToByteArray(Clipboard.GetAudioStream());
            }
            else
            {
                Console.WriteLine("Nothing to send");
                return;
            }
                
            NetworkStream ns = client.GetStream();
            Int32 len = content.Length;
            sizeInBytes = BitConverter.GetBytes(len); //convert size of content into byte array
            Console.WriteLine("Mando size: " + len);
            ns.Write(sizeInBytes, 0, 4); //write 
            Console.WriteLine("Mando buffer...");
            ns.Write(content, 0, content.Length);
            ns.Flush();
            Console.WriteLine("Mandato!");
        }

        #endregion
    }
}
