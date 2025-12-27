using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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

        public Form1()
        {
            InitializeComponent();
            cmbSifreleme.SelectedIndex = 0;
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private async void btnBaglan_Click_1(object sender, EventArgs e)
        {
            try
            {
                client = new TcpClient();
                string ip = txtIP.Text;
                int port = int.Parse(txtPort.Text);

                await client.ConnectAsync(ip, port);

                var stream = client.GetStream();
                reader = new StreamReader(stream, Encoding.UTF8);
                writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                isConnected = true;
                rtbChat.AppendText(">> Sunucuya Bağlanıldı!\n");

                btnBaglan.Enabled = false;
                txtIP.Enabled = false;
                txtPort.Enabled = false;

                Task.Run(() => MesajDinle());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Bağlantı Hatası: " + ex.Message);
            }
        }

        private async void MesajDinle()
        {
            try
            {
                while (isConnected)
                {
                    string gelenSifreliMesaj = await reader.ReadLineAsync();
                    if (gelenSifreliMesaj == null) break;

                    string varsayilanAlgo = "";
                    string kullaniciAnahtari = "";


                    this.Invoke((MethodInvoker)delegate {
                        varsayilanAlgo = cmbSifreleme.SelectedItem.ToString();
                        kullaniciAnahtari = txtAnahtar.Text;
                    });

                    string cozulmusMesaj = KriptoYoneticisi.MetniCoz(gelenSifreliMesaj, varsayilanAlgo, kullaniciAnahtari);

                    EkranaYaz($"KARŞI TARAF: {cozulmusMesaj} [Şifreli: {gelenSifreliMesaj}]");
                }
            }
            catch
            {
                EkranaYaz(">> Bağlantı koptu.");
                isConnected = false;
            }
        }

        private void EkranaYaz(string mesaj)
        {
            if (rtbChat.InvokeRequired)
            {
                rtbChat.Invoke(new Action<string>(EkranaYaz), mesaj);
            }
            else
            {
                rtbChat.AppendText(mesaj + "\n");
                rtbChat.ScrollToCaret();
            }
        }

        private void btnGonder_Click(object sender, EventArgs e)
        {
            if(isConnected && !string.IsNullOrWhiteSpace(txtMesaj.Text))
    {
                string secilenAlgo = cmbSifreleme.SelectedItem.ToString();
                string hamMesaj = txtMesaj.Text;

                string anahtar = txtAnahtar.Text;

     
                string sifreliMesaj = KriptoYoneticisi.MetniSifrele(hamMesaj, secilenAlgo, anahtar);

                writer.WriteLine(sifreliMesaj);

                EkranaYaz($"BEN ({secilenAlgo} - Key:{anahtar}): {hamMesaj} [Giden: {sifreliMesaj}]");
                txtMesaj.Clear();
            }
        }
    }
}
