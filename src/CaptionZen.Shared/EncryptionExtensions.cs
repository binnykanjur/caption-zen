using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace CaptionZen.Shared;

public static class EncryptionExtensions {

    public static string Encrypt(this string plainText, byte[] key, byte[] iv) {
        using (Aes aesAlg = Aes.Create()) {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            using (MemoryStream msEncrypt = new MemoryStream()) {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
                        swEncrypt.Write(plainText);
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
    }


    public static string Decrypt(this string cipherText, byte[] key, byte[] iv) {
        using (Aes aesAlg = Aes.Create()) {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText))) {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt)) {
                        return srDecrypt.ReadToEnd();
                    }
                }
            }
        }
    }

}

public class EncryptionSettings {
    [Required]
    public string? Key { get; set; }
    [Required]
    public string? IV { get; set; }
}