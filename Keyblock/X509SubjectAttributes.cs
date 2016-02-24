using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;

namespace Keyblock
{
    public class X509SubjectAttributes
    {
        public List<DerObjectIdentifier> Identifiers => _attributes.Select(a => a.Key).ToList();
        public List<object> Values => _attributes.Select(a => a.Value).ToList();

        readonly List<KeyValuePair<DerObjectIdentifier, object>> _attributes = new List<KeyValuePair<DerObjectIdentifier, object>>();

        private void Add(DerObjectIdentifier identifier, string value)
        {
            _attributes.Add(new KeyValuePair<DerObjectIdentifier, object>(identifier, value));
        }

        public void ChallangePassword(string value)
        {
            Add(Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Pkcs9AtChallengePassword,value);
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
    }
}