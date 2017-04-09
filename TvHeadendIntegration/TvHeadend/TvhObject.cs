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
        [JsonIgnore]
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TvhObject));

        /*TvHeadend properties*/
        [JsonProperty]
        public string uuid { get; private set; }

        [JsonIgnore]
        public State State { get; set; }

        [JsonIgnore]
        public virtual string CreateUrl { get { return string.Empty; } }
        [JsonIgnore]
        public virtual string UpdateUrl { get { return "/api/idnode/save"; } }
        [JsonIgnore]
        public virtual object CreateData { get { return string.Empty; } }
        [JsonIgnore]
        public virtual object UpdateData { get { return string.Empty; } }

        [JsonIgnore]
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

        public bool CreateOnTvh(TvhCommunication comm)
        {
            if (State != State.Created) return false;
            if (string.IsNullOrWhiteSpace(CreateUrl)) return false;
            comm.Post(CreateUrl, CreateData, null);
            Logger.InfoFormat($"Created {GetType().Name} on tvheadend. Give it 5 sec to initialize");
            comm.WaitUntilScanCompleted();
            return true;
        }

        public bool UpdateOnTvh(TvhCommunication comm)
        {
            if (State != State.Loaded) return false;
            if (string.IsNullOrWhiteSpace(UpdateUrl)) return false;
            Func<string, string> extendUploadData = (data) => string.Concat("node=", data);
            var response = comm.Post(UpdateUrl, UpdateData, extendUploadData);
            if (response == "{}") response = "OK";
            Logger.InfoFormat($"Updated {GetType().Name} on tvheadend with response: {response}");
            return !string.IsNullOrWhiteSpace(response);
        }

        protected virtual string ExtractId(string filename)
        {
            return filename.Split(Path.DirectorySeparatorChar).Last();
        }
    }
}