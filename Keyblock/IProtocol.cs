namespace Keyblock
{
    public interface IProtocol
    {
        string GetCertificate(X509CertificateRequest certificateRequest);
        string GetSessionKey();
        byte[] SaveEncryptedPassword(string timestamp, string ski, string password, byte[] sessionKey);
        byte[] GetEncryptedPassword(string timestamp, string ski, byte[] sessionKey);
        byte[] LoadKeyBlock(string timestamp, string ski, string hash, byte[] sessionKey);
        byte[] GetVksConnectionInfo(string timestamp, string ski, byte[] sessionKey);
    }
}