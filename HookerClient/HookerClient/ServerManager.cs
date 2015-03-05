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
        public ServerManager()
        {
            this.availableServers = new List<ServerEntity>();
            //populate server list on GUI and update list
            this.selectedServers = new List<ServerEntity>();
           
            serverPointer = 0;
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
               // Thread.Sleep(2000);
                e.CBClient = new TcpClient(e.ipAddress.ToString(), 9898); 
                
                //e.cbServer.Connect(new IPEndPoint(e.ipAddress, 9898));
                Console.WriteLine("Connesso al server " + e.name);
            }
            catch (SocketException ex)
            {
                //e.server.Close();
                Console.WriteLine("Errore in connessione con " + e.name);
                return;
            }
   
            
        }

        public void sendMessage(string message){

                Byte[] data;
               // Array.Clear(data, 0, 128);
                data = System.Text.Encoding.ASCII.GetBytes(message);

                this.selectedServers.ElementAt(this.serverPointer).UdpSender.Send(data, data.Length);
                Console.WriteLine("Sent: {0}", message);
            
        }

        public void disconnectFromServer(ServerEntity se){
                 se.UdpSender.Close();
               se.server.GetStream().Close();
               se.server.Close();
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

            //setPasswords(); //gets the passwords for users , to be used for connection
            foreach ( ServerEntity se in selectedServers)
            {
                connectToServer( se);
            }
        }

      
        internal void disconnect()
        {
            foreach (ServerEntity se in selectedServers)
            {
                disconnectFromServer(se);
            }
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

        #region clipboard management

        public void testSendClipboard()
        {
             byte[] lengthBytes = new byte[4];
             if (Clipboard.ContainsText())
                 {
                 String text = Clipboard.GetText();
                 String type = "T";
                 byte[] typeBytes = ObjectToByteArray(type);
                 byte[] contentBytes = ObjectToByteArray(text);
                 lengthBytes = ObjectToByteArray(contentBytes.Length);
                 NetworkStream ns = this.selectedServers.ElementAt(this.serverPointer).CBClient.GetStream();
                 ns.Write(typeBytes, 0, typeBytes.Length);
                 ns.Write(lengthBytes, 0, lengthBytes.Length);//TODO : preferirei mandare una lunghezza fissa
                 ns.Write(contentBytes, 0, contentBytes.Length);
             }
             else if (Clipboard.ContainsFileDropList())
             {

             }
             else if (Clipboard.ContainsImage())
             {
                 
             }
             else if (Clipboard.ContainsAudio())
             {

             }
             else {
                 Console.WriteLine("Nothing to send");
             }
        }

        public void sendClipBoard(TcpClient client)
        {
            if (Clipboard.ContainsData(DataFormats.Text))
                if (Clipboard.ContainsText())
                {
                    String text = Clipboard.GetText();
                    String type = "T";
                    byte[] typeBytes = ObjectToByteArray(type);
                    byte[] contentBytes = ObjectToByteArray(text);
                    byte[] lengthBytes = ObjectToByteArray(contentBytes.Length);
                    NetworkStream ns = client.GetStream();
                    ns.Write(typeBytes, 0, typeBytes.Length);
                    ns.Write(lengthBytes, 0, lengthBytes.Length);//TODO : preferirei mandare una lunghezza fissa
                    ns.Write(contentBytes, 0, contentBytes.Length);
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    //Creates a new, blank zip file to work with - the file will be
                    //finalized when the using statement completes
                    ZipArchive newFile = ZipFile.Open("cbfiles", ZipArchiveMode.Create);
                    //Here are two hard-coded files that we will be adding to the zip
                    //file.  If you don't have these files in your system, this will
                    //fail.  Either create them or change the file names.
                    foreach (String filepath in Clipboard.GetFileDropList())
                    {
                        newFile.CreateEntryFromFile(@filepath, Path.GetFileName(filepath), CompressionLevel.Fastest);
                    }
                    byte[] zipFileInByte = ObjectToByteArray(newFile);//creo lo zip del file
                    NetworkStream ns = client.GetStream();

                    //mando il tipo 
                    byte[] typeInBytes  = ObjectToByteArray("F");
                    ns.Write(typeInBytes, 0, typeInBytes.Length);

                    //mando dimensione dello zip
                    byte[] lengthInBytes = ObjectToByteArray(zipFileInByte.Length);
                    ns.Write(lengthInBytes, 0, lengthInBytes.Length);

                    //mando il file zip
                    ns.Write(zipFileInByte, 0, zipFileInByte.Length);

                }
                else if (Clipboard.ContainsImage())
                {
                   byte[] imageInBytes =  ObjectToByteArray( Clipboard.GetImage());
                }
                else if (Clipboard.ContainsAudio())
                {
                    byte[] audioInBytes = ObjectToByteArray(Clipboard.GetAudioStream());
                }
                else
                {
                    Console.WriteLine("Nothing to send");
                }

        }



        public void sendClipBoardFaster(TcpClient client)
        {
            //client = new TcpClient(this.selectedServers.ElementAt(this.serverPointer).name, 9898);
                if (Clipboard.ContainsText())
                {
                   byte[] contentBytes = ObjectToByteArray(Clipboard.GetText());
                   NetworkStream ns = client.GetStream();
                   ns.Write(contentBytes, 0, contentBytes.Length);
                   ns.Flush();
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    //Creates a new, blank zip file to work with - the file will be
                    //finalized when the using statement completes
                    ZipArchive newFile = ZipFile.Open("cbfiles", ZipArchiveMode.Create);
                    //Here are two hard-coded files that we will be adding to the zip
                    //file.  If you don't have these files in your system, this will
                    //fail.  Either create them or change the file names.
                    foreach (String filepath in Clipboard.GetFileDropList())
                    {
                        newFile.CreateEntryFromFile(@filepath, Path.GetFileName(filepath), CompressionLevel.Fastest);
                    }
                    byte[] zipFileInByte = ObjectToByteArray(newFile);//creo lo zip del file
                    NetworkStream ns = client.GetStream();
                    //mando il file zipxdd
                    ns.Write(zipFileInByte, 0, zipFileInByte.Length);
                }
                else if (Clipboard.ContainsImage())
                {
                    byte[] imageInBytes = ObjectToByteArray(Clipboard.GetImage());
                    NetworkStream ns = client.GetStream();
                    //mando il file zip
                    ns.Write(imageInBytes, 0, imageInBytes.Length);
                }
                else if (Clipboard.ContainsAudio())
                {
                    byte[] audioInBytes = ObjectToByteArray(Clipboard.GetAudioStream());
                    NetworkStream ns = client.GetStream();
                    ns.Write(audioInBytes, 0, audioInBytes.Length);
                }
                else
                {
                    Console.WriteLine("Nothing to send");
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


        #endregion
    }
}
