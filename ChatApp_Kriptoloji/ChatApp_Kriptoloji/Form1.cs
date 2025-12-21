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
                    // 1. Server'ı verilen portta başlatıyoruz
                    serverListener = new TcpListener(IPAddress.Any, port);
                    serverListener.Start();
                    isServerRunning = true;

                    LogEkle($"Server {port} portunda başlatıldı. İstemciler bekleniyor...");
                    btnBaslat.Enabled = false; // İkinci kez basılmasın

                    // 2. Ana arayüz donmasın diye Client kabul etme işini arka plana (Task) atıyoruz
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
                    // Yeni birisi bağlanana kadar burada bekler
                    TcpClient yeniClient = await serverListener.AcceptTcpClientAsync();

                    // Bağlanan kişiyi listeye ekle
                    clientList.Add(yeniClient);
                    LogEkle("Yeni bir istemci bağlandı!");

                    // Bu istemci için özel bir dinleme döngüsü başlat (Her client için ayrı Task)
                    // Discard ( _ = ) kullanarak await beklemeden devam etmesini sağlıyoruz
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
                // Client'ın ağ akışını okumak için
                reader = new StreamReader(client.GetStream());

                while (client.Connected)
                {
                    // Mesaj gelmesini bekle
                    string gelenMesaj = reader.ReadLine();

                    if (gelenMesaj == null) break; // Bağlantı koptuysa döngüden çık

                    // 1. Server ekranına (Log) şifreli halini bas
                    LogEkle("Gelen Şifreli Mesaj: " + gelenMesaj);

                    // 2. Diğer herkese dağıt
                    DigerlerineGonder(gelenMesaj, client);
                }
            }
            catch
            {
                // Hata olursa (client aniden kapatırsa) buraya düşer
            }
            finally
            {
                // Temizlik işlemleri
                clientList.Remove(client);
                client.Close();
                LogEkle("Bir istemci ayrıldı.");
            }
        }
        private void DigerlerineGonder(string mesaj, TcpClient gonderen)
        {
            // Listeyi kilitliyoruz ki işlem sırasında başka biri bağlanıp listeyi bozmasın
            lock (clientList)
            {
                foreach (TcpClient c in clientList)
                {
                    // Mesajı gönderen kişiye tekrar geri yollama, diğerlerine yolla
                    if (c != gonderen && c.Connected)
                    {
                        try
                        {
                            StreamWriter writer = new StreamWriter(c.GetStream());
                            writer.WriteLine(mesaj);
                            writer.AutoFlush = true; // Tamponda bekletmeden hemen yolla
                        }
                        catch
                        {
                            // Gönderim başarısız olursa (örn. bağlantı koptuysa) hatayı yut
                        }
                    }
                }
            }
        }

        // ARAYÜZE GÜVENLİ ERİŞİM (LOGLAMA)
        // Thread'lerden GUI'ye erişirken hata almamak için Invoke kullanıyoruz
        private void LogEkle(string metin)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string>(LogEkle), metin);
            }
            else
            {
                rtbLog.AppendText($"[{DateTime.Now.ToLongTimeString()}] {metin}{Environment.NewLine}");
                rtbLog.ScrollToCaret(); // En son satıra kaydır
            }
        }
    }
}

