using CSMailslotServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowsInput;

namespace HookerServer
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal const string keyboardMailslotName = @"\\.\mailslot\keyboardMailslot";
        NativeMethods.SafeMailslotHandle hMailslot = null;

        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine(System.Environment.MachineName);

        }


        void initKeyboardMailslot()
        {
            

            try
            {
                NativeMethods.SECURITY_ATTRIBUTES sa = null;
                sa = CreateMailslotSecurity();

                // Create the mailslot.
                hMailslot = NativeMethods.CreateMailslot(
                    keyboardMailslotName,               // The name of the mailslot
                    0,                          // No maximum message size
                    NativeMethods.MAILSLOT_WAIT_FOREVER,      // Waits forever for a message
                    sa                          // Mailslot security attributes
                    );

                if (hMailslot.IsInvalid)
                {
                    throw new Win32Exception();
                }

                Console.WriteLine("The mailslot ({0}) is created.",  keyboardMailslotName);

                // Check messages in the mailslot.
                //Console.Write("Press ENTER to check new messages or press Q to quit ...");
                //string cmd = Console.ReadLine();
                //while (!cmd.Equals("Q", StringComparison.OrdinalIgnoreCase)
                int count = 0;
                while (true)
                {
                    count++;
                    if (!ReadMailslot(hMailslot))
                    {
                        Console.WriteLine("Mailslot chiusa");
                        break;
                    }
                    Thread.Sleep(10);
                    //Console.Write("Press ENTER to check new messages or press Q to quit ...");
                    //cmd = Console.ReadLine();
                }
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine("The server throws the error: {0}", ex.Message);
            }
            finally
            {
                /*
                if (hMailslot != null)
                {
                    hMailslot.Close();
                    hMailslot = null;
                }*/
            }
        }
         NativeMethods.SECURITY_ATTRIBUTES CreateMailslotSecurity()
        {
            // Define the SDDL for the security descriptor.
            string sddl = "D:" +        // Discretionary ACL
                "(A;OICI;GRGW;;;AU)" +  // Allow read/write to authenticated users
                "(A;OICI;GA;;;BA)";     // Allow full control to administrators

            NativeMethods.SafeLocalMemHandle pSecurityDescriptor = null;
            if (!NativeMethods.ConvertStringSecurityDescriptorToSecurityDescriptor(
                sddl, 1, out pSecurityDescriptor, IntPtr.Zero))
            {
                throw new Win32Exception();
            }

            NativeMethods.SECURITY_ATTRIBUTES sa = new NativeMethods.SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);
            sa.lpSecurityDescriptor = pSecurityDescriptor;
            sa.bInheritHandle = false;
            return sa;
        }


        /// <summary>
        /// Read the messages from a mailslot by using the mailslot handle in a call 
        /// to the ReadFile function. 
        /// </summary>
        /// <param name="hMailslot">The handle of the mailslot</param>
        /// <returns> 
        /// If the function succeeds, the return value is true.
        /// </returns>
        bool ReadMailslot(NativeMethods.SafeMailslotHandle hMailslot)
        {
            try
            {
                int cbMessageBytes = 0;         // Size of the message in bytes
                int cbBytesRead = 0;            // Number of bytes read from the mailslot
                int cMessages = 0;              // Number of messages in the slot
                int nMessageId = 0;             // Message ID

                bool succeeded = false;

                // Check for the number of messages in the mailslot.
                succeeded = NativeMethods.GetMailslotInfo(
                    hMailslot,                  // Handle of the mailslot
                    IntPtr.Zero,                // No maximum message size 
                    out cbMessageBytes,         // Size of next message 
                    out cMessages,              // Number of messages 
                    IntPtr.Zero                 // No read time-out
                    );
                if (!succeeded)
                {
                    Console.WriteLine("GetMailslotInfo failed w/err 0x{0:X}",
                        Marshal.GetLastWin32Error());
                    return succeeded;
                }

                if (cbMessageBytes == NativeMethods.MAILSLOT_NO_MESSAGE)
                {
                    // There are no new messages in the mailslot at present
                    //Console.WriteLine("No new messages.");
                    return succeeded;
                }

                // Retrieve the messages one by one from the mailslot.
                while (cMessages != 0)
                {
                    nMessageId++;

                    // Declare a byte array to fetch the data
                    byte[] bBuffer = new byte[cbMessageBytes];
                    succeeded = NativeMethods.ReadFile(
                        hMailslot,              // Handle of mailslot
                        bBuffer,                // Buffer to receive data
                        cbMessageBytes,         // Size of buffer in bytes
                        out cbBytesRead,        // Number of bytes read from mailslot
                        IntPtr.Zero             // Not overlapped I/O
                        );
                    if (!succeeded)
                    {
                        Console.WriteLine("ReadFile failed w/err 0x{0:X}",
                            Marshal.GetLastWin32Error());
                        break;
                    }

                    // Display the message. 

                    Console.WriteLine("Message #{0}: {1}", nMessageId,
                        Encoding.Unicode.GetString(bBuffer));

                    // Add item to listbox

                    string stringa = "Message " + nMessageId + " : " + Encoding.Unicode.GetString(bBuffer);

                    parseMessage(Encoding.Unicode.GetString(bBuffer));

                    lbMessages.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        lbMessages.Items.Add(stringa);
                        lbMessages.SelectedIndex = lbMessages.Items.Count -1;
                        
                        lbMessages.ScrollIntoView(lbMessages.SelectedIndex);
                        lbMessages.UpdateLayout();
                    }));



                    // Get the current number of un-read messages in the slot. The number
                    // may not equal the initial message number because new messages may 
                    // arrive while we are reading the items in the slot.
                    succeeded = NativeMethods.GetMailslotInfo(
                        hMailslot,              // Handle of the mailslot
                        IntPtr.Zero,            // No maximum message size 
                        out cbMessageBytes,     // Size of next message 
                        out cMessages,          // Number of messages 
                        IntPtr.Zero             // No read time-out 
                        );
                    if (!succeeded)
                    {
                        Console.WriteLine("GetMailslotInfo failed w/err 0x{0:X}",
                            Marshal.GetLastWin32Error());
                        break;
                    }
                    return succeeded;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Errore in fase di lettura ,forse è stata chiusa");
                return false;
            }
            return true;
        }

  

        private void parseMessage(string buffer)
        {
            //TO-DO : OTTIMIZZARE PRESTAZIONI

            List<string> commands = buffer.Split(' ').ToList();
            if (commands.ElementAt(0).Equals("M"))
            {
                //RAMO DEL MOUSE   
                string[] points = buffer.Split(' ');
                PointX.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    PointX.Text = points[1];
                }));
                PointY.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    PointY.Text = points[2];
                }));
            }
            else if (commands.ElementAt(0).ToString().Equals("K"))
            {
                //RAMO DELLA TASTIERA
                VirtualKeyCode vk = (VirtualKeyCode)Convert.ToInt32(commands.ElementAt(1).ToString());
                if (commands.ElementAt(2).ToString().Equals("DOWN"))
                {
                    //evento key down
                    Console.WriteLine(commands.ElementAt(1) + " DOWN");

                    //InputSimulator.SimulateKeyDown(vk);
                }
                else if (commands.ElementAt(2).ToString().Equals("UP"))
                {
                    //evento key up
                    Console.WriteLine(commands.ElementAt(0) + " UP");
                    //InputSimulator.SimulateKeyUp(vk);
                }
            }

           

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                initKeyboardMailslot();
                //this.IsEnabled = false; 
                //btnStart.IsEnabled = false;
                //btnClose.IsEnabled = true;

            }).Start();
             
        }

      

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //VirtualKeyCode k = VirtualKeyCode.UP; 
            //InputSimulator.SimulateKeyDown(k);
   
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (hMailslot != null)
            {
                if(hMailslot.IsClosed)
                    hMailslot.Close();
                hMailslot = null;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            lbMessages.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                lbMessages.Items.Clear();
            }));
        }

        private void ExitButton(object sender, RoutedEventArgs e)
        {
            if (hMailslot != null)
            {
                if (hMailslot.IsClosed)
                    hMailslot.Close();
                hMailslot = null;
            }

            this.Close();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            switch (this.WindowState)
            {
                case WindowState.Minimized:
                    this.ShowInTaskbar = false;
                    break;
            }
        }

        // metodo temporaneo
        private void WindowResume(object sender, EventArgs e)
        {
            if (this.ShowInTaskbar == false)
            {
                this.ShowInTaskbar = true;
            }
        }
    }
}
