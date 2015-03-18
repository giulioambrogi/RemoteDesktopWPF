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
using System.Net.NetworkInformation;

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
        private ServerManager serverManger;
        private LayoutManager layout;

        public MainWindow()
        {
            Console.WriteLine("Screen resolution : "+(int)System.Windows.SystemParameters.PrimaryScreenWidth+" "+(int)System.Windows.SystemParameters.FullPrimaryScreenHeight);
            InitializeComponent();
            //TODO eliminare definitivamente le checkbox relative a mouse e tastiera
            this.serverManger = new ServerManager();
            this.layout = new LayoutManager();
            btnContinue.IsEnabled = false; //deve per forza essere inattivo all'inizio
            btnConnect.IsEnabled = false;
            new Thread(() =>{
                Thread.CurrentThread.IsBackground = true;
                getAvailableServers();
            }).Start();
        }

        #region Core
        public void closeCommunication(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_E + " " + "UP");
            this.ConnectionChecker.Abort();
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands(); //rimuovo vincoli su hotkeys
            this.serverManger.disconnect();
            

            lblMessages.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { lblMessages.Content = ""; })); //aggiorno la label 
            lvComputers.IsEnabled = true;
            btnRefreshServers.IsEnabled = true;
            btnConnect.IsEnabled = true;
            btnContinue.IsEnabled = false;
            btnExit.IsEnabled = true;
        }

        private void pauseCommunication(object sender , ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_P + " " + "UP");
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands();
            refreshGUIOnPause();

        }

        private void continueCommunication()
        {
            InstallMouseAndKeyboard();
            bindHotkeyCommands();
            refreshGUIOnContinue();
        }

        public void closeOnException(String s)
        {
            //MessageBox.Show(s);
            UnistallMouseAndKeyboard();
            unbindHotkeyCommands(); //rimuovo vincoli su hotkeys
            this.serverManger.disconnect();
            refreshGUIonClosing();
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void gimmeClipboard(object sender, ExecutedRoutedEventArgs e)
        {
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_Z + " " + "UP");
            this.serverManger.sendMessage("G"); //this is the gimme message
        }

        private void switchToNextServer(object sender, ExecutedRoutedEventArgs e)
        {
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_N + " " + "UP");
            //chiamo il metodo che realmente switcha il puntatore al sender udp 
            this.serverManger.nextSelectedServers();
            //aggiorno la label in base ai risultati effettivi dell'operazione
            lblMessages.Dispatcher.Invoke(DispatcherPriority.Background,
             new Action(() => { lblMessages.Content = "Connesso al server : " + this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer).name; }));

        }
        public void sendClipboard(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_X + " " + "UP");
            this.serverManger.sendClipBoardFaster(this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer).CBClient);
        }

        public void getAvailableServers()
        {
            this.serverManger.availableServers.Clear();
            this.serverManger.selectedServers.Clear();
            int index = 1;
            NetworkBrowser nw = new NetworkBrowser();
            foreach (String computerName in nw.getNetworkComputers())
            {
                if (computerName != System.Environment.MachineName) ///*&& computer.Name != System.Environment.MachineName*
                {
                    Console.WriteLine("Found new computer : " + computerName);
                    processFoundComputer(computerName, index);
                    index++;


                }

            }
            /*
            foreach (DirectoryEntry computers in root.Children)
            {
                foreach (DirectoryEntry computer in computers.Children)
                {
                   
                    if (computer.Name != "Schema" ) ///*&& computer.Name != System.Environment.MachineName*
                    {
                        Console.WriteLine("Found new computer : " + computer.Name);
                        processFoundComputer(computer.Name, index);
                        index++;
                                           

                    }
                }
            }
           */
        }

        private void processFoundComputer(string computerName, int index)
        {
            //aggiungo il server alla lista dei server disponibili
            //costruisco server Entity
            ServerEntity se = new ServerEntity(computerName);
            se.setId(index);
            this.serverManger.availableServers.Add(se);


            this.lvComputers.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {


                Label l = new Label { Content = computerName };
                layout.setComputerNameLabelLayout(l);

                //create textbox for password
                TextBox tbPwd = new TextBox() { Name = "k" + index }; //t for the passwords
                tbPwd.TextChanged += tbPassword_Changed;
                layout.setPasswordTextBoxLayout(tbPwd);

                //create port field
                TextBox tbPort = new TextBox() { Name = "p" + index }; //p for the ports
                tbPort.TextChanged += tbPort_Changed;
                layout.setPortTextBoxLayout(tbPort);

                //create button checkin button
                String cbname = "cb" + index;
                CheckBox cbox = new CheckBox { Name = cbname };
                cbox.Checked += checkbox_Checked; //ADD EVENT 
                cbox.Unchecked += checkbox_Unchecked;
                layout.setCheckBoxLayout(cbox);
                //add elements to the window
                this.lvComputers.Items.Add(new MyListViewItem(l, tbPwd, tbPort, cbox));

            }));
        }

        
        
        #endregion
      
        #region Events
      

        private void checkbox_Checked(object sender, RoutedEventArgs e)
        {
            enableConnectButton();
            CheckBox cb = (CheckBox)sender;
            String rem = cb.Name.Replace("cb", "");
            int index = Convert.ToInt32(rem);
            Console.WriteLine("Attivazione della cb " + cb.Name);
            ServerEntity selectedServer = this.serverManger.getServerById(index);

            this.serverManger.selectedServers.Add(selectedServer);
        }

       

        private void checkbox_Unchecked(object sender, RoutedEventArgs e)
        {
            tryDisableConnectButton();
            CheckBox cb = (CheckBox)sender;
            String rem = cb.Name.Replace("cb", "");
            int index = Convert.ToInt32(rem);
            Console.WriteLine("Disattivazione della cb " + cb.Name);
            this.serverManger.selectedServers.Remove(this.serverManger.getServerById(index));
        }

        private void tbPassword_Changed(object sender, TextChangedEventArgs e)
        {
            int id = Convert.ToInt32(((TextBox)sender).Name.Replace("k", ""));
            ServerEntity se = this.serverManger.getServerById(id);
            se.setPassword(((TextBox)sender).Text);
            Console.WriteLine("Modifica password per " + se.name);
        }


        private void tbPort_Changed(object sender, TextChangedEventArgs e)
        {
            int id = Convert.ToInt32(((TextBox)sender).Name.Replace("p", ""));
            ServerEntity se = this.serverManger.getServerById(id);
            se.setPortFromString(((TextBox)sender).Text);
            Console.WriteLine("Modifica porta ["+((TextBox)sender).Text+"] per " + se.name);
        }


        private void enableConnectButton()
        {
            if (btnConnect.IsEnabled == false)
                btnConnect.IsEnabled = true;
        }
        private void tryDisableConnectButton()
        {
            foreach (CheckBox tb in FindVisualChildren<CheckBox>(lvComputers))
            {
                if (tb.IsChecked==true)
                {
                    return;
                }
            }
            //if i'm here, no cb is setted then change connect button status
            btnConnect.IsEnabled = false;
        }
       
        #endregion

        #region Buttons
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Window connWindow = new Window() { Background = Brushes.Red, Foreground = Brushes.White, Width = 200, Height = 100, Content = "In connessione...", WindowStyle = WindowStyle.None, WindowStartupLocation = WindowStartupLocation.CenterScreen };
            connWindow.Show();
            Thread t = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                //this.serverManger.connectToServer(this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer), "TODO");
                this.serverManger.connect();
            });
            t.Start();
            t.Join(); //aspetto che il thread delle connesioni termini

            bool allConnected = true;
            foreach (ServerEntity s in this.serverManger.selectedServers)
            {
                if (s.server == null)
                {
                    //almeno un server non è connesso 
                    this.serverManger.disconnect();
                    allConnected = false;
                    connWindow.Close();
                    MessageBox.Show("Non sono riuscito a connettermi a " + s.name);
                    refreshGUIonClosing();
                }
            }
            if (allConnected != false)
            {
                //se le connessioni sono andate a buon fine
                InstallMouseAndKeyboard();
                //Questo bind vale solo mentre si è connessi
                bindHotkeyCommands();
                connWindow.Close();
                refreshGUIonConnection();
                runConnectionChecker();
                

            }
        }

        private void runConnectionChecker()
        {
            this.ConnectionChecker = new Thread(() =>
            {
                while (true)
                {
                    // Detect if client disconnected
                    try
                    {
                        bool bClosed = false;
                        foreach (ServerEntity se in this.serverManger.selectedServers)
                        {
                            if (se.server == null) //questo ciclo serve a non testare connessione nel caso la pasword fosse sbagliata ( password sbagliata-> chiude server e lo setta a null)
                            {
                                closeOnException("Password sbagliata");
                                MessageBox.Show("La connessione è stata interrotta");
                                break;
                            }
                            if (se.server.Client.Poll(0, SelectMode.SelectRead))
                            {
                                byte[] buff = new byte[1];
                                if (se.server.Client.Receive(buff, SocketFlags.Peek) == 0)
                                {
                                    // Client disconnected
                                    bClosed = true;
                                    MessageBox.Show("La connessione è stata interrotta");
                                    closeOnException("Connessione interrotta");
                                    return;
                                }
                                else
                                {
                                    Console.WriteLine("BZZZZ");
                                }
                            }

                            /*
                            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
                            TcpConnectionInformation[] tcpConnections = ipProperties.GetActiveTcpConnections().Where(x => x.RemoteEndPoint.Equals(se.server.Client.RemoteEndPoint)).ToArray();
                                
                            if (tcpConnections != null && tcpConnections.Length > 0)
                            {
                                if (tcpConnections.First().State.Equals(TcpState.Established))
                                {

                                }
                                else
                                {
                                    // Client disconnected
                                    bClosed = true;
                                    MessageBox.Show("La connessione è stata interrotta");
                                    closeOnException("Connessione interrotta");
                                    return;
                                }
                            }
                            else
                            {
                                // Client disconnected
                                bClosed = true;
                                MessageBox.Show("La connessione è stata interrotta");
                                closeOnException("Connessione interrotta");
                                return;
                            }
                            */


                        }

                        Thread.Sleep(2000);
                    }
                    catch (SocketException se)
                    {
                        closeOnException(se.Message);
                        //MessageBox.Show("La connessione è stata interrotta");
                        break;
                    }
                }
            }
            );
            this.ConnectionChecker.Start();
        }

        private void btnRefreshServers_Click(object sender, RoutedEventArgs e)
        {
            //TODO: CANCELLARE LA GRID
            lvComputers.Items.Clear();
            getAvailableServers();
        }
        private void btnExit_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

            this.btnExit.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (((Button)sender).IsEnabled == true)
                {
                    btnExit.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/exit.png", UriKind.Relative)) };
                }
                else
                {
                    btnExit.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/exit_disabled.png", UriKind.Relative)) };
                }
            }));
        }

        private void btnContinue_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.btnConnect.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (((Button)sender).IsEnabled == true)
                {
                    btnContinue.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/continue.png", UriKind.Relative)) };
                }
                else
                {
                    btnContinue.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/continue_disabled.png", UriKind.Relative)) };
                }
            }));
        }

        private void btnConnect_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.btnConnect.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (((Button)sender).IsEnabled == true)
                {
                    btnConnect.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/remote.png", UriKind.Relative)) };
                }
                else
                {
                    btnConnect.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/remote_disabled.png", UriKind.Relative)) };
                }
            }));

        }

        private void btnRefresh_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.btnConnect.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (((Button)sender).IsEnabled == true)
                {
                    btnRefreshServers.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/refresh.png", UriKind.Relative)) };
                }
                else
                {
                    btnRefreshServers.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(@"icons/refresh_disabled.png", UriKind.Relative)) };
                }
            }));
        }

        private void btnHelp_Click(object sender, RoutedEventArgs e)
        {
            String filepath = @"help.txt";
            Window w = new Window();
            if (System.IO.File.Exists(filepath))
            {
                w.Title = "Help";
               
                string[] content = System.IO.File.ReadAllLines(filepath, Encoding.ASCII);
                foreach (string s in content)
                {
                    w.Content += s+"\n";
                }
                //w.Content = content ;
                w.Foreground = Brushes.White;
                w.Background = Brushes.Red;
                w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                w.Content = "Helper file not found";
            }
            w.Show();
                
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if (this.ConnectionChecker != null && this.ConnectionChecker.IsAlive)
                this.ConnectionChecker.Abort();
            Application.Current.Shutdown();
        }
        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            continueCommunication();
        }

        private void btnClipboardMonitor_Click(object sender, RoutedEventArgs e)
        {
            this.serverManger.sendClipBoardFaster(null);
        }
