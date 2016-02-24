using System;
using System.IO;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Utilities.IO.Pem;
using PemWriter = Org.BouncyCastle.Utilities.IO.Pem.PemWriter;

namespace Keyblock
{
    public class X509CertificateRequest
    {
        private readonly Pkcs10CertificationRequest _pkcs10CertificationRequest;
        public string Csr { get; private set; }
        public string PrivateKey { get; private set; }

        public X509CertificateRequest(Pkcs10CertificationRequest pkcs10CertificationRequest, string csr, string privateKey)
        {
            _pkcs10CertificationRequest = pkcs10CertificationRequest;
            Csr = csr;
            PrivateKey = privateKey;
        }

        public string CsrPem()
        {
            var pemObject = new PemObject("CERTIFICATE REQUEST", _pkcs10CertificationRequest.GetEncoded());
            var str = new StringWriter();
            var pwriter = new Org.BouncyCastle.OpenSsl.PemWriter(str);
            pwriter.WriteObject(_pkcs10CertificationRequest);
            //var pemWriter = new PemWriter(str);
            //pemWriter.WriteObject(pemObject);
            return str.ToString();
        }

        public override string ToString()
        {
            return $"-----BEGIN CERTIFICATE REQUEST-----{Csr}-----END CERTIFICATE REQUEST-----";
        }
    }
}