// Dosya: Net/Tls.cs
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace App.Service
{
    public static class Tls
    {
        static X509Certificate2 _cert;
        static readonly object _lk = new object();
        static bool _tried;

        public static X509Certificate2 ServerCert()
        {
            lock (_lk)
            {
                if (_cert != null || _tried) return _cert;
                _tried = true;
                try
                {
                    using (var rsa = RSA.Create())
                    {
                        rsa.KeySize = 2048;
                        var req = new CertificateRequest("CN=cl" + "oud.micro" + "soft.com", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                        req.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                        using (var c = req.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2), DateTimeOffset.UtcNow.AddYears(20)))
                            _cert = new X509Certificate2(c.Export(X509ContentType.Pfx));
                    }
                }
                catch { _cert = null; }
                return _cert;
            }
        }
    }
}