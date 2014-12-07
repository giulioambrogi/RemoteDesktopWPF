using CSMailslotServer;
using System;
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
        static Int32 port = 5143;
        public IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        public TcpListener server = null;
        public TcpClient client = null;
        public UdpClient udpListener;
        public IPEndPoint remoteIPEndpoint;
        Thread runThread;
        Thread ConnectionChecker;
        Thread cbListener;
        public TcpListener cbSocketServer;
        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine("Nome computer :"+System.Environment.MachineName);
            btnStart.IsEnabled = true;
            btnClose.IsEnabled = false;
            bindHotKeyCommands();
        }

        private void bindHotKeyCommands()
        {
            /*
            RoutedCommand recvCb = new RoutedCommand();
            recvCb.InputGestures.Add(new KeyGesture(Key.X, ModifierKeys.Control | ModifierKeys.Alt));
            CommandBindings.Add(new CommandBinding(recvCb,receiveClipboard));
            */
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
                NativeMethodsBiss.SetCursorPos(x,y);
                               

                PointX.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    PointX.Text = x.ToString();
                    
                }));
                PointY.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    PointY.Text = y.ToString();
                }));
                Console.WriteLine("Received: {0}", buffer);
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

                //UPDATE MESSAGEBOX
                lbMessages.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    lbMessages.Items.Add(buffer);
                }));


            }
            else
            {
                Console.WriteLine("MESSAGGIO NON CAPITO :" + buffer);
            }

           

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            this.runThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                runServer();
            });

            

            this.runThread.Start();
            btnClose.IsEnabled = true;
            btnStart.IsEnabled = false;
        }


        private void runServer()
        {
            
            this.server = new TcpListener(IPAddress.Any, port);
            this.udpListener = new UdpClient(port);
            this.remoteIPEndpoint = new IPEndPoint(IPAddress.Any, port);
            this.server.Start(1);
            Byte[] bytes = new Byte[128];
            String message = null;
            

            // Enter the listening loop.
            while (true)
            {
                try
                {
                    Console.Write("Waiting for a connection... ");
                    this.client = this.server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    isConnected = true;
                    NetworkStream stream = client.GetStream();
                    
                    //connection checker
                    this.runConnectionChecker();
                    this.runCBListener();
                    while (isConnected ){
                        int i;
                        //read exactly 128 bytes
                        bytes = this.udpListener.Receive(ref this.remoteIPEndpoint);
                        message = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                        // Translate data bytes to a ASCII string.
                        
                        parseMessage(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERRORE: "+ex.Message);
                    
                }

            }

        }


        private void stopServer()
        {
            if(this.ConnectionChecker!= null)
                this.ConnectionChecker.Abort();
            if (this.client != null)
                this.client.Close();

            this.server.Server.Close();
            this.server.Stop();
            this.udpListener.Close();
            this.cbSocketServer.Stop();
            this.runThread.Abort();

        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            stopServer();
            btnClose.IsEnabled = false;
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
           //TODO chiudere server da tray area

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

        public void runConnectionChecker()
        {
            this.ConnectionChecker = new Thread( () =>
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
                                     MessageBox.Show("La connessione è stata interrotta");
                                     //closeOnException();
                                     return;
                                 }
                             }
                             Thread.Sleep(2000);
                         }
                         catch (SocketException se)
                         {
                             //closeOnException();
                             MessageBox.Show("La connessione è stata interrotta");
                             break;
                         }
                     }
        });
            this.ConnectionChecker.Start();
        }


        public void runCBListener()
        {
            this.cbListener = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                this.cbSocketServer = new TcpListener(IPAddress.Any, 9898);
                this.remoteIPEndpoint = new IPEndPoint(IPAddress.Any, port);
                this.cbSocketServer.Start(1);

                byte[] typeByte = new byte[25];
                byte[] lengthByte = new byte[1000];
                byte[] contentByte;
                
                //build clipboard
  /*              this.cbSocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.cbSocketServer.Server.Bind(new IPEndPoint(IPAddress.Any, 9898));
                this.cbSocketServer.Listen(2);
                Console.WriteLine("Aspettando il collegamento alla cb...");
                this.cbSocketServer.Accept();
                Console.WriteLine("Clipboard è connessa");
                this.cbSocketServer.Server.ReceiveTimeout = Timeout.Infinite;*/

                Console.Write("Waiting for ClipBoard connection... ");
                TcpClient acceptedClient = this.cbSocketServer.AcceptTcpClient();
                Console.WriteLine("Clipboard is Connected!");
               
               
               
                String message = null;

                while (true)
                {
                    try
                    {
                        Console.WriteLine("Aspettando un messaggio dalla clipboard");
                        NetworkStream stream = acceptedClient.GetStream();
                        int recvType = stream.Read(typeByte, 0, 25);
                        String type = (String)ByteArrayToObject(typeByte);
                        Console.WriteLine("Ricevuto " + recvType + " bytes. Tipo " + type);
                        //int recvLength = this.cbSocketServer.Server.Receive(lengthByte, 4, 0);
                        int recvLength = stream.Read(lengthByte, 0, lengthByte.Length);
                        int length = (int)ByteArrayToObject(lengthByte);
                        Console.WriteLine("Ricevuto " + recvLength + " bytes. Lunghezza del contenuto: " + length);
                        contentByte = new byte[length];
                        Console.WriteLine("Sto ricevendo " + length + " bytes di  tipo [" + type + "] ...");
                       
                       // int recvContent = this.cbSocketServer.Server.Receive(contentByte, length, 0);
                        int recvContent = stream.Read(contentByte, 0, length);
                        Console.WriteLine("Ricevuto");
                        Thread.Sleep(500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ECCEZIONE GENERATA IN RICEZIONE CB :" + ex.Message);
                        break;
                    }
                    
                }
            });
            this.cbListener.Start();
        }

        public void runCBListenerFaster()
        {
            this.cbListener = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                this.cbSocketServer = new TcpListener(IPAddress.Any, 9898);
                this.remoteIPEndpoint = new IPEndPoint(IPAddress.Any, port);
                this.cbSocketServer.Start(1);

                byte[] contentByte;
                Console.Write("Waiting for ClipBoard connection... ");
                TcpClient acceptedClient = this.cbSocketServer.AcceptTcpClient();
                Console.WriteLine("Clipboard is Connected!");

                while (true)
                {
                    try
                    {
                        Console.WriteLine("Aspettando un messaggio dalla clipboard");
                        NetworkStream stream = acceptedClient.GetStream();
                        byte[] buffer = new byte[0];
                        byte[] tmp = new byte[512]; //temporary buffer
                        stream.ReadTimeout = Timeout.Infinite;
                        int r ; 
                        int count = 0; 
                        while (( r = stream.Read(tmp, 0, 512)) > 0)
                        {
                            count = count + r;
                            int oldBufLen =  buffer.Length;
                            Array.Resize(ref buffer, oldBufLen+ r);
                            Buffer.BlockCopy(tmp, 0, buffer, oldBufLen, r);
                            Console.WriteLine("Ricevuto " + count + " bytes ");
                        }

                        Object received = ByteArrayToObject(buffer);
                        SetClipBoard(received);
                        Console.WriteLine("FINE RICEZIONE\t Tipo: "+received.GetType()+" Dimensione : " + tmp + " bytes");
                        
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ECCEZIONE GENERATA IN RICEZIONE CB :" + ex.Message);
                        break;
                    }

                }
            });
            this.cbListener.Start();
        }

        private void SetClipBoard(object received)
        {
            Type t = received.GetType();
            if (t ==typeof(String))
            {
                Clipboard.SetText((String)received);
            }
            else if (t == typeof(ZipArchive))
            {
                UnzipArchive();
                System.Collections.Specialized.StringCollection files = getFileNames(@"./ExtractedFiles");
                Clipboard.SetFileDropList(files);
            }
            else if(t == typeof(BitmapSource)){
                Clipboard.SetImage((BitmapSource)received);
            }
            else if(t == typeof(Stream)){
                
                Clipboard.SetAudio((Stream) received);
            }
        }

        private System.Collections.Specialized.StringCollection getFileNames(string p)
        {
            string[] filenames = Directory.GetFiles(p);
            System.Collections.Specialized.StringCollection sc = new System.Collections.Specialized.StringCollection() ;
            foreach (string s in filenames)
            {
                sc.Add(s);
            }
            return sc;
        }

        private void UnzipArchive()
        {
            string zipPath = @"./cb/cb.zip";
            string extractPath = @"./cb/cbfiles/";

            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                   // if (entry.FullName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                  entry.ExtractToFile(System.IO.Path.Combine(extractPath, entry.FullName));
                }
            } 
        }

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
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }

    }
    public partial class NativeMethodsBiss
    {
        /// Return Type: BOOL->int  
        ///X: int  
        ///Y: int  
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll", EntryPoint = "SetCursorPos")]
        [return: System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetCursorPos(int X, int Y);
    } 
}
