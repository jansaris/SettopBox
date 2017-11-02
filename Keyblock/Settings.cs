using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using SharedComponents.Models;
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
        public bool GenerateMachineId { get; private set; }
        public string ClientId { get; private set; }
        public bool GenerateClientId { get; private set; }

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
        public string KeyblockChannelsToMonitor { get; set; }
        public bool KeepBlockIfChannelsAreOutdated { get; set; }
        public bool AutoCleanUp { get; set; }

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

        public IDictionary<string, int> GetChannelsToMonitor()
        {
            var retValue = new Dictionary<string, int>();
            if (string.IsNullOrWhiteSpace(KeyblockChannelsToMonitor)) return retValue;
            try
            {
                retValue = KeyblockChannelsToMonitor
                    .Split(';') //Split on ; --> ned1:601;ned2:602
                    .Select(s => s.Split(':')) //Split on : --> ned1:601
                    .ToDictionary(key => key[0], value => int.Parse(value[1])); //And convert to dictionary
            }
            catch
            {
                Logger.Error($"Failed to parse KeyblockChannelsToMonitor: {KeyblockChannelsToMonitor}");
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
            if (string.IsNullOrWhiteSpace(ClientId)) GenerateNewClientId();
            if (string.IsNullOrWhiteSpace(MachineId)) GenerateNewMachineId();
        }

        public void GenerateNewMachineId()
        {
            if (!GenerateMachineId)
            {
                Logger.Debug("Generation of Machine ID disabled in settings");
                return;
            }

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

        public void GenerateNewClientId()
        {
            if (!GenerateClientId)
            {
                Logger.Debug("Generation of Client ID disabled in settings");
                return;
            }

            Logger.Debug($"Generate new ClientId");
            var text = "";
            var possible = "abcd0123456789";
            var random = new Random();
            for (var i = 0; i < 56; i++)
                text += possible[random.Next(0, possible.Length)];

            ClientId = text;
            Logger.Debug($"Your ClientID is: {ClientId}");
            Save();
        }

        public void UpdateEmail(string newMailAdress)
        {
            Email = newMailAdress;
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

        internal void ToggleChannel(KeyblockChannelUpdate update)
        {
            if (update == null) return;
            Logger.Debug("Update channel list to monitor");
            var list = GetChannelsToMonitor();
            if (update.Enabled)
            {
                if (list.ContainsKey(update.Id))
                {
                    Logger.Info($"Updated key {update.NewKey} for {update.Id}");
                    list[update.Id] = update.NewKey;
                }
                else
                {
                    Logger.Info($"Add key {update.NewKey} for {update.Id}");
                    list.Add(update.Id, update.NewKey);
                }
            }
            else if(list.ContainsKey(update.Id))
            {
                Logger.Info($"Removed key for {update.Id}");
                list.Remove(update.Id);
            }
            KeyblockChannelsToMonitor = string.Join(";", list.Select(kv => $"{kv.Key}:{kv.Value}"));
            Save();
        }

        internal IList<int> GetChannelNumbersToMonitor()
        {
            return GetChannelsToMonitor().Select(kv => kv.Value).ToList();
        }

        internal IProtocol GetProtocol(ILog logger)
        {
            return ProtocolFactory.CreateProtocol(this, logger);
        }
    }
}
