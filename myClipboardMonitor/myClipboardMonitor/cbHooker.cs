using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace myClipboardMonitor
{
    class cbHooker
    {
        IntPtr viewerHandle = IntPtr.Zero; 
        IntPtr installedHandle = IntPtr.Zero;

        const int WM_DRAWCLIPBOARD = 0x308; 
        const int WM_CHANGECBCHAIN = 0x30D;

        [DllImport("user32.dll")] 
        private extern static IntPtr SetClipboardViewer(IntPtr hWnd);

        [DllImport("user32.dll")] 
        private extern static int ChangeClipboardChain(IntPtr hWnd, IntPtr hWndNext);

        [DllImport("user32.dll", CharSet = CharSet.Auto)] 
        private extern static int SendMessage(IntPtr hWnd,int wMsg,IntPtr wParam,IntPtr lParam);
    }
}