#endregion

        #region Hooks
        //TODO: passare un'oggetto al server in modo che questo possa eseguire azione
        void keyboardHook_KeyPress(int op, RamGecTools.KeyboardHook.VKeys key)
        {
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
                closeOnException(ex.Message);
                MessageBox.Show("La connessione si è interrotta");

            }
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
            switch (type)
            {
                case 0:  //mouse click
                    this.serverManger.sendMessage("C" + " " + move.ToString());

                    break;
                case 1: // Mouse movement
                    double x = Math.Round((mouse.pt.x / System.Windows.SystemParameters.PrimaryScreenWidth), 4); //must send relative position REAL/RESOLUTION
                    double y = Math.Round((mouse.pt.y / System.Windows.SystemParameters.PrimaryScreenHeight), 4);

                    this.serverManger.sendMessage("M" + " " + x.ToString() + " " + y.ToString());
                    break;
                default:
                    break;
            }
        }

        private void MouseWheelEventHandler(object sender, MouseWheelEventArgs e)
        {
            this.serverManger.sendMessage("W" + " " + ((int)e.Delta / 120).ToString());
        }
        public KeyEventHandler wnd_KeyDown { get; set; }

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
            this.MouseWheel += MouseWheelEventHandler;
        }

        private void UnistallMouseAndKeyboard()
        {
            keyboardHook.KeyPress -= new RamGecTools.KeyboardHook.myKeyboardHookCallback(keyboardHook_KeyPress);
            mouseHook.MouseEvent -= new RamGecTools.MouseHook.myMouseHookCallback(mouseHook_MouseEvent);
            keyboardHook.Uninstall();
            mouseHook.Uninstall();
            this.MouseWheel += MouseWheelEventHandler;

        }


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
                //aggancio CTRL+ALT+N per next server
                RoutedCommand nextServer = new RoutedCommand();
                nextServer.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(nextServer, switchToNextServer));
                //aggancio CTRL+ALT+X per inviare mia clipboard
                RoutedCommand sendClipboardcmd = new RoutedCommand();
                sendClipboardcmd.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(sendClipboardcmd, sendClipboard));
                //aggancio CTRL+ALT+Z per ricevere la clipboard dal server
                RoutedCommand gimmeClipboardcmd = new RoutedCommand();
                gimmeClipboardcmd.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt));
                CommandBindings.Add(new CommandBinding(gimmeClipboardcmd, gimmeClipboard));

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
                CommandBindings.Clear();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot unbind : " + ex.Message);
            }
        }

        #endregion

        #region graphic interface refreshing methods

        public void refreshGUIonConnection()
        {
            //aggiorno i pulsanti
            btnConnect.IsEnabled = false;
            btnRefreshServers.IsEnabled = false;
            btnExit.IsEnabled = false;
            lvComputers.IsEnabled = false;
            String serverName = "";
            if (this.serverManger.selectedServers.Count > 0)
                serverName = this.serverManger.selectedServers.ElementAt(this.serverManger.serverPointer).name;
            lblMessages.Dispatcher.Invoke(DispatcherPriority.Background,
               new Action(() => { lblMessages.Content = "Connesso al server : " + serverName; }));
            btnHelp.Dispatcher.Invoke(DispatcherPriority.Background,
             new Action(() => { btnHelp.IsEnabled = false; }));
        }
        public void refreshGUIonClosing()
        {
            btnRefreshServers.Dispatcher.Invoke(DispatcherPriority.Background,
                new Action(() => { btnRefreshServers.IsEnabled = true; }));
            btnConnect.Dispatcher.Invoke(DispatcherPriority.Background,
             new Action(() => { btnConnect.IsEnabled = true; }));
            btnContinue.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnContinue.IsEnabled = false; }));
            btnExit.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnExit.IsEnabled = true; }));
            lvComputers.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { lvComputers.IsEnabled = true; }));
            btnHelp.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnHelp.IsEnabled = true; }));
            lblMessages.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { lblMessages.Content = ""; }));
            btnHelp.Dispatcher.Invoke(DispatcherPriority.Background,
          new Action(() => { btnHelp.IsEnabled = true; }));
        }
        private void refreshGUIOnPause()
        {
            btnRefreshServers.Dispatcher.Invoke(DispatcherPriority.Background,
                new Action(() => { btnRefreshServers.IsEnabled = false; }));
            btnConnect.Dispatcher.Invoke(DispatcherPriority.Background,
             new Action(() => { btnConnect.IsEnabled = false; }));
            btnContinue.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnContinue.IsEnabled = true; }));
            btnExit.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnExit.IsEnabled = false; }));
            btnHelp.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnHelp.IsEnabled = false; }));
            lblMessages.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { lblMessages.IsEnabled = false; }));

        }

        private void refreshGUIOnContinue()
        {
            btnRefreshServers.Dispatcher.Invoke(DispatcherPriority.Background,
                new Action(() => { btnRefreshServers.IsEnabled = false; }));
            btnConnect.Dispatcher.Invoke(DispatcherPriority.Background,
             new Action(() => { btnConnect.IsEnabled = false; }));
            btnContinue.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnContinue.IsEnabled = false; }));
            btnExit.Dispatcher.Invoke(DispatcherPriority.Background,
            new Action(() => { btnExit.IsEnabled = false; }));

        }
        #endregion

    }

   


}
