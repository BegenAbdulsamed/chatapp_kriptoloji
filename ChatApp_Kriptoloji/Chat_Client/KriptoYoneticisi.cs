using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Client
{
    internal class KriptoYoneticisi
    {
        // Sabit anahtarlar (Normalde kullanıcıdan alınabilir ama basitlik için sabitliyoruz)
        private static string VigenereKey = "ANAHTAR";

        // ANA FONKSİYON: ŞİFRELE
        public static string MetniSifrele(string metin, string algoritma, string anahtar)
        {
            if (string.IsNullOrEmpty(metin)) return "";
            metin = metin.ToUpper();

            try
            {
                switch (algoritma)
                {
                    case "Sezar":
                        // Anahtarı sayıya çevir (Yapamazsa varsayılan 3 olsun)
                        int sezarShift = int.TryParse(anahtar, out int s) ? s : 3;
                        return SezarSifrele(metin, sezarShift);

                    case "Vigenere":
                        return VigenereSifrele(metin, anahtar); // Anahtar string gelir

                    case "Rail Fence":
                        // Anahtarı satır sayısına çevir
                        int railRows = int.TryParse(anahtar, out int r) ? r : 2;
                        return RailFenceSifrele(metin, railRows);

                    case "Playfair":
                        return PlayfairSifrele(metin, anahtar);

                    case "Columnar":
                        return ColumnarSifrele(metin, anahtar); // Örn: "3142"

                    case "Hill":
                        // Hill için anahtarı özel parse etmeliyiz (Örn: "3 3 2 5")
                        return HillSifrele(metin, anahtar);

                    case "Route Cipher":
                        // Route için anahtarı grid boyutu sayabiliriz veya basitçe satır sayısı
                        int routeRows = int.TryParse(anahtar, out int rr) ? rr : 4;
                        return RouteSifrele(metin, routeRows, routeRows); // Kare matris varsaydık

                    // Diğerleri anahtar istemiyor veya sabit (Substitution, Pigpen vb.)
                    case "Substitution": return SubstitutionSifrele(metin);
                    case "Polybius": return PolybiusSifrele(metin);
                    case "Pigpen": return PigpenSifrele(metin);

                    default: return metin;
                }
            }
            catch
            {
                return "HATA: Anahtar Uyumsuz!";
            }
        }

        public static string MetniCoz(string metin, string algoritma, string anahtar)
        {
            if (string.IsNullOrEmpty(metin)) return "";
            metin = metin.ToUpper();

            try
            {
                switch (algoritma)
                {
                    case "Sezar":
                        int sezarShift = int.TryParse(anahtar, out int s) ? s : 3;
                        return SezarCoz(metin, sezarShift);

                    case "Vigenere":
                        return VigenereCoz(metin, anahtar);

                    case "Rail Fence":
                        int railRows = int.TryParse(anahtar, out int r) ? r : 2;
                        return RailFenceCoz(metin, railRows);

                    case "Playfair":
                        return PlayfairCoz(metin, anahtar);

                    case "Columnar":
                        return ColumnarCoz(metin, anahtar);

                    case "Hill":
                        return HillCoz(metin, anahtar);

                    case "Route Cipher":
                        int routeRows = int.TryParse(anahtar, out int rr) ? rr : 4;
                        return RouteCoz(metin, routeRows, routeRows);

                    case "Substitution": return SubstitutionCoz(metin);
                    case "Polybius": return PolybiusCoz(metin);
                    case "Pigpen": return PigpenCoz(metin);

                    default: return metin;
                }
            }
            catch
            {
                return "HATA: Çözülemedi (Anahtar Yanlış Olabilir)";
            }
        }

        #region 1. SEZAR ALGORİTMASI
        private static string SezarSifrele(string text, int shift)
        {
            char[] buffer = text.ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                char letter = buffer[i];
                if (char.IsLetter(letter))
                {
                    char d = char.IsUpper(letter) ? 'A' : 'a';
                    buffer[i] = (char)((((letter + shift) - d) % 26) + d);
                }
            }
            return new string(buffer);
        }

        private static string SezarCoz(string text, int shift)
        {
            // Şifrelemenin tersi: 26 - shift kadar ileri gitmek demektir
            return SezarSifrele(text, 26 - shift);
        }
        #endregion

        #region 2. SUBSTITUTION (BASİT DEĞİŞTİRME - ATBASH GİBİ)
        // A->Z, B->Y mantığıyla ters çevirme
        private static string SubstitutionSifrele(string text)
        {
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (char.IsLetter(chars[i]))
                {
                    if (chars[i] >= 'A' && chars[i] <= 'Z')
                        chars[i] = (char)('Z' - (chars[i] - 'A'));
                }
            }
            return new string(chars);
        }
        private static string SubstitutionCoz(string text) => SubstitutionSifrele(text); // Tersi kendisidir
        #endregion

        #region 3. VIGENERE ALGORİTMASI
        private static string VigenereSifrele(string text, string key)
        {
            StringBuilder sb = new StringBuilder();
            int keyIndex = 0;
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    int shift = key[keyIndex % key.Length] - 'A';
                    char encrypted = (char)((c - 'A' + shift) % 26 + 'A');
                    sb.Append(encrypted);
                    keyIndex++;
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string VigenereCoz(string text, string key)
        {
            StringBuilder sb = new StringBuilder();
            int keyIndex = 0;
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    int shift = key[keyIndex % key.Length] - 'A';
                    // Geriye doğru kaydırma işlemi
                    int decrypted = (c - 'A' - shift);
                    if (decrypted < 0) decrypted += 26;
                    sb.Append((char)(decrypted + 'A'));
                    keyIndex++;
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }
        #endregion

        #region 4. RAIL FENCE (ZİKZAK)
        private static string RailFenceSifrele(string text, int rails)
        {
            // Basitlik adına boşlukları siliyoruz veya olduğu gibi işliyoruz.
            // Burada basit 2 satırlı algoritma
            if (rails != 2) return text; // Şimdilik sadece 2 destekliyoruz
            string tekler = "";
            string ciftler = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i % 2 == 0) ciftler += text[i];
                else tekler += text[i];
            }
            return ciftler + tekler;
        }

        private static string RailFenceCoz(string text, int rails)
        {
            if (rails != 2) return text;
            int len = text.Length;
            int mid = (len + 1) / 2;
            string ciftlerPart = text.Substring(0, mid);
            string teklerPart = text.Substring(mid);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < mid; i++)
            {
                sb.Append(ciftlerPart[i]);
                if (i < teklerPart.Length) sb.Append(teklerPart[i]);
            }
            return sb.ToString();
        }
        #endregion

        #region 5. POLYBIUS (A=11, B=12...)
        // Basit 5x5 Grid (I/J birleşik kabul edilir genelde ama biz basit ASCII map yapalım)
        private static string PolybiusSifrele(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetter(c))
                {
                    int val = c - 'A';
                    // Basit mantık: Satır ve Sütun (5x5)
                    // A(0) -> 0,0 -> 11
                    int row = (val / 5) + 1;
                    int col = (val % 5) + 1;
                    sb.Append($"{row}{col} ");
                }
                else sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static string PolybiusCoz(string text)
        {
            // Şifreli metin: "11 32 45..." formatında gelir
            StringBuilder sb = new StringBuilder();
            string[] parts = text.Split(' ');
            foreach (string p in parts)
            {
                if (p.Length == 2 && char.IsDigit(p[0]) && char.IsDigit(p[1]))
                {
                    int row = int.Parse(p[0].ToString()) - 1;
                    int col = int.Parse(p[1].ToString()) - 1;
                    int val = row * 5 + col;
                    sb.Append((char)('A' + val));
                }
                else sb.Append(p); // Sayı değilse olduğu gibi ekle
            }
            return sb.ToString();
        }
        #region 6. PLAYFAIR ALGORİTMASI
        // Sabit bir anahtar ile 5x5 matris oluşturur. I ve J harflerini bir sayar.
        private static string PlayfairSifrele(string text, string key)
        {
            char[,] matrix = PlayfairMatrisOlustur(key);
            text = PlayfairMetniHazirla(text);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < text.Length; i += 2)
            {
                char c1 = text[i];
                char c2 = text[i + 1];
                PlayfairKonumBul(matrix, c1, out int r1, out int c1pos);
                PlayfairKonumBul(matrix, c2, out int r2, out int c2pos);

                if (r1 == r2) // Aynı satır -> Sağa kaydır
                {
                    sb.Append(matrix[r1, (c1pos + 1) % 5]);
                    sb.Append(matrix[r2, (c2pos + 1) % 5]);
                }
                else if (c1pos == c2pos) // Aynı sütun -> Aşağı kaydır
                {
                    sb.Append(matrix[(r1 + 1) % 5, c1pos]);
                    sb.Append(matrix[(r2 + 1) % 5, c2pos]);
                }
                else // Dikdörtgen -> Köşeleri al
                {
                    sb.Append(matrix[r1, c2pos]);
                    sb.Append(matrix[r2, c1pos]);
                }
            }
            return sb.ToString();
        }

        private static string PlayfairCoz(string text, string key)
        {
            char[,] matrix = PlayfairMatrisOlustur(key);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < text.Length; i += 2)
            {
                char c1 = text[i];
                char c2 = (i + 1 < text.Length) ? text[i + 1] : 'X';
                PlayfairKonumBul(matrix, c1, out int r1, out int c1pos);
                PlayfairKonumBul(matrix, c2, out int r2, out int c2pos);

                if (r1 == r2) // Aynı satır -> Sola kaydır
                {
                    sb.Append(matrix[r1, (c1pos + 4) % 5]); // +4 demek aslında -1 demektir (mod 5)
                    sb.Append(matrix[r2, (c2pos + 4) % 5]);
                }
                else if (c1pos == c2pos) // Aynı sütun -> Yukarı kaydır
                {
                    sb.Append(matrix[(r1 + 4) % 5, c1pos]);
                    sb.Append(matrix[(r2 + 4) % 5, c2pos]);
                }
                else // Dikdörtgen -> Köşeleri al
                {
                    sb.Append(matrix[r1, c2pos]);
                    sb.Append(matrix[r2, c1pos]);
                }
            }
            return sb.ToString();
        }

        // Yardımcı Metodlar
        private static char[,] PlayfairMatrisOlustur(string key)
        {
            char[,] m = new char[5, 5];
            string alphabet = "ABCDEFGHIKLMNOPQRSTUVWXYZ"; // J yok
            string tempKey = key.ToUpper().Replace("J", "I") + alphabet;
            string cleanKey = "";

            // Tekrarları temizle
            foreach (char c in tempKey)
            {
                if (char.IsLetter(c) && !cleanKey.Contains(c.ToString()))
                    cleanKey += c;
            }

            for (int i = 0; i < 25; i++)
                m[i / 5, i % 5] = cleanKey[i];

            return m;
        }

        private static string PlayfairMetniHazirla(string text)
        {
            text = text.ToUpper().Replace("J", "I");
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!char.IsLetter(c)) continue;

                sb.Append(c);
                // Çift harf tekrarı varsa araya X koy (Örn: HEL-LO -> HELXLLO)
                if (i + 1 < text.Length && text[i + 1] == c)
                {
                    sb.Append('X');
                }
            }
            if (sb.Length % 2 != 0) sb.Append('X'); // Uzunluk tekse sonuna X ekle
            return sb.ToString();
        }

        private static void PlayfairKonumBul(char[,] m, char c, out int r, out int col)
        {
            r = 0; col = 0;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    if (m[i, j] == c) { r = i; col = j; return; }
        }
        #endregion

        #region 7. ROUTE CIPHER (SPIRAL ROUTE)
        // Basit Spiral Route (Dıştan İçe) - 4x4 veya dinamik grid
        private static string RouteSifrele(string text, int rows, int cols)
        {
            // Metni temizle ve dolgu yap (Padding)
            text = text.Replace(" ", "").ToUpper();
            while (text.Length < rows * cols) text += "X";
            text = text.Substring(0, rows * cols); // Fazlasını kes

            char[,] grid = new char[rows, cols];
            int index = 0;

            // Izgaraya satır satır yaz
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    grid[r, c] = text[index++];

            // Spiral Olarak Oku (Saat Yönünde Dıştan İçe)
            StringBuilder sb = new StringBuilder();
            int top = 0, bottom = rows - 1, left = 0, right = cols - 1;

            while (top <= bottom && left <= right)
            {
                // Üst satır
                for (int i = left; i <= right; i++) sb.Append(grid[top, i]);
                top++;
                // Sağ sütun
                for (int i = top; i <= bottom; i++) sb.Append(grid[i, right]);
                right--;
                // Alt satır
                if (top <= bottom)
                {
                    for (int i = right; i >= left; i--) sb.Append(grid[bottom, i]);
                    bottom--;
                }
                // Sol sütun
                if (left <= right)
                {
                    for (int i = bottom; i >= top; i--) sb.Append(grid[i, left]);
                    left++;
                }
            }
            return sb.ToString();
        }

        private static string RouteCoz(string text, int rows, int cols)
        {
            // Şifrelerken yaptığımızın tam tersi:
            // Boş grid oluştur, Spiral yolla içine harfleri yerleştir, sonra Satır Satır oku.
            char[,] grid = new char[rows, cols];
            int index = 0;
            int top = 0, bottom = rows - 1, left = 0, right = cols - 1;

            while (top <= bottom && left <= right && index < text.Length)
            {
                for (int i = left; i <= right; i++) grid[top, i] = text[index++];
                top++;
                for (int i = top; i <= bottom; i++) grid[i, right] = text[index++];
                right--;
                if (top <= bottom)
                {
                    for (int i = right; i >= left; i--) grid[bottom, i] = text[index++];
                    bottom--;
                }
                if (left <= right)
                {
                    for (int i = bottom; i >= top; i--) grid[i, left] = text[index++];
                    left++;
                }
            }

            // Satır satır oku
            StringBuilder sb = new StringBuilder();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    sb.Append(grid[r, c]);

            return sb.ToString().TrimEnd('X');
        }
        #endregion

        #region 8. COLUMNAR TRANSPOSITION
        // Anahtar: "3142" gibi sütun okuma sırası
        private static string ColumnarSifrele(string text, string key)
        {
            text = text.Replace(" ", "").ToUpper();
            int colCount = key.Length;
            int rowCount = (int)Math.Ceiling((double)text.Length / colCount);

            // Padding
            while (text.Length < rowCount * colCount) text += "X";

            // Grid oluştur
            char[,] grid = new char[rowCount, colCount];
            int k = 0;
            for (int r = 0; r < rowCount; r++)
                for (int c = 0; c < colCount; c++)
                    grid[r, c] = text[k++];

            // Anahtara göre sütunları oku
            StringBuilder sb = new StringBuilder();
            // Basitlik için key rakamlarını sıralıyoruz: 1, 2, 3, 4...
            // Gerçek "3142" mantığı: Önce 1. sıradaki (Keyde '1' olan) sütunu oku, sonra '2'...

            for (int i = 1; i <= colCount; i++)
            {
                int targetColIndex = key.IndexOf(i.ToString()); // '1' nerede?
                if (targetColIndex == -1) targetColIndex = i - 1; // Hata toleransı

                for (int r = 0; r < rowCount; r++)
                    sb.Append(grid[r, targetColIndex]);
            }
            return sb.ToString();
        }

        private static string ColumnarCoz(string text, string key)
        {
            int colCount = key.Length;
            int rowCount = text.Length / colCount;
            char[,] grid = new char[rowCount, colCount];
            int k = 0;

            // Şifrelerken Sütun Sütun okumuştuk, şimdi Sütun Sütun dolduracağız
            // Ama hangi sütuna? Key sırasına göre.
            for (int i = 1; i <= colCount; i++)
            {
                int targetColIndex = key.IndexOf(i.ToString());
                for (int r = 0; r < rowCount; r++)
                {
                    grid[r, targetColIndex] = text[k++];
                }
            }

            // Satır Satır oku
            StringBuilder sb = new StringBuilder();
            for (int r = 0; r < rowCount; r++)
                for (int c = 0; c < colCount; c++)
                    sb.Append(grid[r, c]);

            return sb.ToString().TrimEnd('X');
        }
        #endregion

        #region 9. PIGPEN (GÖRSEL ŞİFRELEME)
        // Pigpen normalde şekillerdir. Chat ekranında şekil çizmek zor olduğu için
        // bunu simüle eden bir metin haritası kullanacağız.
        private static string PigpenSifrele(string text)
        {
            // A= _| B= |_| C= |_ ... gibi basit bir mapping simülasyonu
            StringBuilder sb = new StringBuilder();
            foreach (char c in text.ToUpper())
            {
                if (c >= 'A' && c <= 'Z')
                {
                    // Basit ASCII sanatıyla temsil
                    // Normalde font gerekir, burada temsili kodluyoruz
                    sb.Append($"[{c}]");
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }
        private static string PigpenCoz(string text)
        {
            // [A] formatını geri A'ya çevir
            return text.Replace("[", "").Replace("]", "");
        }
        #endregion

        #region 10. HILL CIPHER (2x2 MATRİS)
        // Anahtar Matrisi: [3 3]
        //                  [2 5]
        // Determinant = 9, Ters Mod 26 = 3
        private static string HillSifrele(string text, string keyString)
        {
            // Anahtar formatı: "3 3 2 5" (Boşlukla ayrılmış 4 rakam)
            int[,] keyMatrix = ParseHillKey(keyString); // Yardımcı fonk. aşağıda

            text = text.Replace(" ", "").ToUpper();
            if (text.Length % 2 != 0) text += "X";

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < text.Length; i += 2)
            {
                int p1 = text[i] - 'A';
                int p2 = text[i + 1] - 'A';
                int c1 = (keyMatrix[0, 0] * p1 + keyMatrix[0, 1] * p2) % 26;
                int c2 = (keyMatrix[1, 0] * p1 + keyMatrix[1, 1] * p2) % 26;
                sb.Append((char)(c1 + 'A'));
                sb.Append((char)(c2 + 'A'));
            }
            return sb.ToString();
        }

        private static string HillCoz(string text, string keyString)
        {
            // Şifre çözmek için girilen matrisin TERSİNİ (Inverse) bulmamız lazım.
            // Bu ileri matematik gerektirir. Basitlik için kullanıcıdan 
            // "ÇÖZME MATRİSİNİ" girmesini bekleyeceğiz.
            // Yani şifrelerken "3 3 2 5" girdiyse, çözerken onun tersi olan "15 17 20 9" girmeli.
            // (Otomatik hesaplama çok karmaşık modüler aritmetik gerektirir)

            return HillSifrele(text, keyString); // Mantık aynı, sadece matris farklı
        }
        private static int[,] ParseHillKey(string key)
        {
            try
            {
                var parts = key.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                int[,] m = new int[2, 2];
                m[0, 0] = int.Parse(parts[0]); m[0, 1] = int.Parse(parts[1]);
                m[1, 0] = int.Parse(parts[2]); m[1, 1] = int.Parse(parts[3]);
                return m;
            }
            catch
            {
                // Hata olursa varsayılan matris döndür
                return new int[,] { { 3, 3 }, { 2, 5 } };
            }
        }
        #endregion
        #endregion
    }
}
