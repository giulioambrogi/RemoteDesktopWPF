using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace HookerClient
{
    class ServerManager
    {
        public const Int32 port = 5143;
        public List<ServerEntity> availableServers;
        public List<ServerEntity> selectedServers;
        public int serverPointer;
        public TcpClient client ;
        public NetworkStream stream;
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
                Console.WriteLine("Connesso al server " + e.name);
                //connetto sender udp
                e.UdpSender = new UdpClient();
                e.UdpSender.Connect(e.ipAddress, port);
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

        public void closeConnection(){
       
                this.selectedServers.ElementAt(this.serverPointer).UdpSender.Close();
                this.selectedServers.ElementAt(this.serverPointer).UdpSender = null;
                this.selectedServers.ElementAt(this.serverPointer).server.GetStream().Close();
                this.selectedServers.ElementAt(this.serverPointer).server.Close();
              
            
           }

        public void nextSelectedServers(){
            //treat the list as a circular list
           

        }

        internal void connect()
        {
            foreach ( ServerEntity se in selectedServers)
            {
                connectToServer( se);
            }
        }
    }
}
