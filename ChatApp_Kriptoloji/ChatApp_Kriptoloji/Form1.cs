using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatApp_Kriptoloji
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private TcpListener listener;
        public StreamReader STR;
        public StreamWriter STW;
        public string recieve;
        public string TextToSend;

        public Form1()
        {
            InitializeComponent();
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker2.WorkerSupportsCancellation = true;

            IPAddress[] localIP = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress address in localIP)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    ServerIPtextBox.Text = address.ToString();
                    break;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                int port = int.Parse(ServerPORTtextBox.Text);
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                SohbetEkranitextBox.AppendText($"Sunucu {port} portunda dinleniyor...{Environment.NewLine}");

                Task.Run(() =>
                {
                    try
                    {
                        TcpClient accepted = listener.AcceptTcpClient();
                        this.Invoke((MethodInvoker)delegate
                        {
                            client = accepted;
                            SohbetEkranitextBox.AppendText("Client1 bağlandı!" + Environment.NewLine);

                            STR = new StreamReader(client.GetStream(), Encoding.UTF8);
                            STW = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };

                            if (!backgroundWorker1.IsBusy)
                                backgroundWorker1.RunWorkerAsync();
                        });
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            MesajtextBox.Text = "Listener hata: " + ex.Message;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                MesajtextBox.Text = ex.Message.ToString();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            client = new TcpClient();
            System.Net.IPEndPoint ipEnd = new System.Net.IPEndPoint(
                System.Net.IPAddress.Parse(ClientIPtextBox.Text.Trim()),
                Convert.ToInt32(ClientPORTtextBox.Text.Trim())
            );

            try
            {
                SohbetEkranitextBox.AppendText("Servera bağlanıyor..." + Environment.NewLine);
                client.Connect(ipEnd);

                STW = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true };
                STR = new StreamReader(client.GetStream(), Encoding.UTF8);

                if (!backgroundWorker1.IsBusy)
                    backgroundWorker1.RunWorkerAsync();

                SohbetEkranitextBox.AppendText("Client2 servera bağlandı!" + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MesajtextBox.Text = ex.Message.ToString();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            try
            {
                while (client != null && client.Connected && !worker.CancellationPending)
                {
                    string line = STR.ReadLine();
                    if (line == null)
                        break;

                    this.SohbetEkranitextBox.Invoke((MethodInvoker)delegate ()
                    {
                        SohbetEkranitextBox.AppendText(line + Environment.NewLine);
                    });
                }
            }
            catch (Exception ex)
            {
                this.Invoke(new MethodInvoker(delegate ()
                {
                    MesajtextBox.Text = ex.Message.ToString();
                }));
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (client != null && client.Connected)
                {
                    // Mesajın başına kendi ismini ekle
                    string senderName = listener != null ? "Client1" : "Client2";
                    string mesaj = senderName + " : " + TextToSend;

                    STW.WriteLine(mesaj);

                    this.SohbetEkranitextBox.Invoke((MethodInvoker)(() =>
                    {
                        SohbetEkranitextBox.AppendText(mesaj + Environment.NewLine);
                    }));
                }
                else
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        MessageBox.Show("Gönderilemedi - bağlantı yok");
                    }));
                }
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    MesajtextBox.Text = ex.Message.ToString();
                }));
            }
        }
        private void label1_Click(object sender, EventArgs e) { }
        private void button3_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(MesajtextBox.Text))
            {
                TextToSend = MesajtextBox.Text;

                if (!backgroundWorker2.IsBusy)
                    backgroundWorker2.RunWorkerAsync();
                else
                    MessageBox.Show("Önceki gönderim devam ediyor...");
            }
            MesajtextBox.Text = "";
        }
    }
}
