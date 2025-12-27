using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatApp_Kriptoloji
{
    public partial class Form1 : Form
    {
        private List<TcpClient> clientList = new List<TcpClient>();
        private TcpListener serverListener;
        private bool isServerRunning = false;
        private readonly object clientListLock = new object();

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Varsayılan değerler
            if (string.IsNullOrEmpty(txtPort.Text))
                txtPort.Text = "5000";

            if (string.IsNullOrEmpty(txtServerKey.Text))
                txtServerKey.Text = "1234567890123456";

            LogEkle("Sunucu başlatılmaya hazır.");
        }

        private void btnBaslat_Click(object sender, EventArgs e)
        {
            if (isServerRunning)
            {
                MessageBox.Show("Sunucu zaten çalışıyor!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Lütfen geçerli bir port numarası girin (1-65535).", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                serverListener = new TcpListener(IPAddress.Any, port);
                serverListener.Start();
                isServerRunning = true;

                LogEkle($"===== SUNUCU BAŞLATILDI =====");
                LogEkle($"Port: {port}");
                LogEkle($"Şifreleme Anahtarı: {txtServerKey.Text}");
                LogEkle($"İstemciler bekleniyor...");
                LogEkle($"==============================");

                btnBaslat.Enabled = false;
                btnBaslat.Text = "Çalışıyor";
                txtPort.Enabled = false;

                // İstemci kabul etme görevi başlat
                _ = Task.Run(() => ClientKabulEt());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucu başlatma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogEkle($"HATA: Sunucu başlatılamadı - {ex.Message}");
            }
        }

        private async void ClientKabulEt()
        {
            try
            {
                while (isServerRunning)
                {
                    TcpClient yeniClient = await serverListener.AcceptTcpClientAsync();

                    lock (clientListLock)
                    {
                        clientList.Add(yeniClient);
                    }

                    string clientEndpoint = yeniClient.Client.RemoteEndPoint.ToString();
                    LogEkle($">> Yeni istemci bağlandı: {clientEndpoint}");
                    LogEkle($">> Toplam bağlı istemci: {clientList.Count}");

                    // Her istemci için ayrı dinleme görevi
                    _ = Task.Run(() => ClientDinle(yeniClient));
                }
            }
            catch (ObjectDisposedException)
            {
                LogEkle("Sunucu durduruldu.");
            }
            catch (Exception ex)
            {
                if (isServerRunning)
                    LogEkle($"HATA (ClientKabulEt): {ex.Message}");
            }
        }

        private void ClientDinle(TcpClient client)
        {
            StreamReader reader = null;
            string clientEndpoint = "Bilinmeyen";

            try
            {
                clientEndpoint = client.Client.RemoteEndPoint.ToString();
                NetworkStream stream = client.GetStream();
                reader = new StreamReader(stream, Encoding.UTF8);

                while (client.Connected && isServerRunning)
                {
                    // Şifreli mesajı oku
                    string gelenMesaj = reader.ReadLine();

                    if (string.IsNullOrEmpty(gelenMesaj))
                        break;

                    gelenMesaj = gelenMesaj.Trim();

                    // Sunucu anahtarını al
                    string serverKey = "";
                    this.Invoke((MethodInvoker)delegate
                    {
                        serverKey = txtServerKey.Text;
                    });

                    // ANAHTAR DEĞİŞİMİ PAKETİ KONTROLÜ
                    if (gelenMesaj.StartsWith("KEY_EXCHANGE:"))
                    {
                        try
                        {
                            // RSA paketini çöz
                            string yeniAnahtar = KriptoYoneticisi.RSA_Anahtar_Coz(gelenMesaj);

                            // Sunucu anahtarını güncelle
                            this.Invoke((MethodInvoker)delegate
                            {
                                txtServerKey.Text = yeniAnahtar;
                            });

                            LogEkle("=====================================");
                            LogEkle($"!!! ANAHTAR DEĞİŞİMİ - İstemci: {clientEndpoint}");
                            LogEkle($"!!! Yeni AES Anahtarı: {yeniAnahtar}");
                            LogEkle($"!!! Sunucu anahtarı otomatik güncellendi");
                            LogEkle("=====================================");

                            // Diğer istemcilere KEY_EXCHANGE paketini ilet
                            DigerlerineGonder(gelenMesaj, client);
                            continue;
                        }
                        catch (Exception ex)
                        {
                            LogEkle($"HATA: Anahtar çözülemedi - {ex.Message}");
                            continue;
                        }
                    }

                    // NORMAL ŞİFRELİ MESAJ
                    string cozulmusMesaj = "[Çözülemedi]";
                    try
                    {
                        cozulmusMesaj = KriptoYoneticisi.MetniCoz(gelenMesaj, "Manuel AES", serverKey);
                    }
                    catch (Exception ex)
                    {
                        cozulmusMesaj = $"[Çözme Hatası: {ex.Message}]";
                    }

                    // Log'a yaz
                    LogEkle($"İstemci [{clientEndpoint}]:");
                    LogEkle($"  Şifreli: {gelenMesaj.Substring(0, Math.Min(40, gelenMesaj.Length))}...");
                    LogEkle($"  Çözülmüş: {cozulmusMesaj}");

                    // Diğer istemcilere ilet
                    DigerlerineGonder(gelenMesaj, client);
                }
            }
            catch (IOException)
            {
                LogEkle($"İstemci bağlantısı koptu: {clientEndpoint}");
            }
            catch (Exception ex)
            {
                LogEkle($"HATA (ClientDinle - {clientEndpoint}): {ex.Message}");
            }
            finally
            {
                // İstemciyi listeden çıkar
                lock (clientListLock)
                {
                    if (clientList.Contains(client))
                        clientList.Remove(client);
                }

                // Bağlantıyı kapat
                try
                {
                    reader?.Close();
                    client?.Close();
                }
                catch { }

                LogEkle($"İstemci ayrıldı: {clientEndpoint}");
                LogEkle($"Kalan istemci sayısı: {clientList.Count}");
            }
        }

        private void DigerlerineGonder(string mesaj, TcpClient gonderen)
        {
            List<TcpClient> clientsCopy;

            lock (clientListLock)
            {
                clientsCopy = new List<TcpClient>(clientList);
            }

            foreach (TcpClient client in clientsCopy)
            {
                if (client == gonderen)
                    continue;

                if (!client.Connected)
                {
                    lock (clientListLock)
                    {
                        clientList.Remove(client);
                    }
                    continue;
                }

                try
                {
                    NetworkStream stream = client.GetStream();
                    StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                    writer.WriteLine(mesaj);
                }
                catch (Exception ex)
                {
                    LogEkle($"HATA (Gönderme): {ex.Message}");

                    lock (clientListLock)
                    {
                        if (clientList.Contains(client))
                            clientList.Remove(client);
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
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {metin}\n");
                rtbLog.ScrollToCaret();
            }
        }

        private void SunucuyuDurdur()
        {
            if (!isServerRunning)
                return;

            LogEkle("Sunucu durduruluyor...");
            isServerRunning = false;

            try
            {
                // Tüm istemcileri kapat
                lock (clientListLock)
                {
                    foreach (TcpClient client in clientList)
                    {
                        try
                        {
                            client.Close();
                        }
                        catch { }
                    }
                    clientList.Clear();
                }

                // Listener'ı durdur
                serverListener?.Stop();

                LogEkle("Sunucu durduruldu.");
            }
            catch (Exception ex)
            {
                LogEkle($"HATA (Durdurma): {ex.Message}");
            }
            finally
            {
                this.Invoke((MethodInvoker)delegate
                {
                    btnBaslat.Enabled = true;
                    btnBaslat.Text = "Sunucuyu Başlat";
                    txtPort.Enabled = true;
                });
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SunucuyuDurdur();
            KriptoYoneticisi.Dispose();
        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // Kullanılmıyor - ileride kullanılabilir
        }

        private void backgroundWorker2_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            // Kullanılmıyor - ileride kullanılabilir
        }
    }
}