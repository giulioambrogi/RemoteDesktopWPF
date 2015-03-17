
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HookerClient
{
    public class AmbrUtils
    {

        public static string ZIP_FILE_PATH = @"C:/tmp/cb/cbfiles.zip"; //temporary zip file received  (really it's never saved on disk)
        public static string ZIP_EXTRACTED_FOLDER = @"C:/tmp/cb/cbfiles/"; //folder in  wich extract received zip
        public static string CB_FILES_DIRECTORY_PATH = @"C:/tmp/CBFILES/"; // folder in wich i copy all file in cb droplist to be zipped and sent
        public static string ZIP_FILE_NAME_AND_PATH = @"C:/tmp/CBFILES.zip";//temp zip created by me, to be sent to the other part

        public static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public static Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            if (byteArrayContainsZipFile(arrBytes))
            {
                return extractZIPtoFolder(arrBytes);
            }
            if (byteArrayContainsBitmap(arrBytes))
            {
                return byteArrayToBitmap(arrBytes);
            }
            Console.WriteLine("Ricevuto bytearray : [" + Encoding.Default.GetString(arrBytes) + "]");
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }

        public static BitmapImage byteArrayToBitmap(byte[] arrBytes)
        {
            using (var ms = new System.IO.MemoryStream(arrBytes))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad; // here
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }
        public static byte[] bitmapSourceToByteArray(BitmapSource bms)
        {
            MemoryStream memStream = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bms));
            encoder.Save(memStream);
            return memStream.GetBuffer();
        }

        private static bool byteArrayContainsBitmap(byte[] arrBytes)
        {
            if (arrBytes[0] == 255 && arrBytes[1] == 216 && arrBytes[2] == 255 && arrBytes[3] == 224)
            {
                return true;
            }
            return false;
        }
        private static bool byteArrayContainsZipFile(byte[] arrBytes)
        {
            if (arrBytes[0] == 80 && arrBytes[1] == 75 && arrBytes[2] == 3 && arrBytes[3] == 4)
            {
                return true;
            }
            return false;
        }



        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name));
                //FileInfoFactory.Create(file.FullName).CopyTo(target.FullName + file.Name);

        }

        public static void showPopUpMEssage(String message, int millis)
        {
            Window w = new Window();
            w.Content = message;
            w.Foreground = Brushes.White;
            w.Background = Brushes.Red;
            w.WindowStyle = WindowStyle.None;
            w.SizeToContent = SizeToContent.WidthAndHeight ;// Automatically resize height and width relative to content
            w.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            w.VerticalAlignment = VerticalAlignment.Center;
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Show(); //show the windows
            Thread.Sleep(millis);

        }

        public static System.Collections.Specialized.StringCollection getFileNames(string p)
        {
            string[] filenames = Directory.GetFiles(p);
            System.Collections.Specialized.StringCollection sc = new System.Collections.Specialized.StringCollection();
            foreach (string s in filenames)
            {
                sc.Add(System.IO.Path.GetFullPath(s));
            }
            return sc;
        }


        public static byte[]  audioSourceToByteArray(Stream audioStream)
        {
            return streamToByteArray(audioStream);
        }

        public static byte[] streamToByteArray(Stream sourceStream){
            using(var memoryStream = new MemoryStream())
            {
              sourceStream.CopyTo(memoryStream);
              return memoryStream.ToArray();
            }
        }

        #region nonGeneriMethods

        public static Object extractZIPtoFolder(byte[] arrBytes)
        {
            using (Stream ms = new MemoryStream(arrBytes))
            {
                Console.WriteLine("Lunghezza del buffer : " + arrBytes.Length);
                Console.WriteLine("Lunghezza dello stream : " + ms.Length);
                ZipArchive archive = new ZipArchive(ms);
                if (Directory.Exists(ZIP_EXTRACTED_FOLDER))
                {
                    Directory.Delete(ZIP_EXTRACTED_FOLDER, true);
                    Console.WriteLine("Cancello Vecchia cartella zip");
                }
                archive.ExtractToDirectory(ZIP_EXTRACTED_FOLDER);
                return archive;
            }
        }
        #endregion

    }
}
