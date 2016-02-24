using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace Keyblock
{
    public class X509Certificate2Builder
    {
        public string SubjectName
        { set { _subjectName = value; } }

        public string IssuerName
        { set { _issuerName = value; } }

        public AsymmetricAlgorithm IssuerPrivateKey
        { set { _issuerPrivateKey = value; } }

        public X509Certificate2 Issuer
        {
            set
            {
                _issuer = value;
                _issuerName = value.IssuerName.Name;
                if (value.HasPrivateKey)
                    _issuerPrivateKey = value.PrivateKey;
            }
        }

        public int? KeyStrength
        { set { _keyStrength = value ?? 2048; } }

        public DateTime? NotBefore
        { set { _notBefore = value; } }

        public DateTime? NotAfter
        { set { _notAfter = value; } }

        public bool Intermediate
        { set { _intermediate = value; } }

        private string _subjectName;
        private X509Certificate2 _issuer;
        private string _issuerName;
        private AsymmetricAlgorithm _issuerPrivateKey;
        private int _keyStrength = 2048;
        private DateTime? _notBefore;
        private DateTime? _notAfter;
        private bool _intermediate = true;

        public X509Certificate2 Build()
        {
            // Generating Random Numbers
            var randomGenerator = new CryptoApiRandomGenerator();
            var random = new SecureRandom(randomGenerator);

            // The Certificate Generator
            var certificateGenerator = new X509V3CertificateGenerator();

            // Serial Number
            var serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(long.MaxValue), random);
            certificateGenerator.SetSerialNumber(serialNumber);

            // Signature Algorithm
            certificateGenerator.SetSignatureAlgorithm("SHA256WithRSA");

            // Issuer and Subject Name
            certificateGenerator.SetIssuerDN(new X509Name(_issuerName ?? _subjectName));
            certificateGenerator.SetSubjectDN(new X509Name(_subjectName));

            // Authority Key Identifier
            if (_issuer != null)
            {
                var authorityKeyIdentifier = new AuthorityKeyIdentifierStructure(
                    DotNetUtilities.FromX509Certificate(_issuer));
                certificateGenerator.AddExtension(
                    X509Extensions.AuthorityKeyIdentifier.Id, false, authorityKeyIdentifier);
            }

            // Basic Constraints - certificate is allowed to be used as intermediate.
            certificateGenerator.AddExtension(
                X509Extensions.BasicConstraints.Id, true, new BasicConstraints(_intermediate));

            // Valid For
            certificateGenerator.SetNotBefore(_notBefore ?? DateTime.UtcNow.Date);
            certificateGenerator.SetNotAfter(_notAfter ?? DateTime.UtcNow.Date.AddYears(2));

            // Subject Public Key
            var keyGenerationParameters = new KeyGenerationParameters(random, _keyStrength);
            var keyPairGenerator = new RsaKeyPairGenerator();
            keyPairGenerator.Init(keyGenerationParameters);

            var subjectKeyPair = keyPairGenerator.GenerateKeyPair();
            var issuerKeyPair = _issuerPrivateKey == null
                ? subjectKeyPair
                : DotNetUtilities.GetKeyPair(_issuerPrivateKey);

            certificateGenerator.SetPublicKey(subjectKeyPair.Public);

            // selfsign certificate
            var certificate = certificateGenerator.Generate(issuerKeyPair.Private, random);

            // merge into X509Certificate2
            return new X509Certificate2(certificate.GetEncoded())
            {
                PrivateKey = ConvertToRsaPrivateKey(subjectKeyPair)
            };
        }

        private static AsymmetricAlgorithm ConvertToRsaPrivateKey(AsymmetricCipherKeyPair keyPair)
        {
            var keyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);
            var seq = (Asn1Sequence)Asn1Object.FromByteArray(keyInfo.PrivateKey.GetDerEncoded());
            if (seq.Count != 9)
                throw new PemException("malformed sequence in RSA private key");

            var rsa = new RsaPrivateKeyStructure(seq);
            var rsaparams = new RsaPrivateCrtKeyParameters(
                rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1,
                rsa.Exponent2, rsa.Coefficient);

            return DotNetUtilities.ToRSA(rsaparams);
        }

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
                var rsaKeyPairGenerator = new RsaKeyPairGenerator();

                // Note: the numbers {3, 5, 17, 257 or 65537} as Fermat primes.
                // NIST doesn't allow a public exponent smaller than 65537, since smaller exponents are a problem if they aren't properly padded.
                // Note: the default in openssl is '65537', i.e. 0x10001.
                //var genParam = new RsaKeyGenerationParameters (BigInteger.ValueOf(0x10001), new SecureRandom(), (int)RootLenght.RootLength1024, 128);
                var genParam = new RsaKeyGenerationParameters(BigInteger.ValueOf(65537), new SecureRandom(), 1024, 5);

                rsaKeyPairGenerator.Init(genParam);

                var pair = rsaKeyPairGenerator.GenerateKeyPair();

                var subject = new X509Name(subjectAttributes.Identifiers, subjectAttributes.Values);

                var pkcs10CertificationRequest = new Pkcs10CertificationRequest
                    (PkcsObjectIdentifiers.Sha1WithRsaEncryption.Id, subject, pair.Public, null, pair.Private);

                var valid = pkcs10CertificationRequest.Verify(pair.Public);

                var ver = pkcs10CertificationRequest.GetCertificationRequestInfo().Version;
                var csr = Convert.ToBase64String(pkcs10CertificationRequest.GetEncoded());

                var pkInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private);
                var privateKey = Convert.ToBase64String(pkInfo.GetDerEncoded());
                return new X509CertificateRequest(pkcs10CertificationRequest, csr, privateKey);
            }
            catch (Exception ex)
            {
                // Note: handles errors on the page. Redirect to error page.
                Console.WriteLine($"Error: {ex.Message}");
            }
            return null;
        }

        private enum RootLenght
        {

            RootLength1024 = 1024,

            RootLength2048 = 2048,

            RootLength3072 = 3072,

            RootLength4096 = 4096,

        }
    }
}