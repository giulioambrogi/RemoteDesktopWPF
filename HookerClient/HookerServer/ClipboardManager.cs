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
        public string ZIP_FILE_PATH = @"./cb/cbfiles.zip";
        public string ZIP_EXTRACTED_FOLDER = @"./cb/cbfiles/";
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
                    System.Collections.Specialized.StringCollection files = getFileNames(ZIP_EXTRACTED_FOLDER+@"/CBFILES/");
                    Clipboard.Clear();
                    Clipboard.SetFileDropList(files);
                    foreach (String file in files)
                        Console.WriteLine("Ho aggiunto in CB : " + file);
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
    }
}
