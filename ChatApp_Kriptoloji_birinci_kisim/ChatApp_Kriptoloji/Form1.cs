using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ChatApp_Kriptoloji
{
    public partial class Form1 : Form
    {
        private List<TcpClient> clientList = new List<TcpClient>();
        private TcpListener serverListener;
        private bool isServerRunning = false;
        public Form1()
        {
            InitializeComponent();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
           
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
           
        }

        private void btnBaslat_Click(object sender, EventArgs e)
        {
            if (!isServerRunning)
            {
                int port;
                if (int.TryParse(txtPort.Text, out port))
                {

                    serverListener = new TcpListener(IPAddress.Any, port);
                    serverListener.Start();
                    isServerRunning = true;

                    LogEkle($"Server {port} portunda başlatıldı. İstemciler bekleniyor...");
                    btnBaslat.Enabled = false; 

                   
                    Task.Run(() => ClientKabulEt());
                }
                else
                {
                    MessageBox.Show("Lütfen geçerli bir port numarası girin.");
                }
            }
        }
        private async void ClientKabulEt()
        {
            try
            {
                while (isServerRunning)
                {
             
                    TcpClient yeniClient = await serverListener.AcceptTcpClientAsync();

                 
                    clientList.Add(yeniClient);
                    LogEkle("Yeni bir istemci bağlandı!");

                    _ = Task.Run(() => ClientDinle(yeniClient));
                }
            }
            catch (Exception ex)
            {
                LogEkle("Hata (ClientKabulEt): " + ex.Message);
            }
        }
        private void ClientDinle(TcpClient client)
        {
            StreamReader reader = null;
            try
            {

                reader = new StreamReader(client.GetStream());

                while (client.Connected)
                {

                    string gelenMesaj = reader.ReadLine();

                    if (gelenMesaj == null) break; 


                    LogEkle("Gelen Şifreli Mesaj: " + gelenMesaj);

                    DigerlerineGonder(gelenMesaj, client);
                }
            }
            catch
            {
                
            }
            finally
            {

                clientList.Remove(client);
                client.Close();
                LogEkle("Bir istemci ayrıldı.");
            }
        }
        private void DigerlerineGonder(string mesaj, TcpClient gonderen)
        {

            lock (clientList)
            {
                foreach (TcpClient c in clientList)
                {

                    if (c != gonderen && c.Connected)
                    {
                        try
                        {
                            StreamWriter writer = new StreamWriter(c.GetStream());
                            writer.WriteLine(mesaj);
                            writer.AutoFlush = true; 
                        }
                        catch
                        {
    
                        }
                    }
                }
            }
        }

        private void LogEkle(string metin)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(LogEkle), metin);
            }
            else
            {
                rtbLog.AppendText($"[{DateTime.Now.ToLongTimeString()}] {metin}{Environment.NewLine}");
                rtbLog.ScrollToCaret();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}

