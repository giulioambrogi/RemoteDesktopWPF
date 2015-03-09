﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
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
        public bool isConnected = false;
        public IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        public TcpListener server = null;
        public TcpClient client = null; 
        public UdpClient udpListener; //object that gets the messages for mouse and keyboards
        public IPEndPoint remoteIPEndpoint;
        public IPEndPoint cbEndpoint; 
        Thread runThread;
        Thread ConnectionChecker;
        Thread cbListener;
        public TcpListener cbSocketServer;
        String temptext;
        public string ZIP_FILE_PATH = @"./cb/cbfiles.zip";
        public string ZIP_EXTRACTED_FOLDER = @"./cb/cbfiles/";
        TcpClient clientCB;
        Window w = new Window();
        #region Properties
        String password { get; set; }

        #endregion
        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine("Nome computer :" + System.Environment.MachineName);
            btnStart.IsEnabled = true;
            bindHotKeyCommands();
            
        }

        private void bindHotKeyCommands()
        {
            //aggancio CTRL+ALT+Z per inviare la mia clipboard al client
            RoutedCommand sendClipboardcmd = new RoutedCommand();
            sendClipboardcmd.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Alt));
            CommandBindings.Add(new CommandBinding(sendClipboardcmd, sendClipboard));
        }

        public void sendClipboard(object sender, ExecutedRoutedEventArgs e)
        {
            //Piccolo stratagemma x evitare che al server arrivino solo gli eventi KEYDOWN (che causerebberro problemi)
            //this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LCONTROL + " " + "UP");
            //this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.LMENU + " " + "UP");
            //this.serverManger.sendMessage("K" + " " + (int)RamGecTools.KeyboardHook.VKeys.KEY_X + " " + "UP");
            ClipboardManager cb = new ClipboardManager();
            cb.sendClipBoardFaster(this.clientCB);
        }
        //temporaneamente disabilitato
        private void receiveClipboard(object sender, ExecutedRoutedEventArgs e)
        {
            /* InputSimulator.SimulateKeyUp(VirtualKeyCode.CONTROL);
             InputSimulator.SimulateKeyUp(VirtualKeyCode.MENU);
             InputSimulator.SimulateKeyUp(VirtualKeyCode.VK_X);
             try
             {
                 Console.WriteLine("INTERRUPT CLIPBOARD");
                 if (isConnected)
                 {
                     byte[] msg = new byte[128];
                     this.cbSocketServer.Receive(msg);
                     String msgString = (String)ByteArrayToObject(msg);
                     Console.WriteLine(msgString);
                 }
             }
             catch (SocketException ex)
             {
                 Console.WriteLine("Non riesco a ricevere la clipboard");
             }*/
        }

        private void parseMessage(string buffer)
        {
            //TO-DO : OTTIMIZZARE PRESTAZIONI

            //List<string> commands = buffer.Split(' ').ToList();
            String[] commands = buffer.Split(' ');
            if (commands.ElementAt(0).Equals("M"))
            {
                //16 bit è più veloce di 32
                int x = Convert.ToInt16(Double.Parse(commands[1]) * System.Windows.SystemParameters.PrimaryScreenWidth);
                int y = Convert.ToInt16(Double.Parse(commands[2]) * System.Windows.SystemParameters.PrimaryScreenHeight);
                //RAMO DEL MOUSE 
                //Metodo che setta la posizione del mouse
                NativeMethods.SetCursorPos(x, y);
                
                //Console.WriteLine("Received: {0}", buffer);
            }
            else if (commands.ElementAt(0).ToString().Equals("K"))
            {
                //RAMO DELLA TASTIERA
                VirtualKeyCode vk = (VirtualKeyCode)Convert.ToInt32(commands.ElementAt(1).ToString());
                if (commands.ElementAt(2).ToString().Equals("DOWN"))
                {
                    //evento key down
                    Console.WriteLine(commands.ElementAt(1) + " DOWN");
                    InputSimulator.SimulateKeyDown(vk);
                }
                else if (commands.ElementAt(2).ToString().Equals("UP"))
                {
                    //evento key up
                    Console.WriteLine(commands.ElementAt(0) + " UP");
                    InputSimulator.SimulateKeyUp(vk);
                }

                else
                {
                    Console.WriteLine("MESSAGGIO NON CAPITO :[" + buffer + "]");
                }

            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            if (Properties.Settings.Default.Port > 65535 || Properties.Settings.Default.Port < 1500)
            {
                icon.ShowBalloonTip("Errore", "La porta deve essere compresa tra 1500 e 60000", new Hardcodet.Wpf.TaskbarNotification.BalloonIcon());
                return;
            }
            icon.ShowBalloonTip("Messaggio", "Il server è in esecuzione..", new Hardcodet.Wpf.TaskbarNotification.BalloonIcon());
            this.runThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                runServer();
            });
            this.runThread.Start();

            this.Close();
            //btnStart.IsEnabled = false;


        }


        private void runServer()
        {

            this.remoteIPEndpoint = new IPEndPoint(IPAddress.Any, Properties.Settings.Default.Port);
            this.server = new TcpListener(IPAddress.Any, Properties.Settings.Default.Port); //server which accepts the connection
            this.server.Start(1);
            initCBListener(); //init clipboard socket
            // Enter the listening loop.
            while (true)
            {
                try
                {
                    Console.WriteLine("PASSO PER IL VIA");
                    Console.WriteLine("Provo a creare il nuovo udplistener");
                    Thread.Sleep(599);
                    if (this.udpListener != null)
                        this.udpListener.Close();
                    this.udpListener = new UdpClient(Properties.Settings.Default.Port); //listener which gets the commands to be executed (keyboard and mouse)
                    Console.WriteLine("Ok creato il nuovo udplistener");
                    Byte[] bytes = new Byte[128];
                    String message = null;
                    Console.Write("Waiting for a connection... ");
                    this.client = this.server.AcceptTcpClient();
                    //now check the credentials
                    //byte[] passwordInBytes = this.udpListener.Receive(ref this.remoteIPEndpoint);
                    byte[] passwordInBytes = new byte[128];
                    int receivedBytes = this.client.Client.Receive(passwordInBytes);
                    Console.WriteLine("Ricevuto password di " + receivedBytes + " bytes");
                    Boolean result;
                    String passwd = (String)ByteArrayToObject(passwordInBytes);
                    if (passwd.Equals(Properties.Settings.Default.Password.Replace("\r\n","")))
                    {
                        result = true;
                        this.client.Client.Send(ObjectToByteArray(result), ObjectToByteArray(result).Length, 0);
                    }else{
                        result = false;
                        this.client.Client.Send(ObjectToByteArray(result), ObjectToByteArray(result).Length, 0);
                        
                        continue;
                    }
                    //connect to client's clipboard endpoint
                    /*
                    this.clientCB = new TcpClient();
                    this.clientCB.Connect(((IPEndPoint)this.client.Client.RemoteEndPoint).Address, 9898); //questo è il client che riceve
                    */
                    Console.WriteLine("Connected!");
                    isConnected = true; //set the variable in order to get into the next loop
                    NetworkStream stream = client.GetStream();
                    //connection checker
                    this.runConnectionChecker(); //run thread  which checks connection
                    Console.WriteLine("Quindi eccomi qui : dopo il conn checker ");
                    this.runCBListenerFaster(); //run thread which handle clipboard
                    Console.WriteLine("Quindi eccomi qui : dopo il run cb ");
                    while (this.isConnected==true)
                    { //loop around the global variable that says if the client is already connected
                        try
                        {
                            //Console.WriteLine("*** mi sono bloccato in ricezione");
                            bytes = this.udpListener.Receive(ref this.remoteIPEndpoint);//read exactly 128 bytes
                            if (this.isConnected == false)
                            {
                                Console.WriteLine("Appena ricevuto un messaggio udp mi sono accorto che isConnected==false");
                                break;
                            }
                            message = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                            parseMessage(message); // Translate data bytes to a ASCII string.
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Eccolo : " + e.Message);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("TANTI SALUTI: " + ex.Message);
                    break;
                }

            }

        }


        private void stopServer()
        {
            
            if (this.client != null)
                this.client.Close();
            if (this.cbSocketServer != null)
                this.cbSocketServer.Stop();
            if( this.cbListener!= null && this.cbListener.IsAlive)
                this.cbListener.Abort();
            if (this.ConnectionChecker != null && this.ConnectionChecker.IsAlive)
                this.ConnectionChecker.Abort();
            if (this.server != null)
                this.server.Server.Close();
            if(this.udpListener!= null)
                this.udpListener.Close();

        }

        private void restartServer()
        {
            stopServer();
            this.runThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                runServer();
            });
        }
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            stopServer();
        }


       




        private void ExitButton(object sender, RoutedEventArgs e)
        {
            //TODO chiudere server da tray area
            icon.Visibility = Visibility.Hidden;
            Application.Current.Shutdown();
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

        public void runConnectionChecker()
        {
            this.ConnectionChecker = new Thread(() =>
            {
                while (true)
                {

                    try
                    {
                        if (this.client.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (this.client.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                this.isConnected = false;
                                Console.WriteLine("OPS connessione interrotta");
                                this.udpListener.Close();
                                //this.cbSocketServer.Server.Close();
                                Console.WriteLine("Ho chiuso il listener UDP");
                                //stopServer();
                                //closeOnException();
                                return;
                                
                            }
                        }
                        Thread.Sleep(2000);
                    }
                    catch (SocketException se)
                    {
                        //closeOnException();
                        Console.WriteLine("La connessione è stata interrotta\n" + se.Message);
                        return;
                    }
                }
            });
            this.ConnectionChecker.Start();
        }

        private void closeOnException()
        {
            restartServer();
        }

        public void initCBListener()
        {
            this.cbSocketServer = new TcpListener(IPAddress.Any, Properties.Settings.Default.CBPort); //Start the TcpListener of the clipboard
            this.cbEndpoint = new IPEndPoint(IPAddress.Any, Properties.Settings.Default.CBPort); //Create the Ip Endpoint 
            this.cbSocketServer.Start(1); //Start the listener
        }

        public void runCBListenerFaster()
        {
            this.cbListener = new Thread(() =>
            {
                try
                {
                    Thread.CurrentThread.IsBackground = true;
                   
                        
                        Console.Write("Waiting for ClipBoard connection... ");
                        TcpClient acceptedClient = this.cbSocketServer.AcceptTcpClient();
                        Console.WriteLine("Clipboard is Connected!");
                        while (this.isConnected)
                        {
                            try
                            {
                                Console.WriteLine("Aspettando un messaggio dalla clipboard");
                                int count = 0;
                                int r = -1;
                                NetworkStream stream = acceptedClient.GetStream();
                                byte[] buffer = receiveAllData(stream);
                                Object received = ByteArrayToObject(buffer);
                                Console.WriteLine("FINE RICEZIONE\t Tipo: " + received.GetType() + " Dimensione : " + buffer.Length + " bytes");
                                SetClipBoard(received);
                            }
                            catch (IndexOutOfRangeException cbex)
                            {
                                //eccezione generata quando chiudo il client dalla clipboard
                                //bool b = this.isConnected;
                                Console.WriteLine("Index Out Of Range  generata in cb: [{0}]", cbex.Message);
                                //this.isConnected = false;
                                //closeOnException();
                                return;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("ECCEZIONE GENERATA IN RICEZIONE CB : [{0}]" , ex.Message);
                                return;
                                //closeOnException();
                                //return;
                                // restartServer();
                            }

                        }
                    }
                
                catch (Exception e)
                {
                    Console.WriteLine("Eccezione generica in cblistener " + e.Message);
                    return;
                }
            });
            this.cbListener.Start();
        }


        private byte[] receiveAllData(NetworkStream stream)
        {
            byte[] tmp = new byte[512]; //temporary buffer
            byte[] sizeOfBuf = new byte[4]; //init the buffer containing the size
            stream.Read(sizeOfBuf, 0, 4);
            //if (BitConverter.IsLittleEndian)
            //    Array.Reverse(sizeOfBuf);
            Int32 dim = BitConverter.ToInt32(sizeOfBuf, 0); //dimensione del buffer;
            //byte[] buffer = new byte[dim]; //init bufferone
            byte[] buffer = new byte[0];
            Console.WriteLine("La dimensione del bufferone è : {0}", dim);
            int counter = dim;
            while (counter > 0 ){
                int r = stream.Read(tmp, 0, 512);
                Console.WriteLine("Ricevuto " + r + " bytes");
                int oldBufLen = buffer.Length;
                Array.Resize(ref buffer, oldBufLen + r);
                Buffer.BlockCopy(tmp, 0, buffer, oldBufLen, r);
                counter = counter - r;
            }
            return buffer;
        }

        private void SetClipBoard(object received)
        {
            try
            {
                ClipboardManager cbm = new ClipboardManager(received); //passo l'oggetto al costruttore della classe
                //non so perchè forse perchè non sapevo in che altro modo lanciare il thread 
                Thread runThread = new Thread(new ThreadStart(cbm.setData));
                runThread.SetApartmentState(ApartmentState.STA);
                runThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        #region CredentialMgmt

        private bool checkPassword(String password)
        {
            if (Properties.Settings.Default.Password.Equals(password))
                return true;
            else
                return false;
        }

        private void setPassword(String password)
        {
            Properties.Settings.Default.Password = password;
            Properties.Settings.Default.Save();
        }

        #endregion



        // Convert an object to a byte array
        private byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        // Convert a byte array to an Object
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
             if(byteArrayContainsZipFile(arrBytes)){
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

        private BitmapImage byteArrayToBitmap(byte[] arrBytes)
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

        private bool byteArrayContainsBitmap(byte[] arrBytes)
        {
            if (arrBytes[0] == 255 && arrBytes[1] == 216 && arrBytes[2] == 255 && arrBytes[3] == 224)
            {
                return true;
            }
            return false;
        }

        private Object extractZIPtoFolder(byte[] arrBytes)
        {
            using (Stream ms = new MemoryStream(arrBytes))
            {
                Console.WriteLine("Lunghezza del buffer : " + arrBytes.Length);
                Console.WriteLine("Lunghezza dello stream : " + ms.Length);
                ZipArchive archive = new ZipArchive(ms);
                if (Directory.Exists(ZIP_EXTRACTED_FOLDER))
                {
                    Directory.Delete(ZIP_EXTRACTED_FOLDER,true);
                    Console.WriteLine("Cancello Vecchia cartella zip");
                }
                archive.ExtractToDirectory(ZIP_EXTRACTED_FOLDER);
                return archive;
            } 
        }

        private bool byteArrayContainsZipFile(byte[] arrBytes)
        {
            if( arrBytes[0]==80 && arrBytes[1]==75 && arrBytes[2] == 3 && arrBytes[3] == 4){
                return true;
            }
            return false;
        }

        private void tbPassword_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.Password = ((TextBox)sender).Text;
        }

        private void tbPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            String portString = ((TextBox)sender).Text;
            try
            {
                Int32 port = Convert.ToInt32(portString);
                Properties.Settings.Default.Port = port;
            }
            catch (Exception ex)
            {
                Console.WriteLine("La porta non è stata cambiata");
            }
        }

      

        /*public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }*/

 
            
            
        }

    


    public partial class NativeMethods
    {
        /// Return Type: BOOL->int  
        ///X: int  
        ///Y: int  
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, UIntPtr dwExtraInfo);

    }
}
