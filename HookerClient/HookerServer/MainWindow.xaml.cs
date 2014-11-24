using CSMailslotServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
       
        static Int32 port = 5143;
        public IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        public TcpListener server = null;
        public TcpClient client = null;
        public UdpClient udpListener;
        public IPEndPoint remoteIPEndpoint;
        Thread runThread;
        Thread ConnectionChecker;
        public MainWindow()
        {
            InitializeComponent();
            Console.WriteLine("Nome computer :"+System.Environment.MachineName);
            btnStart.IsEnabled = true;
            btnClose.IsEnabled = false;
            this.ConnectionChecker = new Thread(() =>
            {
                while (true)
                {
                    // Detect if client disconnected
                    try
                    {
                        bool bClosed = false;
                        if (this.client.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (this.client.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                bClosed = true;
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
                    }
                }
            }
                       );
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

                    //InputSimulator.SimulateKeyDown(vk);
                }
                else if (commands.ElementAt(2).ToString().Equals("UP"))
                {
                    //evento key up
                    Console.WriteLine(commands.ElementAt(0) + " UP");
                    //InputSimulator.SimulateKeyUp(vk);
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
            this.ConnectionChecker.Start();
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
                    
                    NetworkStream stream = client.GetStream();

                    // Loop to receive all the data sent by the client.
                    while (this.client.Connected ){
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
            this.ConnectionChecker.Abort();
            if (this.client != null)
            {
                this.client.Close();
            }
            this.remoteIPEndpoint = null;
            this.server.Server.Close();
            this.server.Stop();
            this.udpListener.Close();
            this.udpListener = null;
            this.client = null;
            this.server = null;
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
