using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using SharedComponents.Settings;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Keyblock
{
    public class Settings : IniSettings
    {
        readonly Random _random;
        protected override string Name => "Keyblock";

        public string MachineId { get; private set; }
        public string ClientId { get; private set; }

        // Cert data
        public string Address { get; private set; }
        public string ZipCode { get; private set; }
        public string Country { get; private set; }
        public string Province { get; private set; }
        public string City { get; private set; }
        public string Organization { get; private set; }
        public string Common { get; private set; }
        public string Telephone { get; private set; }
        public string Email { get; private set; }
        public string EmailHost { get; private set; }
        public string ChallengePassword { get; private set; }

        // Connection data
        public string VcasServer { get; private set; }
        public string VksServer { get; private set; }
        public int VcasPort { get; private set; }
        public int VksPort { get; private set; }

        // API data
        public string Company { get; private set; }
        public string MessageFormat { get; private set; }

        // Communication data
        public string DataFolder { get; private set; } = "Data";
        public string KeyblockFile { get; private set; } = "Keyblock.dat";
        public bool WriteAllCommunicationToDisk { get; private set; } = false;
        public string CommunicationFolder { get; private set; } = "Communication";
        public bool DontUseRealServerButMessagesFromDisk { get; private set; } = false;
        public int MaxRetries { get; set; } = 3;
        public int WaitOnFailingBlockRetrievalInMilliseconds { get; private set; } = 1000;
        public double KeyblockValidationInHours { get; set; } = 1;
        public bool ForceInitialKeyblockDownload { get; set; } = false;
        public bool InitialLoadKeyblock { get; set; } = true;
        public string KeyblockChannelsToIgnore { get; set; }

        public IList<int> GetChannelsToIgnore()
        {
            var retValue = new List<int>();
            if (string.IsNullOrWhiteSpace(KeyblockChannelsToIgnore)) return retValue;
            try
            {
                return KeyblockChannelsToIgnore
                    .Split(';')
                    .Select(v => Convert.ToInt32(v))
                    .ToList();
            }
            catch
            {
                Logger.Error($"Failed to parse KeyblockChannelsToIgnore: {KeyblockChannelsToIgnore}");
            }
            return retValue;
        }

        public Settings(ILog logger) : base(logger)
        {
            _random = new Random();
        }

        public override void Load()
        {
            base.Load();
            if (string.IsNullOrWhiteSpace(ClientId)) GenerateClientId();
            if (string.IsNullOrWhiteSpace(MachineId)) GenerateMachineId();
        }

        public void GenerateMachineId()
        {
            Logger.Debug("Generate new MachineID");
            var buf = new byte[10];
            _random.NextBytes(buf);
            MachineId = string.Empty;
            foreach (var b in buf)
            {
                MachineId += (b & 0xFF).ToString("x2");
            }
            MachineId = Convert.ToBase64String(Encoding.ASCII.GetBytes(MachineId));
            Logger.Debug($"Your MachineID is: {MachineId}");
            Save();
        }

        public void UpdateEmail(string newMailAdress)
        {
            Email = newMailAdress;
            Save();
        }

        public void GenerateClientId()
        {
            Logger.Debug("Generate new ClientId");
            var buf = new byte[28];
            _random.NextBytes(buf);
            ClientId = string.Empty;
            foreach (var b in buf)
            {
                ClientId += (b & 0xFF).ToString("x2");
            }
            Logger.Debug($"Your ClientID is: {ClientId}");
            Save();
        }

        public void EnsureDataFolderExists(string folder)
        {
            var directory = new DirectoryInfo(folder);
            if (directory.Exists) Logger.Debug($"Data folder '{directory.FullName}' exists");
            else
            {
                Logger.Debug($"Data folder '{directory.FullName}' doesn't exist, create it");
                directory.Create();
                Logger.Info($"Created data folder '{directory.FullName}'");
            }
        }
    }
}
