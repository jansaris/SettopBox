using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Keyblock
{
    public class X509CertificateRequest
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(X509CertificateRequest));

        Pkcs10CertificationRequest _pkcs10CertificationRequest;
        List<DerObjectIdentifier> Identifiers => _attributes.Select(a => a.Key).ToList();
        List<object> Values => _attributes.Select(a => a.Value).ToList();

        readonly List<KeyValuePair<DerObjectIdentifier, object>> _attributes = new List<KeyValuePair<DerObjectIdentifier, object>>();

        void Add(DerObjectIdentifier identifier, string value)
        {
            _attributes.Add(new KeyValuePair<DerObjectIdentifier, object>(identifier, value));
        }

        public string Csr { get; private set; }
        public string PrivateKey { get; private set; }

        public void ChallangePassword(string value)
        {
            Add(PkcsObjectIdentifiers.Pkcs9AtChallengePassword, value);
        }

        public void AddCountry(string country)
        {
            Add(X509Name.C, country);
        }

        public void AddProvice(string province)
        {
            Add(X509Name.ST, province);
        }

        public void AddCity(string city)
        {
            Add(X509Name.L, city);
        }

        public void AddCompany(string company)
        {
            Add(X509Name.O, company);
        }

        public void AddOrganization(string organization)
        {
            Add(X509Name.OU, organization);
        }

        public void AddCommon(string common)
        {
            Add(X509Name.CN, common);
        }

        public void AddEmail(string email)
        {
            Add(X509Name.EmailAddress, email);
        }

        /// <summary>
        ///     Generates certificate request in PKCS#10 format defined by RFC 2986.
        ///     This will also output the private key at the same time.
        ///     *******************************************
        ///     Notes / Handy references:
        ///     http://www.keylength.com/en/compare/
        ///     http://csrc.nist.gov/publications/nistpubs/800-57/sp800-57_part1_rev3_general.pdf
        /// </summary>
        public void Generate()
        {
            try
            {
                Logger.Info($"Create X509 Request Certificate with {Identifiers.Count} subject parts");
                var rsaKeyPairGenerator = new RsaKeyPairGenerator();
                // Note: the default public exponent in openssl is '65537'
                var genParam = new RsaKeyGenerationParameters(BigInteger.ValueOf(65537), new SecureRandom(), 1024, 5);

                rsaKeyPairGenerator.Init(genParam);

                var pair = rsaKeyPairGenerator.GenerateKeyPair();

                var subject = new X509Name(Identifiers, Values);

                _pkcs10CertificationRequest = new Pkcs10CertificationRequest(PkcsObjectIdentifiers.Sha1WithRsaEncryption.Id, subject, pair.Public, null, pair.Private);

                Csr = Convert.ToBase64String(_pkcs10CertificationRequest.GetEncoded());
                var pkInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private);
                PrivateKey = Convert.ToBase64String(pkInfo.GetDerEncoded());
            }
            catch (Exception ex)
            {
                Logger.Error("An error occured during Request Certificate generation", ex);
                throw new KeyblockException("An error occured during Request Certificate generation", ex);
            }
        }

        public override string ToString()
        {
            if(_pkcs10CertificationRequest == null) Generate();
            var str = new StringWriter();
            var pwriter = new Org.BouncyCastle.OpenSsl.PemWriter(str);
            pwriter.WriteObject(_pkcs10CertificationRequest);
            return str.ToString();
        }
    }
}