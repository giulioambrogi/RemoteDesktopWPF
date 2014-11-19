using CSMailslotClient;
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
using System.DirectoryServices;
using System.Windows.Threading;
using System.IO;


namespace HookerClient
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RamGecTools.MouseHook mouseHook = new RamGecTools.MouseHook();
        RamGecTools.KeyboardHook keyboardHook = new RamGecTools.KeyboardHook();
        
       //METTENDO L'ASTERISCO MANDO IL MESSAGGIO A TUTTE LE MAISLOT CON QUEL NOME
        //String keyboardMailslotName = @"\\*\mailslot\keyboardMailslot";
        
        //Questa lista di mailslot è la lista dei nomi della mailslot costantemente aggiornata ad ogni cambiamento di  selezione su listbox
        public  List<String> mailslotNames = new List<String>();
        //op
        //Questa lista di handler verrà popolata in fase di connessione
        private List<NativeMethods.SafeMailslotHandle> mailslotHandlers = new List<NativeMethods.SafeMailslotHandle>();

        public MainWindow()
        {
            InitializeComponent();
            InstallMouseAndKeyboard();
            //TODO eliminare definitivamente le checkbox relative a mouse e tastiera
    
            btnContinue.IsEnabled = false; //deve per forza essere inattivo all'inizio
            new Thread(() =>
            {

                Thread.CurrentThread.IsBackground = true;
                populateComputerList();
              
            }).Start();

        }

        public void populateComputerList()
        {

            List<String> computerList = new List<string>();
            DirectoryEntry root = new DirectoryEntry("WinNT:");
            foreach (DirectoryEntry computers in root.Children)
            {
                foreach (DirectoryEntry computer in computers.Children)
                {
                    if (computer.Name != "Schema" && computer.Name != System.Environment.MachineName)
                    {
                        Console.WriteLine("Found new computer : "+computer.Name);
                        lbServers.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            lbServers.Items.Add(computer.Name);
                        }));

                    }
                }
            }
            return; 
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



        public void initSelectedMailslots()
        {
            foreach (String mailslotname in mailslotNames)
            {
                try
                {
                    // Try to open the mailslot with the write access.
                    
                    NativeMethods.SafeMailslotHandle hMailslot = NativeMethods.CreateFile(
                        mailslotname,                           // The name of the mailslot
                        NativeMethods.FileDesiredAccess.GENERIC_WRITE,        // Write access 
                        NativeMethods.FileShareMode.FILE_SHARE_READ,          // Share mode
                        IntPtr.Zero,                            // Default security attributes
                        NativeMethods.FileCreationDisposition.OPEN_EXISTING,  // Opens existing mailslot
                        0,                                      // No other attributes set
                        IntPtr.Zero                             // No template file
                        );

                    NativeMethods.SECURITY_ATTRIBUTES sa = null;
                   
                    if (hMailslot.IsInvalid)
                    {
                        throw new Win32Exception();
                    }
                    mailslotHandlers.Add(hMailslot); //aggiungo handler alla lista
                    Console.WriteLine("The mailslot is opened {0}", mailslotname);

                }
                catch (Win32Exception ex)
                {
                    Console.WriteLine("The client throws the error: {0}", ex.Message);
                }
                finally
                {
                    /* if (hMailslot != null)
                     {
                         hMailslot.Close();
                         hMailslot = null;
                     }*/
                }
            }
        }
     

        //metodo wrapper che permette di scrivere a tutte le mailslot selezionate
        public void WriteSelectedMailslots(string message)
        {
            //TODO : scrivere a tutte le mailslot (gestire sia handle che 
            foreach (NativeMethods.SafeMailslotHandle h in mailslotHandlers)
            {
                WriteMailslot(h, message);
                
            }
        }
        private int WriteMailslot(NativeMethods.SafeMailslotHandle hMailslot, string message)
        {
            int cbMessageBytes = 0;         // Message size in bytes
            int cbBytesWritten = 0;         // Number of bytes written to the slot

            byte[] bMessage = Encoding.Unicode.GetBytes(message);
            cbMessageBytes = bMessage.Length;

            bool succeeded = NativeMethods.WriteFile(
                hMailslot,                  // Handle to the mailslot
                bMessage,                   // Message to be written
                cbMessageBytes,             // Number of bytes to write
                out cbBytesWritten,         // Number of bytes written
                IntPtr.Zero                 // Not overlapped
                );
            if (!succeeded || cbMessageBytes != cbBytesWritten)
            {
                Console.WriteLine("WriteFile failed w/err 0x{0:X}",
                    Marshal.GetLastWin32Error());
                return -1;
            }
            else
            {
                Console.WriteLine("The message \"{0}\" is written to the slot",
                    message);
                return 0;
            }
        }

       

      
      
        //TODO: passare un'oggetto al server in modo che questo possa eseguire azione
        void keyboardHook_KeyPress(int op,RamGecTools.KeyboardHook.VKeys key ){
            if(op == 0){
                //key is down
                WriteSelectedMailslots("K" + " " + (int)key + " " + "DOWN");
                
            }
            else{
                //key is up
                WriteSelectedMailslots("K" + " " + (int)key + " " + "UP");
            }
        }


        /*
         Questo metodo è stato creato per il seguente motivo: il keyboard hooker fa in modo che gli hotkeys tipo alt-tab, non vadano al sistema operativo 
         * del client: in pratica è come se l'hooker si "mangiasse" gli eventi key_down, di conseguenza bisogna
         * generarli "artificialmente" in questo modo, per far sì che il server li riceva
         */
        void keyboardHook_HotKeyPress(int virtualKeyCode)
        {
            WriteSelectedMailslots("K" + " " + (int)virtualKeyCode + " " + "DOWN");
        }

        void mouseHook_MouseEvent(int type, RamGecTools.MouseHook.MSLLHOOKSTRUCT mouse, RamGecTools.MouseHook.MouseMessages move)
        {
            switch(type)
            {
                case 0: // Mouse click
                    WriteSelectedMailslots( move.ToString());
                    break;
                case 1: // Mouse movement
                    int x = mouse.pt.x;
                    int y = mouse.pt.y;
                    WriteSelectedMailslots( "M" + " " + x.ToString() + " " + y.ToString());
                    break;
                default:
                    break;
            }
        }

        private void InstallMouseAndKeyboard()
        {
            //Insatllo keyboard 
            keyboardHook.KeyPress += new RamGecTools.KeyboardHook.myKeyboardHookCallback(keyboardHook_KeyPress);
            //Questo qui sotto era un vecchio handler che usavo per i problemi degli shortcut, momentaneamente lascio commentato
            //keyboardHook.HotKeyPress += new RamGecTools.KeyboardHook.myKeyboardHotkeyCallback(keyboardHook_HotKeyPress);
            keyboardHook.Install();  
            //Installo Mouse
            mouseHook.MouseEvent += new RamGecTools.MouseHook.myMouseHookCallback(mouseHook_MouseEvent);
            mouseHook.Install();
        }
       

        private void lbServers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mailslotNames.Clear(); //pulisco la lista dei nomi delle maislot, per ricalcolarla

            foreach (var x in lbServers.SelectedItems)
            {
                mailslotNames.Add(@"\\"+x.ToString()+@"\mailslot\keyboardMailslot");
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                initSelectedMailslots();
            }).Start();
            //Questo bind vale solo mentre si è connessi
            bindHotkeyCommands();
            //aggiorno i pulsanti
            btnConnect.IsEnabled = false;
            btnRefreshServers.IsEnabled = false;
            btnExit.IsEnabled = false;
            lbServers.IsEnabled = false;
            //scrivo messaggio per l'utente
            tbStatus.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                tbStatus.Text = "Il Client è attivo, ricordati di attivare il Server sulla/e macchina/e selezionata/e!\n"
                                +"Per mettere in pausa la connessione premi CTRL-ALT-P\n"
                                +"Per terminare la connessione premi CTRL-ALT-E";
            }));
        }

        public void closeCommunication(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            WriteSelectedMailslots("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            WriteSelectedMailslots("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            WriteSelectedMailslots("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_E + " " + "UP");
            foreach (NativeMethods.SafeMailslotHandle h in mailslotHandlers)
            {
                h.Close();
            }
            mailslotHandlers.Clear(); //pulisco la lista degli handlers
            unbindHotkeyCommands(); //rimuovo vincoli su hotkeys
            
            btnRefreshServers.IsEnabled = true;
            lbServers.IsEnabled = true; 
            btnConnect.IsEnabled = true;
            btnContinue.IsEnabled = false;
            btnExit.IsEnabled = true;
            tbStatus.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                tbStatus.Text = "Seleziona uno o più server dalla lista e connettiti!";
            }));
        }

        private void pauseCommunication(object sender , ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            WriteSelectedMailslots("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            WriteSelectedMailslots("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            WriteSelectedMailslots("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_P + " " + "UP");
            /*
            foreach (NativeMethods.SafeMailslotHandle h in mailslotHandlers)
            {
                h.Close();
            }*/
            mailslotHandlers.Clear(); //pulisco la lista degli handlers
            unbindHotkeyCommands(); //rimuovo i vincoli sugli hotkeys
            btnContinue.IsEnabled = true; 
            
            btnRefreshServers.IsEnabled = false;
            lbServers.IsEnabled = false;
            btnConnect.IsEnabled = false;
            btnExit.IsEnabled = true;
            tbStatus.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                tbStatus.Text = "La comunicazione è in PAUSA, premi CONTINUA per riattivarla!";
            }));
        }

        private void continueCommunication()
        {
            initSelectedMailslots();
            bindHotkeyCommands();
            btnContinue.IsEnabled = false;
            tbStatus.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                tbStatus.Text = "Il Client è attivo, ricordati di attivare il Server sulla/e macchina/e selezionata/e!\n"
                               + "Per mettere in pausa la connessione premi CTRL-ALT-P\n"
                               + "Per terminare la connessione premi CTRL-ALT-E";
            }));
        }

        private void btnRefreshServers_Click(object sender, RoutedEventArgs e)
        {
            lbServers.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                lbServers.Items.Clear();
            }));
            populateComputerList();
        }



        public KeyEventHandler wnd_KeyDown { get; set; }

        /*
         * metodo che serve ad intercettare alcuni tasti come:
         * ALT+F4 : non deve terminare il Client
         * tasto windows
         * Solamente la combinazione scelta (ctrl+alt+E) serve ad interrompere la comunicazione e tornare alla finestra principale
        */

        private void bindHotkeyCommands()
        {
            try
            {
                //aggancio CTRL+ALT+P con pauseCommunication
                RoutedCommand pauseComm = new RoutedCommand();
                pauseComm.InputGestures.Add(new KeyGesture(Key.P, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(pauseComm, pauseCommunication));
                //aggancio CTRL+ALT+E con closeCommunication
                RoutedCommand closeComm = new RoutedCommand();
                closeComm.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(closeComm, closeCommunication));

            }
            catch (Exception e)
            {
                //MessageBox.Show("bindHotKeyCommands: " + e.Message);
                Application.Current.Shutdown();
            }
        }

        private void unbindHotkeyCommands()
        {
            try
            {
                //Al momento li rimuovo tutti, in futuro forse è meglio rimuoverne solo alcuni
                CommandBindings.Clear();
            }
            catch (Exception e)
            {
                //MessageBox.Show("unbindHotKeyCommands: "+e.Message);
                Application.Current.Shutdown();
            }
        }
    
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            continueCommunication();
        }
    }

   


}
