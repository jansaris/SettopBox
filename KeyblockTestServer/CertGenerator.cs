using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace KeyblockTestServer
{
    public class CertGenerator
    {
        public byte[] SignWithOpenSsl(string pemcsr)
        {
            var batch = "generate.bat";
            var responseFile = Path.Combine(Program.OpenSslFolder, "response.der");
            if(File.Exists(responseFile)) File.Delete(responseFile);
            File.WriteAllText(Path.Combine(Program.OpenSslFolder, "Certificate.csr"), pemcsr);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = Program.OpenSslFolder,
                    FileName = Path.Combine(Program.OpenSslFolder, batch)
                }
            };
            process.Start();
            process.WaitForExit();

            while (!File.Exists(responseFile))
            {
                Task.Delay(50).Wait();
            }
            return File.ReadAllBytes(responseFile);
        }
    }
}