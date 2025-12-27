using System;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp_Kriptoloji
{
    internal class KriptoYoneticisi
    {
        // RSA Anahtar Çifti - Gerçek uygulama için kalıcı anahtarlar kullanılmalı
        private static RSACryptoServiceProvider _rsaProvider = null;
        private static readonly object _rsaLock = new object();

        // RSA Provider'ı başlat
        private static RSACryptoServiceProvider GetRSAProvider(int keySize = 2048)
        {
            lock (_rsaLock)
            {
                if (_rsaProvider == null || _rsaProvider.KeySize != keySize)
                {
                    _rsaProvider?.Dispose();
                    _rsaProvider = new RSACryptoServiceProvider(keySize);
                }
                return _rsaProvider;
            }
        }

        // ==========================================
        // ANAHTAR DAĞITIMI (KEY EXCHANGE)
        // ==========================================

        public static string RSA_Anahtar_Hazirla(string gonderilecekAESAnahtari)
        {
            try
            {
                // RSA ile AES anahtarını şifrele
                string sifreliVeri = SifreleRSA(gonderilecekAESAnahtari, 2048);
                // KEY_EXCHANGE formatına çevir
                return sifreliVeri.Replace("RSA-2048:", "KEY_EXCHANGE:");
            }
            catch (Exception ex)
            {
                throw new Exception("RSA Anahtar Hazırlama Hatası: " + ex.Message);
            }
        }

        public static string RSA_Anahtar_Coz(string sifreliPaket)
        {
            try
            {
                if (string.IsNullOrEmpty(sifreliPaket))
                    throw new ArgumentException("Şifreli paket boş olamaz");

                // KEY_EXCHANGE: prefix'ini kaldır
                string payload = sifreliPaket.Replace("KEY_EXCHANGE:", "");

                // Base64'ten byte array'e çevir
                byte[] encryptedData = Convert.FromBase64String(payload);

                // RSA ile çöz
                lock (_rsaLock)
                {
                    RSACryptoServiceProvider rsa = GetRSAProvider(2048);
                    byte[] decryptedData = rsa.Decrypt(encryptedData, false);
                    return Encoding.UTF8.GetString(decryptedData);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("RSA Anahtar Çözme Hatası: " + ex.Message);
            }
        }

        // ==========================================
        // ŞİFRELEME METODLARi
        // ==========================================

        public static string SifreleLibAES(string veri, string anahtar, int bitSize)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = bitSize;
                int byteSize = bitSize / 8;

                byte[] keyBytes = new byte[byteSize];
                byte[] tempKey = Encoding.UTF8.GetBytes(anahtar);
                Array.Copy(tempKey, keyBytes, Math.Min(tempKey.Length, byteSize));

                aes.Key = keyBytes;
                aes.IV = new byte[16]; // Sıfır IV (Eğitim amaçlı - gerçek uygulamada rastgele IV kullanın)
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] input = Encoding.UTF8.GetBytes(veri);
                    byte[] output = encryptor.TransformFinalBlock(input, 0, input.Length);
                    return $"AES-{bitSize}:" + Convert.ToBase64String(output);
                }
            }
        }

        public static string CozLibAES(string veri, string anahtar, int bitSize)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = bitSize;
                int byteSize = bitSize / 8;

                byte[] keyBytes = new byte[byteSize];
                byte[] tempKey = Encoding.UTF8.GetBytes(anahtar);
                Array.Copy(tempKey, keyBytes, Math.Min(tempKey.Length, byteSize));

                aes.Key = keyBytes;
                aes.IV = new byte[16];
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    byte[] input = Convert.FromBase64String(veri);
                    byte[] output = decryptor.TransformFinalBlock(input, 0, input.Length);
                    return Encoding.UTF8.GetString(output);
                }
            }
        }

        public static string SifreleManuelAES(string veri, string anahtar)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");
            return ManuelAES.Encrypt(veri, anahtar);
        }

        public static string CozManuelAES(string veri, string anahtar)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");
            return ManuelAES.Decrypt(veri, anahtar);
        }

        public static string SifreleLibDES(string veri, string anahtar)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");

            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] keyBytes = MD5Hash(anahtar);
                des.Key = keyBytes;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.PKCS7;

                byte[] buffer = Encoding.UTF8.GetBytes(veri);
                using (ICryptoTransform encryptor = des.CreateEncryptor())
                {
                    byte[] result = encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
                    return "DES:" + Convert.ToBase64String(result);
                }
            }
        }

        public static string CozLibDES(string veri, string anahtar)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");

            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] keyBytes = MD5Hash(anahtar);
                des.Key = keyBytes;
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.PKCS7;

                byte[] buffer = Convert.FromBase64String(veri);
                using (ICryptoTransform decryptor = des.CreateDecryptor())
                {
                    byte[] result = decryptor.TransformFinalBlock(buffer, 0, buffer.Length);
                    return Encoding.UTF8.GetString(result);
                }
            }
        }

        public static string SifreleRSA(string veri, int keySize)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");

            lock (_rsaLock)
            {
                RSACryptoServiceProvider rsa = GetRSAProvider(keySize);
                byte[] input = Encoding.UTF8.GetBytes(veri);

                // RSA max veri boyutu kontrolü
                int maxDataSize = (keySize / 8) - 11; // PKCS#1 padding için
                if (input.Length > maxDataSize)
                    throw new ArgumentException($"Veri çok büyük. Max {maxDataSize} byte olmalı.");

                byte[] output = rsa.Encrypt(input, false);
                return $"RSA-{keySize}:" + Convert.ToBase64String(output);
            }
        }

        public static string SifreleECC(string veri)
        {
            if (string.IsNullOrEmpty(veri))
                throw new ArgumentException("Veri boş olamaz");

            // ECC simülasyonu - AES ile şifreleme
            return "ECC:" + SifreleLibAES(veri, "ECC_SECRET_KEY_123", 128);
        }

        private static byte[] MD5Hash(string text)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
                byte[] result = new byte[8];
                Array.Copy(hash, result, 8);
                return result;
            }
        }

        // ==========================================
        // YÖNETİCİ METODLAR
        // ==========================================

        public static string MetniSifrele(string metin, string algoritma, string anahtar, int bitBoyutu)
        {
            try
            {
                if (string.IsNullOrEmpty(metin))
                    throw new ArgumentException("Metin boş olamaz");

                switch (algoritma)
                {
                    case "Manuel AES":
                        return SifreleManuelAES(metin, anahtar);

                    case "Lib AES":
                        return SifreleLibAES(metin, anahtar, bitBoyutu);

                    case "Lib DES":
                        return SifreleLibDES(metin, anahtar);

                    case "Lib RSA":
                        return SifreleRSA(metin, bitBoyutu);

                    case "Lib ECC":
                        return SifreleECC(metin);

                    default:
                        throw new ArgumentException("Bilinmeyen algoritma: " + algoritma);
                }
            }
            catch (Exception ex)
            {
                return "Hata: " + ex.Message;
            }
        }

        public static string MetniCoz(string metin, string algoritma, string anahtar)
        {
            try
            {
                if (string.IsNullOrEmpty(metin))
                    throw new ArgumentException("Metin boş olamaz");

                // Prefix kontrolü
                if (metin.StartsWith("AES-128:"))
                    return CozLibAES(metin.Substring(8), anahtar, 128);

                if (metin.StartsWith("AES-192:"))
                    return CozLibAES(metin.Substring(8), anahtar, 192);

                if (metin.StartsWith("AES-256:"))
                    return CozLibAES(metin.Substring(8), anahtar, 256);

                if (metin.StartsWith("DES:"))
                    return CozLibDES(metin.Substring(4), anahtar);

                if (metin.StartsWith("RSA-"))
                {
                    string sizeStr = metin.Split(':')[0].Replace("RSA-", "");
                    return $"[RSA Şifreli Veri - Boyut: {sizeStr} bit]";
                }

                if (metin.StartsWith("ECC:"))
                {
                    string eccData = metin.Substring(4);
                    if (eccData.StartsWith("AES-128:"))
                        return CozLibAES(eccData.Substring(8), "ECC_SECRET_KEY_123", 128);
                    return "[ECC Şifreli Veri]";
                }

                // Algoritma bazlı çözme
                switch (algoritma)
                {
                    case "Manuel AES":
                        return CozManuelAES(metin, anahtar);

                    case "Lib DES":
                        return CozLibDES(metin, anahtar);

                    default:
                        return "[Bilinmeyen Format]";
                }
            }
            catch (Exception ex)
            {
                return "Çözme Hatası: " + ex.Message;
            }
        }

        // Cleanup metodu
        public static void Dispose()
        {
            lock (_rsaLock)
            {
                _rsaProvider?.Dispose();
                _rsaProvider = null;
            }
        }
    }
}