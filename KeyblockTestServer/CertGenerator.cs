using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using log4net;

namespace KeyblockTestServer
{
    public class CertGenerator
    {
        private readonly ILog _logger = LogManager.GetLogger(typeof(CertGenerator));

        public byte[] SignWithOpenSsl(string pemcsr)
        {
            try
            {
                var batch = "generate.bat";
                var responseFile = Path.Combine(Program.OpenSslFolder, "response.der");
                if (File.Exists(responseFile)) File.Delete(responseFile);
                File.WriteAllText(Path.Combine(Program.OpenSslFolder, "Certificate.csr"), pemcsr);
                var executable = Path.Combine(Program.OpenSslFolder, batch);
                _logger.Info($"Start generate certificate using: {executable} with working directory {Program.OpenSslFolder}");
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        WorkingDirectory = Program.OpenSslFolder,
                        FileName = executable
                    }
                };
                process.Start();
                process.WaitForExit();

                for (var i = 0; i < 10; i++)
                {
                    if (File.Exists(responseFile)) break;
                    Task.Delay(50).Wait();
                }

                return File.ReadAllBytes(responseFile);
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to generate a certificate: {ex.Message}");
                _logger.Error(ex.StackTrace);
                throw;
            }
        }
    }
}