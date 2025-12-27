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
                    // 1. Şifreli mesajı hat üzerinden oku
                    string gelenMesaj = reader.ReadLine();
                    if (gelenMesaj == null) break;

                    // 2. Arayüzden (TextBox) anahtarı almamız lazım.
                    // Form elemanlarına thread içinden erişmek için Invoke kullanıyoruz.
                    string serverKey = "";
                    this.Invoke((MethodInvoker)delegate {
                        serverKey = txtServerKey.Text; // Tasarıma eklediğin kutunun adı
                    });

                    // 3. Mesajı Çözmeyi Dene
                    // Ödevde varsayılan olarak "Manuel AES" ile çözüldüğünü varsayıyoruz.
                    // İstersen burayı "Lib RSA" vs. diye de deneyebilirsin.
                    string cozulmusMesaj = "Çözülemedi";
                    try
                    {
                        // KriptoYoneticisi sunucu projesinde de olmalı!
                        cozulmusMesaj = KriptoYoneticisi.MetniCoz(gelenMesaj, "Manuel AES", serverKey);
                    }
                    catch
                    {
                        cozulmusMesaj = "HATA";
                    }

                    // 4. Log Ekranına Yaz (Hem şifreliyi hem çözülmüşü gör)
                    LogEkle($"GELEN PAKET: {gelenMesaj}");
                    LogEkle($"-> İÇERİK: {cozulmusMesaj}");

                    // 5. Diğer istemcilere olduğu gibi (şifreli) ilet
                    DigerlerineGonder(gelenMesaj, client);
                }
            }
            catch (Exception ex)
            {
                LogEkle("Bağlantı Hatası: " + ex.Message);
            }
            finally
            {
                if (clientList.Contains(client))
                {
                    clientList.Remove(client);
                }
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

