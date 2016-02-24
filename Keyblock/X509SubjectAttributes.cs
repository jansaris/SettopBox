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

        public void Add(DerObjectIdentifier identifier, string value)
        {
            //if (identifier == X509Name.EmailAddress)
            //{
            //    var str = new DerIA5String(value);
            //    _attributes.Add(new KeyValuePair<DerObjectIdentifier, object>(identifier, str));
            //}
            //else
            _attributes.Add(new KeyValuePair<DerObjectIdentifier, object>(identifier, value));
        }

        public void ChallangePassword(string value)
        {
            Add(Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.Pkcs9AtChallengePassword,value);
        }
    }
}