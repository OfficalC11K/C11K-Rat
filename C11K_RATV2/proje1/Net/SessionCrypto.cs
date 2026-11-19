// Dosya: Net/SessionCrypto.cs
//using App.Service.Net;
using System;
using System.Security.Cryptography;
using System.Text;

namespace App.Service
{
    public sealed class SessionCrypto
    {
        readonly object _lock = new object();
        readonly Aes _aes;
        readonly HMACSHA256 _hmac;

        public SessionCrypto(byte[] k)
        {
            var enc = new byte[32];
            var mac = new byte[32];
            Buffer.BlockCopy(k, 0, enc, 0, 32);
            Buffer.BlockCopy(k, 32, mac, 0, 32);

            _aes = Aes.Create();
            _aes.Key = enc;
            _aes.Mode = CipherMode.CBC;
            _aes.Padding = PaddingMode.PKCS7;
            _hmac = new HMACSHA256(mac);
        }

        public byte[] Encrypt(string msg)
        {
            lock (_lock)
            {
                var iv = new byte[16];
                Protocol.Fill(iv);
                _aes.IV = iv;

                byte[] plain = Encoding.UTF8.GetBytes(msg);
                byte[] cipher;
                using (var e = _aes.CreateEncryptor())
                    cipher = e.TransformFinalBlock(plain, 0, plain.Length);

                var body = new byte[16 + cipher.Length + 32];
                Buffer.BlockCopy(iv, 0, body, 0, 16);
                Buffer.BlockCopy(cipher, 0, body, 16, cipher.Length);
                byte[] mac = _hmac.ComputeHash(body, 0, 16 + cipher.Length);
                Buffer.BlockCopy(mac, 0, body, 16 + cipher.Length, 32);
                return body;
            }
        }

        public string Decrypt(byte[] body)
        {
            if (body == null || body.Length <= 48) return null;
            int clen = body.Length - 48;

            lock (_lock)
            {
                byte[] mac = _hmac.ComputeHash(body, 0, 16 + clen);
                int diff = 0;
                for (int i = 0; i < 32; i++) diff |= mac[i] ^ body[16 + clen + i];
                if (diff != 0) return null;

                var iv = new byte[16];
                Buffer.BlockCopy(body, 0, iv, 0, 16);
                _aes.IV = iv;
                using (var d = _aes.CreateDecryptor())
                    return Encoding.UTF8.GetString(d.TransformFinalBlock(body, 16, clen));
            }
        }
    }
}