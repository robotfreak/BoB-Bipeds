using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading;

namespace ServoControl
{
    /// <summary>
    /// Zusammenfassung für Class1.
    /// </summary>
    public class Client
	{
		//private Socket socket = null;
		private bool receiving = false;
		private int SleepTime = 100;
		private int TimeOut = 5000;
		private int BufferSize = 10240;

        TcpClient tcpClient;
        Stream tcpStream;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipAdress"></param>
        public Client(String Address, int port)
		{
			try
			{
				IPHostEntry hostInfo = Dns.GetHostEntry(Address);
                tcpClient = new TcpClient();
                tcpClient.Connect(Address, port);


                //System.Net.IPEndPoint ep = new System.Net.IPEndPoint(IPAddress.Parse(Address),port);
                //IPEndPoint ep = new IPEndPoint(Convert.ToInt64(Address), port);
				//socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
				//socket.Connect(ep);
			}
			catch (SecurityException ex)
			{
				throw new Exception("Fehler beim Herstellen der Verbindung zum Server, evtl. verursacht durch eine Firewall oder ähnliche Schutzmechanismen",ex);
			}
			catch (Exception ex)
			{
				throw new Exception("Fehler beim Herstellen der Verbindung zum Server",ex);
			}
		}

		/// <summary>
		/// Sendet die übergebenen Daten.
		/// Fehler werden z.Zt. still akzeptiert, als Rückgabewert
		/// erfolgt dann 0
		/// </summary>
		/// <param name="data">Die zu übermittelnden Daten</param>
		/// <returns>Anzahl der übertragenen Bytes</returns>
		public int send(Byte[] data)
		{
			if (tcpClient == null) return 0;
			if (data==null) return 0;
			if (data.Length<=0) return 0;
			if (receiving) return 0;
			try
			{
                //int offset = 0;// Quelldaten-Offset
                //int lastsend = 0;// Anzahl der zuletzt übertragenen Bytes
                //int toSend = 0;// Anzahl der zu sendenden Bytes
                tcpStream = tcpClient.GetStream();
                tcpStream.Write(data, 0, data.Length);
                return data.Length;
			}
			catch (SocketException ex)
			{
				newMessage(ex.Message);
                tcpClient.Close();
                tcpClient = null;
				return 0;
			}
			catch (ObjectDisposedException ex)
			{
				newMessage(ex.Message);
                tcpClient.Close();
                tcpClient = null;
				return 0;
			}
			catch (Exception ex)
			{
				newMessage(ex.Message);
                tcpClient.Close();
                tcpClient = null;
				return 0;
			}
		}
        public void newMessage(String message)
        {
        }

        /// <summary>
        /// Schliesst eine offene Verbindung, falls nicht grade
        /// ein Empfang läuft.
        /// </summary>
        public void close()
		{
			if (receiving) return;
			if (tcpClient == null) return;
            byte[] msg = System.Text.Encoding.ASCII.GetBytes("quit");
            send(msg);
            tcpClient.Close();
            tcpClient = null;
		}

		/// <summary>
		/// Wartet auf Daten vom Server.
		/// Wurden innerhalb der Zeitspanne "TimeOut" (in ms) keine
		/// Daten empfangen, wird null zurückgegeben, ansonsten ein
		/// Byte-Array mit den empfangenen Daten.
		/// </summary>
		/// <returns>null oder ein Byte-Array mit empfangenen Daten</returns>
		public byte[] receive()
		{
			try
			{
				int cnt = 0;
				receiving=true;
				MemoryStream mem = new MemoryStream();// Empfangspuffer
				byte[] buffer = new byte[BufferSize];
				while (cnt<(TimeOut/SleepTime))
				{
					while (tcpClient.Available>0) 
					{
                        tcpStream = tcpClient.GetStream();
                        int bytesRead = tcpStream.Read(buffer, 0, buffer.Length);
						if (bytesRead<=0) continue;
						mem.Write(buffer,0,bytesRead);
					}
					Thread.Sleep(SleepTime);
					if (mem.Length>0 && tcpClient.Available==0)
					{
						//Console.WriteLine("Client: {0} bytes received",mem.Length);
						receiving = false;
						return mem.ToArray();
					} 
					else 
					{
						cnt++;
					}
				}
				receiving=false;
				return null;
			}
			catch
			{
				receiving=false;
				return null;
			}
		}
	}
}
