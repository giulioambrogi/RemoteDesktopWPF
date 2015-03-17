using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace HookerClient
{
    class ServerManager
    {
        public const Int32 default_port = 5143;
        public const Int32 cbport = 9898;
        public List<ServerEntity> availableServers;
        public List<ServerEntity> selectedServers;
        public int serverPointer;
        public TcpClient client ;
        public NetworkStream stream;
        public Socket ClipboardEndpoint;
        //public String CB_FILES_DIRECTORY_PATH = @"C:/CBFILES/";
        //public String ZIP_FILE_NAME_AND_PATH = @"C:/CBFILES.zip";

        public TcpListener cbSocketServer; //clipboard receiver
        private IPEndPoint cbEndpoint;  //clipboardReceiverEndpoint
        private Thread cbListener;
        public ServerManager()
        {
            this.availableServers = new List<ServerEntity>();
            //populate server list on GUI and update list
            this.selectedServers = new List<ServerEntity>();
            serverPointer = 0;
        }

        public void initCBListener()
        {
            if (this.cbSocketServer != null)
                this.cbSocketServer.Server.Close();
            this.cbSocketServer = new TcpListener(IPAddress.Any, Properties.Settings.Default.CBPort); //Start the TcpListener of the clipboard
            this.cbEndpoint = new IPEndPoint(IPAddress.Any, Properties.Settings.Default.CBPort); //Create the Ip Endpoint 
            this.cbSocketServer.Start(Properties.Settings.Default.MaximumAllowedServers); //Start the listener
        }

        public void runCBListenerFaster()
        {
            this.cbListener = new Thread(() =>
            {
                try
                {
                    Thread.CurrentThread.IsBackground = true;


                    
                    while (true)
                    {
                        //ho spostato qui queste tre righe per evitare il prob che ogni volta va tutto a puttane 
                        Console.Write("Waiting for ClipBoard connection... ");
                        TcpClient acceptedClient = this.cbSocketServer.AcceptTcpClient();
                        Console.WriteLine("Clipboard is Connected!");

                        Thread.Sleep(100);
                        try
                        {
                            Console.WriteLine("Aspettando un messaggio dalla clipboard");
                            NetworkStream stream = acceptedClient.GetStream();
                            byte[] buffer = receiveAllData(stream);
                            
                            Object received = AmbrUtils.ByteArrayToObject(buffer);
                            Console.WriteLine("FINE RICEZIONE\t Tipo: " + received.GetType() + " Dimensione : " + buffer.Length + " bytes");
                            SetClipBoard(received);
                            Console.WriteLine("CBLISTENER : clipboard settata");
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
            Console.WriteLine("La dimensione che mi aspetto è : {0}", dim);
            int counter = dim;
            while (counter > 0)
            {
                int r = stream.Read(tmp, 0, 512);
                //Console.WriteLine("Ricevuto " + r + " bytes");
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
    
    
        public void connectToServer( ServerEntity e){
            try
            {
                Console.WriteLine("Mi sto connettendo al server " + e.name);
                e.server = new TcpClient();
                e.server.Connect(e.ipAddress, e.port_base);
                e.stream = e.server.GetStream();
                //exchange data for authentication
                //connetto sender udp
                e.UdpSender = new UdpClient();
                e.UdpSender.Connect(e.ipAddress, e.port_base);
                //connect to clipboard
                if (e.authenticateWithPassword() == false)
                {
                    Console.WriteLine("Password errata");
                    e.server.Close();
                    e.server = null; //convenzione in modo tale che il conn checker si accorga che la password non vabbène
                    return;
                }
                //e.initCBListener(); // lancio il cb listener del client
                //connessione delle clipboard 
                e.CBClient = new TcpClient(e.name, cbport); // client si connette al cb listener del server
                //creazione del listener cb lato client per ricevere la cb dal server
                //e.runCBListenerFaster(); // run clipboard listener che comincia la fase di accept, e dopo aver accettato riceve all'infinito
                //e.cbServer.Connect(new IPEndPoint(e.ipAddress, 9898));
                Console.WriteLine("Connesso al server " + e.name);
                
            }
            catch (Exception ex)
            {
                e.server.Close();
                e.server = null;
                Console.WriteLine("Errore in connessione con " + e.name + "\nMessaggio : "+ ex.Message);
                return;
            }
   
            
        }

        public void sendMessage(string message){

                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                this.selectedServers.ElementAt(this.serverPointer).UdpSender.Send(data, data.Length);
                //Console.WriteLine("Sent: {0}", message);
            
        }

        public void disconnectFromServer(ServerEntity se){
               if(se.UdpSender!=null)
                    se.UdpSender.Close();
               if(se.server != null)
                    se.server.Close();
               if(se.CBClient!= null)
                   se.CBClient.Close();
               if(this.cbSocketServer!=null )
                   this.cbSocketServer.Server.Close();        
        }

        public void nextSelectedServers(){
            //treat the list as a circular list
            
            if (this.serverPointer < this.selectedServers.Count - 1)
            {
                this.serverPointer++;
            }
            else if (this.serverPointer != 0 && this.serverPointer == this.selectedServers.Count - 1)
            {
                this.serverPointer = 0;
            }

        }

        internal void connect()
        {
            initCBListener();
            foreach ( ServerEntity se in selectedServers)
            {
                connectToServer( se);
            }
            this.runCBListenerFaster();
        }

        internal void disconnect()
        {
            foreach (ServerEntity se in selectedServers)
            {
                disconnectFromServer(se);
            }
            if (this.cbSocketServer != null)
                this.cbSocketServer.Server.Close();
            if (this.cbListener != null && this.cbListener.IsAlive)
                this.cbListener.Abort();
        }

        public ServerEntity getSrvByName(String name)
        {
            foreach (ServerEntity se in this.availableServers)
            {
                if (se.name.Equals(name))
                {
                    return se;
                }
            }
            return null;
        }

        public ServerEntity getServerById(int id)
        {
            foreach (ServerEntity se in this.availableServers)
            {
                if (se.Id == id)
                {
                    return se;
                }
            }
            return null;
        }

        public void sendClipBoardFaster(TcpClient client)
        {

                byte[] content = new byte[0]; //byte array that will contain the clipboard
                byte[] sizeInBytes = new byte[4]; //byte array that will contain the size

                if (Clipboard.ContainsText())
                {
                    content = AmbrUtils.ObjectToByteArray(Clipboard.GetText());
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    //Creates a new, blank zip file to work with - the file will be
                    //finalized when the using 
                    if (Directory.Exists(AmbrUtils.CB_FILES_DIRECTORY_PATH))
                        Directory.Delete(AmbrUtils.CB_FILES_DIRECTORY_PATH, true);
                    if (File.Exists(AmbrUtils.ZIP_FILE_NAME_AND_PATH))
                        File.Delete(AmbrUtils.ZIP_FILE_NAME_AND_PATH);
                    Directory.CreateDirectory(AmbrUtils.CB_FILES_DIRECTORY_PATH);
                    foreach (String filepath in Clipboard.GetFileDropList())
                    {
                        FileAttributes attr = File.GetAttributes(filepath);
                        if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                        {
                            DirectoryInfo diSource = new DirectoryInfo(filepath);
                            System.IO.Directory.CreateDirectory(AmbrUtils.CB_FILES_DIRECTORY_PATH + diSource.Name);
                            DirectoryInfo diDst = new DirectoryInfo(AmbrUtils.CB_FILES_DIRECTORY_PATH + diSource.Name);
                            AmbrUtils.CopyFilesRecursively(diSource, diDst);
                        }
                        else { //it's a file
                            String dstFilePath = AmbrUtils.CB_FILES_DIRECTORY_PATH + Path.GetFileName(filepath);
                            System.IO.File.Copy(filepath, dstFilePath);
                        
                        }
                       
                    }
                    ZipFile.CreateFromDirectory(AmbrUtils.CB_FILES_DIRECTORY_PATH, AmbrUtils.ZIP_FILE_NAME_AND_PATH, CompressionLevel.Fastest, true);
                    FileInfo info = new FileInfo(AmbrUtils.ZIP_FILE_NAME_AND_PATH);
                    Console.WriteLine("Dimensione del file zip : " + info.Length +" bytes");
                    if (info.Length > 1024 * 1024 * 200) //limite a 200 mega
                    {
                        MessageBoxResult result = MessageBox.Show("Non è possibile mandare file per più di 200 mega ( attualmente " + info.Length + " bytes )");
                        Console.WriteLine("Can't send more than 200 Mega Bytes");
                        return;
                    }
                    content = File.ReadAllBytes(AmbrUtils.ZIP_FILE_NAME_AND_PATH); 
                }
                else if (Clipboard.ContainsImage())
                {
                    //content = imageToByteArray(Clipboard.GetImage());
                    content = AmbrUtils.bitmapSourceToByteArray(Clipboard.GetImage());
                }
                else if (Clipboard.ContainsAudio())
                {
                    content = AmbrUtils.audioSourceToByteArray(Clipboard.GetAudioStream());
                }
                else
                {
                    Console.WriteLine("Nothing to send");
                    return;
                }
                
                NetworkStream ns = client.GetStream();
                Int32 len = content.Length;
                sizeInBytes = BitConverter.GetBytes(len); //convert size of content into byte array
                Console.WriteLine("Mando size: " + len);
                ns.Write(sizeInBytes, 0, 4); //write 
                Console.WriteLine("Mando buffer...");
                ns.Write(content, 0, content.Length);
                ns.Flush();
                Console.WriteLine("Mandato!");

        }

       
    }
}
