using System.IO;
using Org.BouncyCastle.Pkcs;

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
            var str = new StringWriter();
            var pwriter = new Org.BouncyCastle.OpenSsl.PemWriter(str);
            pwriter.WriteObject(_pkcs10CertificationRequest);
            return str.ToString();
        }

        public override string ToString()
        {
            return CsrPem();
        }
    }
}