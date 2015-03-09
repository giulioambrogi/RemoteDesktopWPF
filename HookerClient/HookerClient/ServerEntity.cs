using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;

namespace HookerClient
{
    class ServerEntity
    {
        //usato per connettersi al server
        public TcpClient server;
        //usato per mandare i messaggi al server
        public UdpClient UdpSender;
        public IPAddress ipAddress;
        public string name;
        public string password;
        public IPEndPoint remoteIPEndPoint;
        public NetworkStream stream;
        //usato per clipboard
        public IPEndPoint cbLocal;
        public TcpClient CBClient;
        public int Id; //id usato per creare gli oggetti dinamici 
        public int port_base;
        private int DEFAULT_BASE_PORT = 5143;
        private TcpListener cbSocketServer; //clipboard receiver
        private IPEndPoint cbEndpoint;  //clipboardReceiverEndpoint
        private Thread cbListener;
        public ServerEntity( string name)
        {
            try
            {
                this.name = name;
                IPAddress[] ipaddrs;
                ipaddrs = Dns.GetHostAddresses(this.name);
                //risolvo l'indirizzo ipv4 in fase di costruzione
                this.ipAddress = ipaddrs.First(a => a.AddressFamily == AddressFamily.InterNetwork);
                this.password = "TODO";
                this.remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
                this.cbLocal = new IPEndPoint(this.ipAddress, 9898);
                // this.cbServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // this.CBClient = new TcpClient(this.ipAddress.ToString(),9898);
            }catch (SocketException se)
            {
                Console.WriteLine("[" + this.name + "]" + se.Message);
            }
        }

        public void setPassword(string password)
        {
            this.password = password;
        }

        private String getPassword()
        {
            return this.password;
        }

        private String getName()
        {
            return this.name;
        }
        private int getId()
        {
            return this.Id;
        }

        internal void setId(int index)
        {
            this.Id = index;
        }
        internal void setPortFromString(String port)
        {
            try
            {
                int convertedPort = Convert.ToInt32(port);
                this.port_base = convertedPort;
            }
            catch (Exception e)
            {
                this.port_base = DEFAULT_BASE_PORT;
            }
        }

        internal bool authenticateWithPassword()
        {
            byte[] b =ObjectToByteArray(this.password);
            Console.WriteLine("Mandato password : [" + this.password + "]");
            
            //UdpSender.Send(b, b.Length);
            server.Client.Send(b, b.Length, 0);

            //byte[] receivedResponse = UdpSender.Receive(ref remoteIPEndPoint);
            byte[] receivedResponse = new byte[ObjectToByteArray(new Boolean()).Length];
            server.Client.Receive(receivedResponse);
            Boolean result = (Boolean)ByteArrayToObject(receivedResponse);
            return result;
        }

        public void initCBListener()
        {
            this.cbSocketServer = new TcpListener(IPAddress.Any, Properties.Settings.Default.CBPort); //Start the TcpListener of the clipboard
            this.cbEndpoint = new IPEndPoint(IPAddress.Any, Properties.Settings.Default.CBPort); //Create the Ip Endpoint 
            this.cbSocketServer.Start(1); //Start the listener
        }

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
                    while (true)
                    {
                        Thread.Sleep(1000);
                        try
                        {
                            Console.WriteLine("Aspettando un messaggio dalla clipboard");
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
                            Console.WriteLine("ECCEZIONE GENERATA IN RICEZIONE CB : [{0}]", ex.Message);
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
            while (counter > 0)
            {
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
                //ClipboardManager cbm = new ClipboardManager(received); //passo l'oggetto al costruttore della classe
                //non so perchè forse perchè non sapevo in che altro modo lanciare il thread 
                ClipboardMgmt cbm = new ClipboardMgmt(received);
                Thread runThread = new Thread(new ThreadStart(cbm.setData));
                runThread.SetApartmentState(ApartmentState.STA);
                runThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    
    
    }
}
