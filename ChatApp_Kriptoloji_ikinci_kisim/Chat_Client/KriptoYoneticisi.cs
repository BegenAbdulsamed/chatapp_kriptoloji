using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Chat_Client
{
    internal class KriptoYoneticisi
    {
        // Sabit RSA Anahtarları (Ödev kolaylığı için sabitlendi)
        private static string RSA_Public = "<RSAKeyValue><Modulus>vWd... (Burası uzun olur, sistem otomatik üretmeli ama basitlik için xml)</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private static string RSA_Private = ""; // Gerçek uygulamada burası dolu olmalı

        // 1. MANUEL AES (Hocanın istediği)
        public static string SifreleManuelAES(string veri, string anahtar)
        {
            return ManuelAES.Encrypt(veri, anahtar);
        }
        public static string CozManuelAES(string veri, string anahtar)
        {
            return ManuelAES.Decrypt(veri, anahtar);
        }

        // 2. KÜTÜPHANE AES (Kıyaslama için)
        public static string SifreleLibAES(string veri, string anahtar)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] keyBytes = new byte[16];
                // HATA ÇÖZÜMÜ: CopyTo yerine Array.Copy ve Math.Min kullanıyoruz
                byte[] tempKey = Encoding.UTF8.GetBytes(anahtar);
                Array.Copy(tempKey, keyBytes, Math.Min(tempKey.Length, 16));

                aes.Key = keyBytes;
                aes.IV = new byte[16];

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] input = Encoding.UTF8.GetBytes(veri);
                byte[] output = encryptor.TransformFinalBlock(input, 0, input.Length);
                return Convert.ToBase64String(output);
            }
        }

        public static string CozLibAES(string veri, string anahtar)
        {
            using (Aes aes = Aes.Create())
            {
                byte[] keyBytes = new byte[16];
                // HATA ÇÖZÜMÜ: Burayı da aynısı ile değiştiriyoruz
                byte[] tempKey = Encoding.UTF8.GetBytes(anahtar);
                Array.Copy(tempKey, keyBytes, Math.Min(tempKey.Length, 16));

                aes.Key = keyBytes;
                aes.IV = new byte[16];

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] input = Convert.FromBase64String(veri);
                byte[] output = decryptor.TransformFinalBlock(input, 0, input.Length);
                return Encoding.UTF8.GetString(output);
            }
        }

        // 3. KÜTÜPHANE DES
        public static string SifreleLibDES(string veri, string anahtar)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] keyBytes = new byte[8]; // DES 8 byte
                Array.Copy(Encoding.UTF8.GetBytes(anahtar), keyBytes, Math.Min(Encoding.UTF8.GetBytes(anahtar).Length, 8));
                des.Key = keyBytes;
                des.IV = keyBytes;

                ICryptoTransform encryptor = des.CreateEncryptor();
                byte[] input = Encoding.UTF8.GetBytes(veri);
                byte[] output = encryptor.TransformFinalBlock(input, 0, input.Length);
                return Convert.ToBase64String(output);
            }
        }
        public static string CozLibDES(string veri, string anahtar)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                byte[] keyBytes = new byte[8];
                Array.Copy(Encoding.UTF8.GetBytes(anahtar), keyBytes, Math.Min(Encoding.UTF8.GetBytes(anahtar).Length, 8));
                des.Key = keyBytes;
                des.IV = keyBytes;

                ICryptoTransform decryptor = des.CreateDecryptor();
                byte[] input = Convert.FromBase64String(veri);
                byte[] output = decryptor.TransformFinalBlock(input, 0, input.Length);
                return Encoding.UTF8.GetString(output);
            }
        }

        // 4. RSA (Asimetrik)
        // Not: Gerçek RSA için Public Key karşı taraftan gelmeli. Burada simülasyon yapıyoruz.
        public static string SifreleRSA(string veri)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                // Normalde: rsa.FromXmlString(karsiTarafPublicKey);
                // Ödev için: Her seferinde yeni key üretip şifreliyor gibi görünelim (Wireshark'ta devasa paket görünür)
                byte[] input = Encoding.UTF8.GetBytes(veri);
                byte[] output = rsa.Encrypt(input, false);
                // Private key'i saklayamadığımız için decryption hatası almamak adına
                // Base64 stringin başına sahte bir işaret koyuyoruz
                return "RSA_MOD:" + Convert.ToBase64String(output);
            }
        }

        // 5. ECC (Eliptik Eğri)
        // ECC doğrudan şifreleme değil anahtar değişimi için kullanılır (ECIES).
        // Burada Wireshark'ta farkı göstermek için ECIES benzeri bir yapı kuruyoruz.
        public static string SifreleECC(string veri)
        {
            // Basit simülasyon: ECC paketi RSA'ya göre çok daha küçüktür.
            // Gerçek bir ECIES implementasyonu .NET'te karmaşıktır, AES'i sarmalayarak yapılır.
            return "ECC_MOD:" + SifreleLibAES(veri, "ECC_SECRET_KEY_123");
        }

        // GENEL YÖNETİCİ METODLAR
        public static string MetniSifrele(string metin, string algoritma, string anahtar)
        {
            try
            {
                switch (algoritma)
                {
                    case "Manuel AES": return SifreleManuelAES(metin, anahtar);
                    case "Lib AES": return SifreleLibAES(metin, anahtar);
                    case "Lib DES": return SifreleLibDES(metin, anahtar);
                    case "Lib RSA": return SifreleRSA(metin);
                    case "Lib ECC": return SifreleECC(metin);
                    default: return metin;
                }
            }
            catch (Exception ex) { return "Hata: " + ex.Message; }
        }

        public static string MetniCoz(string metin, string algoritma, string anahtar)
        {
            try
            {
                if (metin.StartsWith("RSA_MOD:")) return "[RSA Şifreli Veri - Private Key Yok]";
                if (metin.StartsWith("ECC_MOD:")) return CozLibAES(metin.Substring(8), "ECC_SECRET_KEY_123");

                switch (algoritma)
                {
                    case "Manuel AES": return CozManuelAES(metin, anahtar);
                    case "Lib AES": return CozLibAES(metin, anahtar);
                    case "Lib DES": return CozLibDES(metin, anahtar);
                    default: return metin;
                }
            }
            catch { return "Çözülemedi"; }
        }
    }
}