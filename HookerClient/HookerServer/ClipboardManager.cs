using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HookerServer
{
    class ClipboardManager
    {
        Object content;
        string ZIP_FILE_PATH = @"./cb/cb.zip";
        string ZIP_EXTRACTED_FOLDER = @"./cb/cbfiles/";
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
                     Clipboard.SetText((String)this.content);
                }
                else if (t == typeof(ZipArchive))
                {
                    UnzipArchive();
                    System.Collections.Specialized.StringCollection files = getFileNames(@"./ExtractedFiles");
                    Clipboard.SetFileDropList(files);
                }
                else if (t == typeof(BitmapSource))
                {
                    Clipboard.SetImage((BitmapSource)content);
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
                sc.Add(s);
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
    }
}
