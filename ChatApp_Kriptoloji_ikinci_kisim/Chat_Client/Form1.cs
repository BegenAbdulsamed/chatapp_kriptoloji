using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_Client
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private StreamReader reader;
        private StreamWriter writer;
        private bool isConnected = false;
        private readonly object connectionLock = new object();

        public Form1()
        {
            InitializeComponent();
            this.FormClosing += Form1_FormClosing;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Şifreleme Türlerini Doldur
            cmbSifreleme.Items.Clear();
            cmbSifreleme.Items.AddRange(new string[] {
                "Manuel AES",
                "Lib AES",
                "Lib DES",
                "Lib RSA",
                "Lib ECC"
            });

            cmbSifreleme.SelectedIndex = 0;
            cmbSifreleme.SelectedIndexChanged += cmbSifreleme_SelectedIndexChanged;

            // Bit boyutlarını başlangıçta ayarla
            cmbSifreleme_SelectedIndexChanged(null, null);

            // Varsayılan değerler
            if (string.IsNullOrEmpty(txtIP.Text))
                txtIP.Text = "127.0.0.1";

            if (string.IsNullOrEmpty(txtPort.Text))
                txtPort.Text = "5000";

            if (string.IsNullOrEmpty(txtAnahtar.Text))
                txtAnahtar.Text = "1234567890123456";
        }

        private void cmbSifreleme_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbBitBoyutu == null || cmbSifreleme.SelectedItem == null)
                return;

            cmbBitBoyutu.Items.Clear();
            cmbBitBoyutu.Enabled = true;

            string secilenAlgo = cmbSifreleme.SelectedItem.ToString();

            switch (secilenAlgo)
            {
                case "Manuel AES":
                    // Manuel AES sadece 128 bit
                    cmbBitBoyutu.Items.AddRange(new string[] { "128" });
                    cmbBitBoyutu.SelectedIndex = 0;
                    cmbBitBoyutu.Enabled = false;
                    break;

                case "Lib AES":
                    // AES: 128, 192, 256
                    cmbBitBoyutu.Items.AddRange(new string[] { "128", "192", "256" });
                    cmbBitBoyutu.SelectedIndex = 0;
                    break;

                case "Lib RSA":
                    // RSA: 1024, 2048, 4096
                    cmbBitBoyutu.Items.AddRange(new string[] { "1024", "2048", "4096" });
                    cmbBitBoyutu.SelectedIndex = 1; // 2048 varsayılan
                    break;

                case "Lib ECC":
                    // ECC: 128 bit (P-256 eğrisi dengi)
                    cmbBitBoyutu.Items.AddRange(new string[] { "128" });
                    cmbBitBoyutu.SelectedIndex = 0;
                    cmbBitBoyutu.Enabled = false;
                    break;

                case "Lib DES":
                    // DES: 64 bit sabit (56 bit efektif + 8 bit parity)
                    cmbBitBoyutu.Items.AddRange(new string[] { "64" });
                    cmbBitBoyutu.SelectedIndex = 0;
                    cmbBitBoyutu.Enabled = false;
                    break;

                default:
                    cmbBitBoyutu.Items.AddRange(new string[] { "128" });
                    cmbBitBoyutu.SelectedIndex = 0;
                    break;
            }
        }

        private async void btnBaglan_Click_1(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtIP.Text))
                {
                    MessageBox.Show("Lütfen IP adresi girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(txtPort.Text, out int port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Lütfen geçerli bir port numarası girin (1-65535).", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnBaglan.Enabled = false;
                btnBaglan.Text = "Bağlanıyor...";

                client = new TcpClient();
                await client.ConnectAsync(txtIP.Text, port);

                NetworkStream stream = client.GetStream();
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                lock (connectionLock)
                {
                    isConnected = true;
                }

                EkranaYaz(">> Sunucuya bağlandı!");

                txtIP.Enabled = false;
                txtPort.Enabled = false;
                btnBaglan.Text = "Bağlı";

                // Mesaj dinleme görevi başlat
                _ = Task.Run(() => MesajDinle());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnBaglan.Enabled = true;
                btnBaglan.Text = "Bağlan";
                BaglantiyiKapat();
            }
        }

        private async void MesajDinle()
        {
            try
            {
                while (true)
                {
                    lock (connectionLock)
                    {
                        if (!isConnected) break;
                    }

                    string gelenPaket = await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(gelenPaket))
                    {
                        EkranaYaz(">> Sunucu bağlantısı kapandı.");
                        break;
                    }

                    gelenPaket = gelenPaket.Trim();

                    // ANAHTAR DEĞİŞİMİ PAKETİ
                    if (gelenPaket.StartsWith("KEY_EXCHANGE:"))
                    {
                        try
                        {
                            string yeniAnahtar = KriptoYoneticisi.RSA_Anahtar_Coz(gelenPaket);

                            this.Invoke((MethodInvoker)delegate
                            {
                                txtAnahtar.Text = yeniAnahtar;
                            });

                            EkranaYaz("\n===== ANAHTAR DEĞİŞİMİ =====");
                            EkranaYaz($"Yeni AES Anahtarı: {yeniAnahtar}");
                            EkranaYaz("============================\n");

                            this.Invoke((MethodInvoker)delegate
                            {
                                MessageBox.Show($"Karşı taraf şifreleme anahtarını değiştirdi!\n\nYeni Anahtar: {yeniAnahtar}",
                                    "Anahtar Değişimi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            });
                        }
                        catch (Exception ex)
                        {
                            EkranaYaz($"[HATA] Anahtar çözülemedi: {ex.Message}");
                        }
                        continue;
                    }

                    // NORMAL ŞİFRELİ MESAJ
                    string algoritma = "";
                    string anahtar = "";

                    this.Invoke((MethodInvoker)delegate
                    {
                        if (cmbSifreleme.SelectedItem != null)
                            algoritma = cmbSifreleme.SelectedItem.ToString();
                        anahtar = txtAnahtar.Text;
                    });

                    try
                    {
                        string cozulmusMesaj = KriptoYoneticisi.MetniCoz(gelenPaket, algoritma, anahtar);
                        EkranaYaz($"KARŞI TARAF: {cozulmusMesaj}");
                    }
                    catch (Exception ex)
                    {
                        EkranaYaz($"[HATA] Mesaj çözülemedi: {ex.Message}");
                        EkranaYaz($"[DEBUG] Paket: {gelenPaket.Substring(0, Math.Min(50, gelenPaket.Length))}...");
                    }
                }
            }
            catch (IOException)
            {
                EkranaYaz(">> Bağlantı koptu.");
            }
            catch (Exception ex)
            {
                EkranaYaz($">> Hata: {ex.Message}");
            }
            finally
            {
                BaglantiyiKapat();
                this.Invoke((MethodInvoker)delegate
                {
                    ToggleUI(true);
                });
            }
        }

        private void btnGonder_Click(object sender, EventArgs e)
        {
            lock (connectionLock)
            {
                if (!isConnected)
                {
                    MessageBox.Show("Önce sunucuya bağlanmalısınız!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(txtMesaj.Text))
            {
                MessageBox.Show("Lütfen göndermek için bir mesaj yazın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string hamMesaj = txtMesaj.Text.Trim();
                string secilenAlgo = cmbSifreleme.SelectedItem.ToString();
                string anahtar = txtAnahtar.Text;

                int bitDegeri = 128;
                if (cmbBitBoyutu.SelectedItem != null)
                {
                    if (!int.TryParse(cmbBitBoyutu.SelectedItem.ToString(), out bitDegeri))
                        bitDegeri = 128;
                }

                // Mesajı şifrele
                string sifreliMesaj = KriptoYoneticisi.MetniSifrele(hamMesaj, secilenAlgo, anahtar, bitDegeri);

                if (sifreliMesaj.StartsWith("Hata:"))
                {
                    MessageBox.Show(sifreliMesaj, "Şifreleme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Gönder
                writer.WriteLine(sifreliMesaj);

                // Ekrana yaz
                EkranaYaz($"BEN ({secilenAlgo}-{bitDegeri}): {hamMesaj}");

                // Mesaj kutusunu temizle
                txtMesaj.Clear();
                txtMesaj.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gönderme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BaglantiyiKapat();
            }
        }

        private void btnKeyExchange_Click(object sender, EventArgs e)
        {
            lock (connectionLock)
            {
                if (!isConnected)
                {
                    MessageBox.Show("Önce sunucuya bağlanmalısınız!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                string yeniAnahtar = txtAnahtar.Text;

                if (string.IsNullOrWhiteSpace(yeniAnahtar))
                {
                    MessageBox.Show("Lütfen bir anahtar girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // RSA ile şifrele ve KEY_EXCHANGE paketi oluştur
                string rsaPaketi = KriptoYoneticisi.RSA_Anahtar_Hazirla(yeniAnahtar);

                // Gönder
                writer.WriteLine(rsaPaketi);

                EkranaYaz("\n===== ANAHTAR GÖNDER İLDİ =====");
                EkranaYaz($"Yeni Anahtar: {yeniAnahtar}");
                EkranaYaz($"RSA Paketi: {rsaPaketi.Substring(0, Math.Min(40, rsaPaketi.Length))}...");
                EkranaYaz("================================\n");

                MessageBox.Show("Yeni anahtar RSA ile şifrelenerek gönderildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Anahtar gönderme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToggleUI(bool enable)
        {
            btnBaglan.Enabled = enable;
            btnBaglan.Text = enable ? "Bağlan" : "Bağlı";
            txtIP.Enabled = enable;
            txtPort.Enabled = enable;
        }

        private void EkranaYaz(string mesaj)
        {
            if (rtbChat.InvokeRequired)
            {
                rtbChat.Invoke(new Action<string>(EkranaYaz), mesaj);
            }
            else
            {
                rtbChat.AppendText($"[{DateTime.Now:HH:mm:ss}] {mesaj}\n");
                rtbChat.ScrollToCaret();
            }
        }

        private void BaglantiyiKapat()
        {
            lock (connectionLock)
            {
                isConnected = false;
            }

            try
            {
                reader?.Close();
                writer?.Close();
                client?.Close();
            }
            catch { }
            finally
            {
                reader = null;
                writer = null;
                client = null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            BaglantiyiKapat();
            KriptoYoneticisi.Dispose();
        }

        // Enter tuşu ile mesaj gönder
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && txtMesaj.Focused)
            {
                btnGonder_Click(null, null);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}