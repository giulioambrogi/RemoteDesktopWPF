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
        public String CB_FILES_DIRECTORY_PATH = @"./CBFILES/";
        public String ZIP_FILE_NAME_AND_PATH = "CBFILES.zip";
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
                if (e.authenticateWithPassword() == false)
                {
                    Console.WriteLine("Password errata");
                    e.server.Close();
                    e.server = null; //convenzione in modo tale che il conn checker si accorga che la password non vabbène
                    return;
                }
                e.CBClient = new TcpClient(e.ipAddress.ToString(), 9898);
                //e.cbServer.Connect(new IPEndPoint(e.ipAddress, 9898));
                Console.WriteLine("Connesso al server " + e.name);
            }
            catch (Exception ex)
            {
                e.server.Close();
                e.server = null;
                Console.WriteLine("Errore in connessione con " + e.name);
                return;
            }
   
            
        }

        public void sendMessage(string message){

                Byte[] data;
               // Array.Clear(data, 0, 128);
                data = System.Text.Encoding.ASCII.GetBytes(message);

                this.selectedServers.ElementAt(this.serverPointer).UdpSender.Send(data, data.Length);
                //Console.WriteLine("Sent: {0}", message);
            
        }

        public void disconnectFromServer(ServerEntity se){
               se.UdpSender.Close();
               //se.server.GetStream().Close();
               se.server.Close();
               se.CBClient.Close();
               
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


        public void sendClipBoardFaster(TcpClient client)
        {

                byte[] content = new byte[0]; //byte array that will contain the clipboard
                byte[] sizeInBytes = new byte[4]; //byte array that will contain the size

                if (Clipboard.ContainsText())
                {
                   content= ObjectToByteArray(Clipboard.GetText());
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    //Creates a new, blank zip file to work with - the file will be
                    //finalized when the using 
                    if (Directory.Exists(CB_FILES_DIRECTORY_PATH))
                         Directory.Delete(CB_FILES_DIRECTORY_PATH, true);
                    if (File.Exists("CBFILES.zip"))
                        File.Delete(ZIP_FILE_NAME_AND_PATH);
                    Directory.CreateDirectory(CB_FILES_DIRECTORY_PATH);
                    foreach (String filepath in Clipboard.GetFileDropList())
                    {
                        String dstFilePath = CB_FILES_DIRECTORY_PATH+Path.GetFileName(filepath);
                        System.IO.File.Copy(filepath, dstFilePath);
                       
                    }
                    ZipFile.CreateFromDirectory(CB_FILES_DIRECTORY_PATH, ZIP_FILE_NAME_AND_PATH, CompressionLevel.Fastest, true);
                    FileInfo info = new FileInfo(ZIP_FILE_NAME_AND_PATH);
                    Console.WriteLine("Dimensione del file zip : " + info.Length +" bytes");
                    if (info.Length > 1024 * 1024 * 200) //limite a 200 mega
                    {
                        MessageBoxResult result = MessageBox.Show("Sei sicuro di voler trasferire " + info.Length + " bytes?");
                        if (result == MessageBoxResult.No || result == MessageBoxResult.Cancel)
                            Console.WriteLine("Can't send more than 200 Mega Bytes");
                            return;
                    }
                    content = File.ReadAllBytes(ZIP_FILE_NAME_AND_PATH); 
                }
                else if (Clipboard.ContainsImage())
                {
                    //content = imageToByteArray(Clipboard.GetImage());
                    content = bitmapSourceToByteArray(Clipboard.GetImage());
                }
                else if (Clipboard.ContainsAudio())
                {
                    content = ObjectToByteArray(Clipboard.GetAudioStream());
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

        public byte[] imageToByteArray(System.Windows.Media.Imaging.BitmapSource imageIn)
        {
            byte[] data;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageIn));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = ms.ToArray();
            }
            return data;
        }

        public byte[] bitmapSourceToByteArray(BitmapSource bms)
        {
             MemoryStream memStream = new MemoryStream();              
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bms));
            encoder.Save(memStream);
            return memStream.GetBuffer();
        }
        public BitmapImage byteArrayToBitMapImage(byte[] byteArrayIn)
        {

            MemoryStream strmImg = new MemoryStream(byteArrayIn);
            BitmapImage myBitmapImage = new BitmapImage();
            myBitmapImage.BeginInit();
            myBitmapImage.StreamSource = strmImg;
            myBitmapImage.DecodePixelWidth = 200;
            myBitmapImage.EndInit();
            return myBitmapImage;
        }

        #endregion
    }
}
