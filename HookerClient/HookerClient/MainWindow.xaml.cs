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
using System.Net.Sockets;


namespace HookerClient
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        RamGecTools.MouseHook mouseHook = new RamGecTools.MouseHook();
        RamGecTools.KeyboardHook keyboardHook = new RamGecTools.KeyboardHook();
        
        public Thread ConnectionChecker;
        //Questa lista di mailslot è la lista dei nomi della mailslot costantemente aggiornata ad ogni cambiamento di  selezione su listbox
        public  List<String> mailslotNames = new List<String>();
        //op
        //Questa lista di handler verrà popolata in fase di connessione o
        private List<NativeMethods.SafeMailslotHandle> mailslotHandlers = new List<NativeMethods.SafeMailslotHandle>();
        private ServerManager serverManger;
        public MainWindow()
        {
            Console.WriteLine("Screen resolution :" + (int)System.Windows.SystemParameters.PrimaryScreenWidth + " " + (int)System.Windows.SystemParameters.FullPrimaryScreenHeight);
            InitializeComponent();
            //TODO eliminare definitivamente le checkbox relative a mouse e tastiera
            this.serverManger = new ServerManager();
            btnContinue.IsEnabled = false; //deve per forza essere inattivo all'inizio
            new Thread(() =>
            {

                Thread.CurrentThread.IsBackground = true;
                getAvailableServers();
              
            }).Start();

        }

        public void getAvailableServers()
        {
            this.serverManger.availableServers.Clear();
            List<ServerEntity> servers = new List<ServerEntity>();
            DirectoryEntry root = new DirectoryEntry("WinNT:");
            foreach (DirectoryEntry computers in root.Children)
            {
                foreach (DirectoryEntry computer in computers.Children)
                {
                    if (computer.Name != "Schema" /*&& computer.Name != System.Environment.MachineName*/)
                    {
                        Console.WriteLine("Found new computer : " + computer.Name);
                        //aggiungo il server alla lista dei server disponibili
                        this.serverManger.availableServers.Add(new ServerEntity(computer.Name));
                        lbServers.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            lbServers.Items.Add(computer.Name);
                        }));

                    }
                }
            }
        }



        //TODO: passare un'oggetto al server in modo che questo possa eseguire azione
        void keyboardHook_KeyPress(int op,RamGecTools.KeyboardHook.VKeys key ){
            try
            {
                if (op == 0)
                {
                    //key is down
                    this.serverManger.sendMessage("K" + " " + (int)key + " " + "DOWN");

                }
                else
                {
                    //key is up
                    this.serverManger.sendMessage("K" + " " + (int)key + " " + "UP");
                }
            }
            catch (Exception ex)
            {
                closeOnException();
                MessageBox.Show("La connessione si è interrotta");
                
            }
        }

        public void closeOnException()
        {
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands(); //rimuovo vincoli su hotkeys
            this.serverManger.disconnect();
            refreshGUIonClosing();
        }


        /*
         Questo metodo è stato creato per il seguente motivo: il keyboard hooker fa in modo che gli hotkeys tipo alt-tab, non vadano al sistema operativo 
         * del client: in pratica è come se l'hooker si "mangiasse" gli eventi key_down, di conseguenza bisogna
         * generarli "artificialmente" in questo modo, per far sì che il server li riceva
         */
        void keyboardHook_HotKeyPress(int virtualKeyCode)
        {
            this.serverManger.sendMessage("K" + " " + (int)virtualKeyCode + " " + "DOWN");
        }

        void mouseHook_MouseEvent(int type, RamGecTools.MouseHook.MSLLHOOKSTRUCT mouse, RamGecTools.MouseHook.MouseMessages move)
        {
            switch(type)
            {
                case 0: // Mouse click
                    this.serverManger.sendMessage(move.ToString());
                    break;
                case 1: // Mouse movement
                    double x = Math.Round((mouse.pt.x / System.Windows.SystemParameters.PrimaryScreenWidth),4); //must send relative position REAL/RESOLUTION
                    double y = Math.Round((mouse.pt.y / System.Windows.SystemParameters.PrimaryScreenHeight),4) ;

                    this.serverManger.sendMessage("M" + " " + x.ToString() + " " + y.ToString());
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

        private void UnistallMouseAndKeyboard()
        {
            keyboardHook.KeyPress -= new RamGecTools.KeyboardHook.myKeyboardHookCallback(keyboardHook_KeyPress);
            mouseHook.MouseEvent -= new RamGecTools.MouseHook.myMouseHookCallback(mouseHook_MouseEvent);
            keyboardHook.Uninstall();
            mouseHook.Uninstall();
            
        }

        private void lbServers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.serverManger.selectedServers.Clear();
            foreach (var x in lbServers.SelectedItems)
            {
                //trova il server a cui si riferisce
                ServerEntity se = this.serverManger.availableServers.Find(item => item.name.Equals(x.ToString()));
                this.serverManger.selectedServers.Add(se);
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Thread t = new Thread(() =>
            {
                    Thread.CurrentThread.IsBackground = true;
                    //this.serverManger.connectToServer(this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer), "TODO");
                    this.serverManger.connect();
            });
            t.Start();
            t.Join();

            bool allConnected = true;
            foreach(ServerEntity s in this.serverManger.selectedServers){
                if(s.server!= null && !s.server.Connected){
                    //almeno un server non è connesso 
                    allConnected = false;
                    MessageBox.Show("Non sono riuscito a connettermi a "+s.name);
                    refreshGUIonClosing();
                }
            }
            if (allConnected != false)
            {
                //se le connessioni sono andate a buon fine
                InstallMouseAndKeyboard();
                //Questo bind vale solo mentre si è connessi
                bindHotkeyCommands();
                refreshGUIonConnection();
                this.ConnectionChecker = new Thread(() =>
                {
                    while (true)
                    {
                        // Detect if client disconnected
                        try
                        {
                            bool bClosed = false;
                            if (this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer).server.Client.Poll(0, SelectMode.SelectRead))
                            {
                                byte[] buff = new byte[1];
                                if (this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer).server.Client.Receive(buff, SocketFlags.Peek) == 0)
                                {
                                    // Client disconnected
                                    bClosed = true;
                                    MessageBox.Show("La connessione è stata interrotta");
                                    closeOnException();
                                    break;
                                }
                            }

                            Thread.Sleep(2000);
                        }
                        catch (SocketException se)
                        {
                            closeOnException();
                            MessageBox.Show("La connessione è stata interrotta");
                            break;
                        }
                    }
                }
            );
                this.ConnectionChecker.Start();

            }
        }

        public void refreshGUIonConnection()
        {
            //aggiorno i pulsanti
            btnConnect.IsEnabled = false;
            btnRefreshServers.IsEnabled = false;
            btnExit.IsEnabled = false;
            lbServers.IsEnabled = false;
            //scrivo messaggio per l'utente
            tbStatus.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                tbStatus.Text = "Il Client è attivo, ricordati di attivare il Server sulla/e macchina/e selezionata/e!\n"
                                + "Per mettere in pausa la connessione premi CTRL-ALT-P\n"
                                + "Per terminare la connessione premi CTRL-ALT-E";
            }));
        }
        public void refreshGUIonClosing()
        {
            btnRefreshServers.Dispatcher.Invoke(DispatcherPriority.Background , 
                new Action(()=>{btnRefreshServers.IsEnabled = true; }));
            lbServers.Dispatcher.Invoke(DispatcherPriority.Background,
               new Action(() => { lbServers.IsEnabled = true; }));
            btnConnect.Dispatcher.Invoke(DispatcherPriority.Background,
             new Action(() => { btnConnect.IsEnabled = true; }));
            btnContinue.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnContinue.IsEnabled = false; }));
            btnExit.Dispatcher.Invoke(DispatcherPriority.Background,
           new Action(() => { btnExit.IsEnabled = true; }));
            tbStatus.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                tbStatus.Text = "Seleziona uno o più server dalla lista e connettiti!";
            }));
        }

        public void closeCommunication(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_E + " " + "UP");
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands(); //rimuovo vincoli su hotkeys
            this.serverManger.disconnect();

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
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_P + " " + "UP");
            /*
            foreach (NativeMethods.SafeMailslotHandle h in mailslotHandlers)
            {
                h.Close();
            }*/
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
        /*
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
        */
        private void btnRefreshServers_Click(object sender, RoutedEventArgs e)
        {
            lbServers.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                lbServers.Items.Clear();
            }));
            getAvailableServers();
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
          //TODO
        }
    
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            //continueCommunication();
            MessageBox.Show("Non fa nulla al momento");
        }
    }

   


}
