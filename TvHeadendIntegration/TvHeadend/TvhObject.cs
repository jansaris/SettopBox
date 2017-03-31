using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TvHeadendIntegration.TvHeadend.Web;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TvHeadendIntegration.TvHeadend
{
    public abstract class TvhObject
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TvhObject));

        /*TvHeadend properties*/
        [JsonProperty]
        public string uuid { get; private set; }

        [JsonIgnore]
        public State State { get; set; }

        [JsonIgnore]
        public virtual string CreateUrl { get { return string.Empty; } }
        [JsonIgnore]
        public virtual object CreateData { get { return string.Empty; } }

        public abstract Urls Urls { get; }

        [JsonIgnore] private string _originalJson;

        /*Tvheadend extra properties*/
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        protected TvhObject()
        {
            uuid = Guid.NewGuid().ToString("N");
            _originalJson = string.Empty;
            State = State.New;
        }

        protected static T LoadFromFile<T>(string filename) where T : TvhObject
        {
            try
            {
                var json = File.ReadAllText(filename);
                var tvhFile = JsonConvert.DeserializeObject<T>(json);
                tvhFile._originalJson = json;
                tvhFile.uuid = tvhFile.ExtractId(filename);
                tvhFile.State = State.Loaded;
                return tvhFile;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load {typeof(T).Name} from file {filename}", ex);
                return null;
            }
        }

        protected void SaveToFile(string filename)
        {
            try
            {
                var json = TvhJsonConvert.Serialize(this);
                Logger.Debug($"Generated json: {json} for {filename}");

                if (json == _originalJson)
                {
                    Logger.Debug($"No changes made to object, don't save to file {filename}");
                    return;
                }

                State = File.Exists(filename) ? State.Updated : State.Created;
                if (!PostOnUrl())
                {
                    File.WriteAllText(filename, json);
                    Logger.Debug($"Written json to file {filename} ({State})");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save {GetType().Name} to file {filename}", ex);
            }
        }

        private bool PostOnUrl()
        {
            if (State != State.Created) return false;
            if (string.IsNullOrWhiteSpace(CreateUrl)) return false;
            var comm = new TvhWebCommunication();
            comm.Create(CreateUrl, CreateData);
            Logger.InfoFormat($"Created {GetType().Name} on tvheadend using web. Give it 5 sec to initialize");
            comm.WaitUntilScanCompleted();
            return true;
        }

        protected virtual string ExtractId(string filename)
        {
            return filename.Split(Path.DirectorySeparatorChar).Last();
        }

        protected void RemoveFromFile(string file)
        {
            var fileinfo = new FileInfo(file);
            var folder = fileinfo.Directory;
            if (fileinfo.Exists)
            {
                Logger.Debug($"Remove file {file} for {GetType().Name} {uuid}");
                fileinfo.Delete();
            }
            if (folder != null && folder.Exists && !folder.EnumerateFiles().Any())
            {
                Logger.Debug($"Remove empty folder {folder} for {GetType().Name} {uuid}");
                folder.Delete(true);
            }
            State = State.Removed;
        }
    }
}