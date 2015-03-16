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
        Object content;
        public string ZIP_FILE_PATH = @"./cb/cbfiles.zip"; //temporary zip file received by the client
        public string ZIP_EXTRACTED_FOLDER = @"./cb/cbfiles/"; //folder containing the files received from the client
        public String CB_FILES_DIRECTORY_PATH = @"./CBFILES/"; //zip folder that will be zipped 
        public String ZIP_FILE_NAME_AND_PATH = "CBFILES.zip"; //zip file to be sent to the client
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
                if ( t== typeof(String))
                {
                    //setta il file di testo nella clipboard
                     Clipboard.SetText((String)this.content);
                }
                else if (t == typeof(ZipArchive))
                {
                    //extraction  already been done
                    Clipboard.Clear();
                    System.Collections.Specialized.StringCollection files = getFileNames(ZIP_EXTRACTED_FOLDER + @"/CBFILES/"); //add all files to list
                    foreach (DirectoryInfo dir in new DirectoryInfo(ZIP_EXTRACTED_FOLDER + @"/CBFILES/").GetDirectories())
                    {
                        files.Add(dir.FullName);
                    }
                    if (files != null && files.Count > 0)
                    {
                        Clipboard.SetFileDropList(files);
                    }
                }
                else if (t == typeof(BitmapImage))
                {
                    Clipboard.SetImage((BitmapImage)content);
                }
                else if (t == typeof(Stream))
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

        private System.Collections.Specialized.StringCollection getFileNames(string p)
        {
            string[] filenames = Directory.GetFiles(p);
            System.Collections.Specialized.StringCollection sc = new System.Collections.Specialized.StringCollection();
            foreach (string s in filenames)
            {
                sc.Add(System.IO.Path.GetFullPath(s));
            }
            return sc;
        }

        private void UnzipArchive()
        {
            string zipPath = ZIP_FILE_PATH;
            string extractPath = ZIP_EXTRACTED_FOLDER;

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    entry.ExtractToFile(System.IO.Path.Combine(extractPath, entry.FullName));
                }
            }
        }

        public void sendClipBoardFaster(TcpClient client)
        {

                byte[] content = new byte[0]; //byte array that will contain the clipboard
                byte[] sizeInBytes = new byte[4]; //byte array that will contain the size

                if (Clipboard.ContainsText())
                {
                   content= ObjectToByteArray(Clipboard.GetText());
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    //Creates a new, blank zip file to work with - the file will be
                    //finalized when the using 
                    if (Directory.Exists(CB_FILES_DIRECTORY_PATH))
                         Directory.Delete(CB_FILES_DIRECTORY_PATH, true);
                    if (File.Exists("CBFILES.zip"))
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
                            CopyFilesRecursively(diSource, diDst);                  
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
                    content = bitmapSourceToByteArray(Clipboard.GetImage());
                }
                else if (Clipboard.ContainsAudio())
                {
                    content = ObjectToByteArray(Clipboard.GetAudioStream());
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

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
        }
        // Convert an object to a byte array
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }

        public byte[] imageToByteArray(System.Windows.Media.Imaging.BitmapSource imageIn)
        {
            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageIn));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = ms.ToArray();
            }
            return data;
        }

        public byte[] bitmapSourceToByteArray(BitmapSource bms)
        {
             MemoryStream memStream = new MemoryStream();              
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bms));
            encoder.Save(memStream);
            return memStream.GetBuffer();
        }
        public BitmapImage byteArrayToBitMapImage(byte[] byteArrayIn)
        {

            MemoryStream strmImg = new MemoryStream(byteArrayIn);
            BitmapImage myBitmapImage = new BitmapImage();
            myBitmapImage.BeginInit();
            myBitmapImage.StreamSource = strmImg;
            myBitmapImage.DecodePixelWidth = 200;
            myBitmapImage.EndInit();
            return myBitmapImage;
        }

    }
}
