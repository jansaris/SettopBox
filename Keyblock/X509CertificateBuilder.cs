using System;
using log4net;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Keyblock
{
    public class X509CertificateBuilder
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(X509CertificateBuilder));

        /// <summary>
        ///     Generates certificate request in PKCS#10 format defined by RFC 2986.
        ///     This will also output the private key at the same time.
        ///     *******************************************
        ///     Notes / Handy references:
        ///     http://www.keylength.com/en/compare/
        ///     http://csrc.nist.gov/publications/nistpubs/800-57/sp800-57_part1_rev3_general.pdf
        /// </summary>
        public static X509CertificateRequest GeneratePkcs10 (X509SubjectAttributes subjectAttributes)
        {
            try
            {
                Logger.Info($"Create X509 Request Certificate with {subjectAttributes.Identifiers.Count} subject parts");
                var rsaKeyPairGenerator = new RsaKeyPairGenerator();
                // Note: the default public exponent in openssl is '65537'
                var genParam = new RsaKeyGenerationParameters(BigInteger.ValueOf(65537), new SecureRandom(), 1024, 5);

                rsaKeyPairGenerator.Init(genParam);

                var pair = rsaKeyPairGenerator.GenerateKeyPair();

                var subject = new X509Name(subjectAttributes.Identifiers, subjectAttributes.Values);

                var pkcs10CertificationRequest = new Pkcs10CertificationRequest(PkcsObjectIdentifiers.Sha1WithRsaEncryption.Id, subject, pair.Public, null, pair.Private);

                var csr = Convert.ToBase64String(pkcs10CertificationRequest.GetEncoded());
                var pkInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private);
                var privateKey = Convert.ToBase64String(pkInfo.GetDerEncoded());
                return new X509CertificateRequest(pkcs10CertificationRequest, csr, privateKey);
            }
            catch (Exception ex)
            {
                // Note: handles errors on the page. Redirect to error page.
                Logger.Error("An error occured during certificate generation", ex);
            }
            return null;
        }
    }
}