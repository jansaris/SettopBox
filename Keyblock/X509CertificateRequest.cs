using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using log4net;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Keyblock
{
    public class X509CertificateRequest
    {
        readonly ILog _logger;
        readonly string _keyFile;
        readonly Settings _settings;

        Pkcs10CertificationRequest _pkcs10CertificationRequest;
        public AsymmetricCipherKeyPair KeyPair { get; private set; }

        public X509CertificateRequest(ILog logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
            _keyFile = Path.Combine(settings.DataFolder, "privatekey.pem");
        }

        X509Name LoadSubject()
        {
            var attributes = new List<KeyValuePair<DerObjectIdentifier, object>>();
            Add(attributes, X509Name.C, _settings.Country);
            Add(attributes, X509Name.ST, _settings.Province);
            Add(attributes, X509Name.L, _settings.City);
            Add(attributes, X509Name.O, _settings.Company);
            Add(attributes, X509Name.OU, _settings.Organization);
            Add(attributes, X509Name.CN, _settings.Common);
            Add(attributes, X509Name.EmailAddress, _settings.Email);
            Add(attributes, PkcsObjectIdentifiers.Pkcs9AtChallengePassword, _settings.ChallengePassword);
            var identifiers = attributes.Select(a => a.Key).ToList();
            var values = attributes.Select(a => a.Value).ToList();
            return new X509Name(identifiers, values);
        }

        static void Add(ICollection<KeyValuePair<DerObjectIdentifier, object>> list, DerObjectIdentifier identifier, string value)
        {
            list.Add(new KeyValuePair<DerObjectIdentifier, object>(identifier, value));
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
                if (LoadKeyPairFromDisk())
                {
                    _logger.Info("Loaded keypair from disk");
                }
                else
                {
                    var rsaKeyPairGenerator = new RsaKeyPairGenerator();
                    // Note: the default public exponent in openssl is '65537'
                    var genParam = new RsaKeyGenerationParameters(BigInteger.ValueOf(65537), new SecureRandom(), 1024, 5);

                    rsaKeyPairGenerator.Init(genParam);
                    KeyPair = rsaKeyPairGenerator.GenerateKeyPair();
                    SaveKeyPairToDisk();
                }

                var subject = LoadSubject();
                _pkcs10CertificationRequest = new Pkcs10CertificationRequest(PkcsObjectIdentifiers.Sha1WithRsaEncryption.Id, subject, KeyPair.Public, null, KeyPair.Private);
            }
            catch (Exception ex)
            {
                _logger.Error("An error occured during Request Certificate generation", ex);
                throw new KeyblockException("An error occured during Request Certificate generation", ex);
            }
        }

        public override string ToString()
        {
            if(_pkcs10CertificationRequest == null) Generate();
            var str = new StringWriter();
            var pwriter = new PemWriter(str);
            pwriter.WriteObject(_pkcs10CertificationRequest);
            return str.ToString();
        }
        private bool LoadKeyPairFromDisk()
        {
            var file = new FileInfo(_keyFile);
            if (!file.Exists)
            {
                _logger.Warn($"No Keypair available on disk at '{file.FullName}'");
                return false;
            }
            try
            {
                using (var stream = file.OpenText())
                    KeyPair = (AsymmetricCipherKeyPair)new PemReader(stream).ReadObject();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to load keypair from disk", ex);
                return false;
            }
        }

        private void SaveKeyPairToDisk()
        {
            try
            {
                using (var stream = File.CreateText(_keyFile))
                {
                    var writer = new PemWriter(stream);
                    writer.WriteObject(KeyPair.Private);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to save the keypair to disk", ex);
            }
        }

        public void CleanUp()
        {
            try
            {
                var file = new FileInfo(_keyFile);
                if (file.Exists)
                {
                    _logger.Warn($"Clean up file '{file.FullName}'");
                    file.Delete();
                }
                else
                {
                    _logger.Warn($"Can't clean up file '{file.FullName}' because it doesn't exists");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn("Failed to delete the keypair from disk", ex);
            }
        }
    }
}