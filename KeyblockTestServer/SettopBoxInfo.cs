using System;
using System.IO;
using Org.BouncyCastle.Asn1.X509;

namespace KeyblockTestServer
{
    public class SettopBoxInfo
    {
        /*1155~01025~1234567e62626241436b6b52727471744a724536546e6c513456789d~getCertificate~KPN~NA~NA~-----BEGIN CERTIFICATE REQUEST-----
MIICGjCCAYMCADCB3DELMAkGA1UEBhMCVVMxCzAJBgNVBAgTAkNBMRIwEAYDVQQH
EwlTYW4gRGllZ28xDDAKBgNVBAoTA0tQTjE9MDsGA1UECxM0dnIyLjMuMTMtYXJj
YWR5YW4taG1iMjI2MGMuMi1leHRyb290Y2VydC1jYW5kaWRhdGUuMTEMMAoGA1UE
AxMDU1RCMTUwMwYJKoZIhvcNAQkBFiYwMDAyOUJFMDg4MEQuMTUwODAwNzI3NEBW
ZXJpbWF0cml4LmNvbTEaMBgGCSqGSIb3DQEJBxMLVk9EUGFzc3dvcmQwgZ0wDQYJ
KoZIhvcNAQEBBQADgYsAMIGHAoGBAKqPeNVJNc7FvVzXiQQcThqokSgu5dfdl8iN
75alnL5/e8bABZxiZjOZBVyPmdevXjKS2x+EMsC0s/M8tVFrbkMnk1VnLcrzTTR1
5LUGyWVKoWs/fTsP23kg6QCX69Y3rw0IKMFIwIZTNEf4L/Y0xjPgEw28W/j6Skr4
wOzj6+/XAgEDoAAwDQYJKoZIhvcNAQEFBQADgYEAQbJ8Q6VROcjEM34BBwHTXZGA
IrbFetAQ8/NNxt2Ov11BHDAxBhgnc/riB+oybJS9vHKpLYqOOii8hUHzZoIEYoCG
A5x9zuX2tbuSzsrUH7gd3W3/0Tu/7wOmui2pkSJDOyCVn19Kp+7F3SXfWPM0kIa6
IKaId93FQLfvUzNv0xw=
-----END CERTIFICATE REQUEST-----                            
~STB~6825 Flanders Drive~ ~San Diego~CA~92121~US~858-677-7800~001234512345.1508007274@Verimatrix.com~001234512345~VODPassword~*/
        public static SettopBoxInfo Create(string getCertificateMessage)
        {
            var parts = getCertificateMessage.Split('~');
            if (parts.Length != 20)
            {
                throw new InvalidDataException();
            }

            var reader = new Org.BouncyCastle.OpenSsl.PemReader(new StringReader(parts[7]));
            var req = (Org.BouncyCastle.Pkcs.Pkcs10CertificationRequest)reader.ReadObject();
            var info = req.GetCertificationRequestInfo();
            var organization = info.Subject.GetValueList(X509Name.OU)[0].ToString();

            return new SettopBoxInfo
            {
                Protocol = parts[0],
                //Message length = parts[1]
                ClientId = parts[2],
                //Message type = parts[3],
                Company = parts[4],
                //NA = parts[5],
                //NA = parts[6],
                //CertificateRequest = parts[7],
                Organization = organization,
                Common = parts[8],
                Address = parts[9],
                //space = parts[10],
                City = parts[11],
                Province = parts[12],
                ZipCode = parts[13],
                Country = parts[14],
                Telephone = parts[15],
                Email = parts[16],
                MachineId = parts[17],
                ChallengePassword = parts[18]
            };
        }

        public string Protocol { get; set; }

        public string ClientId { get; set; }

        public string MachineId { get; set; }

        public string Company { get; set; }

        public string Common { get; set; }

        public string ChallengePassword { get; set; }

        public string Email { get; set; }

        public string Telephone { get; set; }

        public string Country { get; set; }

        public string ZipCode { get; set; }

        public string Province { get; set; }

        public string City { get; set; }

        public string Address { get; set; }
        public string EmailHost => Email.Substring(Email.IndexOf("@", StringComparison.Ordinal) + 1);
        public string Organization { get; set; }

        public string Serialize()
        {
            return $"{nameof(Protocol)}|{Protocol}" + Environment.NewLine +
                   $"{nameof(ClientId)}|{ClientId}" + Environment.NewLine +
                   $"{nameof(MachineId)}|{MachineId}" + Environment.NewLine +
                   $"{nameof(Company)}|{Company}" + Environment.NewLine +
                   $"{nameof(Organization)}|{Organization}" + Environment.NewLine +
                   $"{nameof(Common)}|{Common}" + Environment.NewLine +
                   $"{nameof(ChallengePassword)}|{ChallengePassword}" + Environment.NewLine +
                   $"{nameof(Email)}|{Email}" + Environment.NewLine +
                   $"{nameof(EmailHost)}|{EmailHost}" + Environment.NewLine +
                   $"{nameof(Telephone)}|{Telephone}" + Environment.NewLine +
                   $"{nameof(Country)}|{Country}" + Environment.NewLine +
                   $"{nameof(ZipCode)}|{ZipCode}" + Environment.NewLine +
                   $"{nameof(Province)}|{Province}" + Environment.NewLine +
                   $"{nameof(City)}|{City}" + Environment.NewLine +
                   $"{nameof(Address)}|{Address}" + Environment.NewLine;

        }
    }
}
