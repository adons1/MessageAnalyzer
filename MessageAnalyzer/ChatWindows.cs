using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MessageAnalyzer
{
    public partial class ChatWindows : Form
    {
        public ChatWindows()
        {
            InitializeComponent();
        }

        TcpClient tcpClient;
        user user = new user();
        NetworkStream stream;

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
            XDocument  xml = XDocument.Parse(text);
            XmlSerializer s = new XmlSerializer(typeof(user));
            return (user)s.Deserialize(xml.CreateReader());
        }
        private void ReaderThread(object serverConnection)
        {
            NetworkStream stream = (NetworkStream)serverConnection;
            while (true)
            {
                string message = rw.Read(stream);
                try
                {
                    chatField.Invoke((Action)delegate
                    {
                        user user = Deserialize(message);
                        chatField.Items.Add(string.Format("{0}:\t{1}\r\n", user.name, user.message));
                    });
                }
                catch
                {

                }
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //адрес сервера и порт
            tcpClient = new TcpClient();

            tcpClient.Connect(IPAddress.Parse("127.0.0.1"), 5222);

            stream = tcpClient.GetStream();

            Thread readerThread = new Thread(ReaderThread);
            readerThread.Start(stream);
        }

        private void sendMessage_Click(object sender, EventArgs e)
        {
            user.message = textBox1.Text;
            rw.Write(stream, Serialize(user));

            textBox1.Text = string.Empty;
        }

        private void SendOnEnter(object sender, EventArgs e)
        {
            user.message = textBox1.Text;

            rw.Write(stream, Serialize(user));

            textBox1.Text = string.Empty;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                sendMessage_Click(this, new EventArgs());
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Visible = true;
            sendMessage.Visible = true;
            chatField.Visible = true;

            textBox2.Visible = false;
            button1.Visible = false;
            label1.Visible = false;

            user.name = textBox2.Text;
            
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
            do
            {
                bytes = stream.Read(data, 0, data.Length);
                stringBuilder.Append(Encoding.UTF8.GetString(data, 0, bytes));
            }
            while (stream.DataAvailable);

            return stringBuilder.ToString();
        }
        public static void Write(NetworkStream stream, string response)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(response);
            stream.Write(data, 0, data.Length);
        }
    }
}
