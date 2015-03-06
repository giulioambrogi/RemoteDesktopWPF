using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace HookerClient
{
    class ClipboardMgmt
    {
        #region Private fields
        private IntPtr hWndNextViewer;
        private HwndSource hWndSource;
        public bool isViewing;
        #endregion

        


        public void InitCBViewer(System.Windows.Window window) 
        {
            WindowInteropHelper wih = new WindowInteropHelper(window);
            hWndSource = HwndSource.FromHwnd(wih.Handle);
            hWndSource.AddHook(this.WinProc);   // start processing window messages
            hWndNextViewer = Win32.SetClipboardViewer(hWndSource.Handle);   // set this window as a viewer
            isViewing = true;
        }

        public void CloseCBViewer()
        {
            // remove this window from the clipboard viewer chain
            Win32.ChangeClipboardChain(hWndSource.Handle, hWndNextViewer);
            hWndNextViewer = IntPtr.Zero;
            hWndSource.RemoveHook(this.WinProc);
            isViewing = false;
        }
        

        #region operazioni per hooking
        public void DrawContent()
        {
           

            if (Clipboard.ContainsText())
            {
                String content = Clipboard.GetText();
                Console.WriteLine("Cb TEXT : "+content);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                // we have a file drop list in the clipboard
             
                foreach(String f in Clipboard.GetFileDropList()){
                    Console.WriteLine("CB FILEDROPLIST : "+f);
                }
            }
            else if (Clipboard.ContainsImage())
            {
                Console.WriteLine("CB IMMAGINE!!!!!");
               /*
                // Because of a known issue in WPF,
                // we have to use a workaround to get correct
                // image that can be displayed.
                // The image have to be saved to a stream and then 
                // read out to workaround the issue.
                MemoryStream ms = new MemoryStream();
                BmpBitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(Clipboard.GetImage()));
                enc.Save(ms);
                ms.Seek(0, SeekOrigin.Begin);

                BmpBitmapDecoder dec = new BmpBitmapDecoder(ms,
                    BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

                Image img = new Image();
                img.Stretch = Stretch.Uniform;
                img.Source = dec.Frames[0];
                pnlContent.Children.Add(img);*/
            }
            else
            {
                Console.WriteLine("CB FORMATO NON SUPPORTATO");
            }
        }

        public  IntPtr WinProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //Console.WriteLine(">>> "+hwnd + " " + msg + " " + wParam + " " + lParam + " " + handled);
            switch (msg)
            {
                /*
                case Win32.WM_CHANGECBCHAIN:
                    if (wParam == hWndNextViewer)
                    {
                        // clipboard viewer chain changed, need to fix it.
                        Console.WriteLine("CHANGECBCHAIN IF 1");
                        hWndNextViewer = lParam;
                    }
                    else if (hWndNextViewer != IntPtr.Zero)
                    {
                        Console.WriteLine("CHANGECBCHAIN IF 2");
                        // pass the message to the next viewer.
                        Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    }
                    break;
                */
                case Win32.WM_DRAWCLIPBOARD:
                    Console.WriteLine("DRAWNCLIPBOARD IF 0 [hwnd: "+hwnd+"][mesg: "+msg+"][wparam "+wParam+"][lparam "+lParam+"][handled "+handled+"]");
                    // clipboard content changed
                    this.DrawContent();
                    // pass the message to the next viewer.
                    Win32.SendMessage(hWndNextViewer, msg, wParam, lParam);
                    break;
            }

            return IntPtr.Zero;
        }

        #endregion

        internal void setData(string format, string content)
        {
            Clipboard.Clear();
            Clipboard.SetData(format, content);
        }
    }
}
