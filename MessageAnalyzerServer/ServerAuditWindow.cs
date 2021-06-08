using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.IO;

namespace MessageAnalyzerServer
{
    public partial class ServerAuditWindow : Form
    {
        public ServerAuditWindow()
        {
            InitializeComponent();
        }

        TcpListener tcpServer;
        Thread serverThread, readThread;
        int quantity_of_messages=0;
        List<TcpClient> connectedClients = new List<TcpClient>();
        private void SendToOthers(string message)
        {
            foreach (var user in connectedClients)
            {
                try
                {
                    var stream = user.GetStream();

                    rw.Write(stream, message);
                }
                catch
                {
                }
            }
        }
        private string Serialize(user user)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(user));

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, user);
                return textWriter.ToString();
            }
        }
        private user Deserialize(string text)
        {
            XDocument xml = XDocument.Parse(text);
            XmlSerializer s = new XmlSerializer(typeof(user));
            return (user)s.Deserialize(xml.CreateReader());
        }
        private void ReadThreadFunction(object connectedClient)
        {
            TcpClient client = (TcpClient)connectedClient;

            connectedClients.Add(client);
            try
            {
                while (true)
                {
                    var stream = client.GetStream();

                    string message = rw.Read(stream);

                    SendToOthers(message);

                    user user = Deserialize(message);

                    string[] words = textBox3.Text.Split(' ');

                    textBox1.Invoke((Action)delegate
                    {
                        foreach(var word in words)
                        {
                            int position = user.message.IndexOf(word);

                            if ((position >= 0)&&(word.Length>0))
                            {
                                textBox2.Text += string.Format("Пользователь {0} - номер сообщения {1} строка {2}:{3}\r\n",user.name, quantity_of_messages, position, word);
                            }
                        }
                        textBox1.Text += string.Format("{0}.){1}:\t{2}\r\n", quantity_of_messages, user.name, user.message);
                    });
                    quantity_of_messages++;
                }
            }
            catch
            {

            }
        }
        private void Server()
        {
            tcpServer = new TcpListener(IPAddress.Parse("127.0.0.1"), 5222);

            tcpServer.Start();
            while (true)
            {
                System.Net.Sockets.TcpClient client = tcpServer.AcceptTcpClient();

                readThread = new Thread(ReadThreadFunction);
                readThread.Start(client);
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            serverThread = new Thread(Server);
            serverThread.Start();
        }
    }
    [Serializable]
    public class user
    {
        public string name;
        public string message;
    }
    abstract class rw
    {
        public static string Read(NetworkStream stream)
        {
            StringBuilder stringBuilder = new StringBuilder();
            byte[] data = new byte[1460];
            int bytes = 0;
            try
            {
                do
                {
                    bytes = stream.Read(data, 0, data.Length);
                    stringBuilder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);
            }
            catch
            {

            }
            return stringBuilder.ToString();
        }
        public static void Write(NetworkStream stream, string response)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(response);
            stream.Write(data, 0, data.Length);
        }
    }
}
