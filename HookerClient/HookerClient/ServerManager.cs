using System;
using System.Collections.Generic;
using System.IO;
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
        public const Int32 port = 5143;
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
                e.server.Connect(e.ipAddress, port);
                e.stream = e.server.GetStream();
                //exchange data for authentication
                //connetto sender udp
                e.UdpSender = new UdpClient();
                e.UdpSender.Connect(e.ipAddress, port);
                //connect to clipboard
               // Thread.Sleep(2000);
                e.cbServer.Connect(new IPEndPoint(e.ipAddress, 9898));
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

        #region clipboard management

        public void testSendClipboard()
        {
            byte[] typeBytes = new byte[4] ;
             byte[] lengthBytes = new byte[4];
             if (Clipboard.ContainsData(DataFormats.Text))
             {
                 String text = Clipboard.GetText();
                 typeBytes = ObjectToByteArray("T");
                 byte[] contentBytes = ObjectToByteArray(text);
                 lengthBytes = ObjectToByteArray(contentBytes.Length);

                 int typeSent = this.selectedServers.ElementAt(this.serverPointer).cbServer.Send(typeBytes, 4, 0);
                 Console.WriteLine("Inviati " + typeSent + " bytes (tipo)");
                 int lengthSent = this.selectedServers.ElementAt(this.serverPointer).cbServer.Send(lengthBytes, 4, 0);
                 Console.WriteLine("Inviati " + lengthSent + " bytes (lunghezza = " + contentBytes.Length + ")");
                 int bytesSent = this.selectedServers.ElementAt(this.serverPointer).cbServer.Send(contentBytes, contentBytes.Length, 0);
                 Console.WriteLine("Inviati " + bytesSent + " bytes (contenuto = "+(String)ByteArrayToObject(contentBytes)+")");
                 Thread.Sleep(100);
                 // int sent2 = this.selectedServers.ElementAt(this.serverPointer).cbServer.Send(bytes);
                 // Console.WriteLine("Inviati " + sent2 + " bytes [" + text + "]");
             }
             else {
                 Console.WriteLine("Nothing to send");
             }
        }

        public void sendClipBoard(ServerEntity se, Socket from, Object obj, String format)
        {
            switch (format)
            {
                case "T":
                    String s = (String)obj;
                    byte[] bytearr = this.ObjectToByteArray(obj);
                    int sent = from.Send(bytearr);
                    Console.WriteLine("Ho inviato " + sent + " bytes [" + s + "]");
                    break;
                case "F":
                    break;
                case "I":
                    break;
                default:
                    break;


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
