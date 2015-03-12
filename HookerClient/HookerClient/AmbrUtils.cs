using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace HookerClient
{
    class AmbrUtils
    {

        private static string ZIP_FILE_PATH = @"./cb/cbfiles.zip";
        private static string ZIP_EXTRACTED_FOLDER = @"./cb/cbfiles/";

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
