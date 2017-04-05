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
    }
}