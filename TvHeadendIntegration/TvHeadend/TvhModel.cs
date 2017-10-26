using System;
using System.Collections.Generic;
using System.Linq;
using TvHeadendIntegration.TvHeadend.Web;
using log4net;
using SharedComponents.Module;

namespace TvHeadendIntegration.TvHeadend
{
    public enum State { New, Loaded, Created, Updated, Removed }

    public class TvhModel
    {
        private readonly ILog _logger;
        private readonly Settings _settings;
        private readonly Func<TvhCommunication> _communicationFactory;
        public bool AuthenticationSuccessfull { get; private set; }

        private List<Network> _networks = new List<Network>();
        private List<Channel> _channels = new List<Channel>();
        private List<Tag> _tags = new List<Tag>();
        private List<Epg> _epgs = new List<Epg>(); 
        private string _tvhFolder = string.Empty;
        private string _defaultNetworkName = string.Empty;
        
        public TvhModel(ILog logger, Settings settings, Func<TvhCommunication> communicationFactory)
        {
            _logger = logger;
            _settings = settings;
            _communicationFactory = communicationFactory;
        }

        private Network DefaultNetwork
        {
            get
            {
                var network = _networks.FirstOrDefault(n => n.networkname.Equals(_defaultNetworkName, StringComparison.OrdinalIgnoreCase));
                return network ?? CreateNetwork(_defaultNetworkName);
            }
        }

        public void UpdateOnTvh(TvhObject obj)
        {
            obj.UpdateOnTvh(_communicationFactory());
        }

        public void ReadFromWeb()
        {
            AuthenticationSuccessfull = _communicationFactory().TestAuthentication();
            if (!AuthenticationSuccessfull) return;
            
            _defaultNetworkName = _settings.TvhNetworkName;
            _networks = ReadFromWeb<Network>();
            _channels = ReadFromWeb<Channel>();
            _tags = ReadFromWeb<Tag>();
            _epgs = ReadFromWeb<Epg>();
                //TODO: Ik probeer de EPG tabel op te halen via het web

            var muxes = ReadFromWeb<Mux>();
            var services = ReadFromWeb<Service>();
            muxes.ForEach(mux => mux.Services.AddRange(services.Where(service => service.multiplex_uuid == mux.uuid)));
            _networks.ForEach(network => network.Muxes.AddRange(muxes.Where(mux => mux.network_uuid == network.uuid)));
        }

        internal List<TvHeadendChannelInfo> GetChannelInfo()
        {
            return _networks.SelectMany(n => n.Muxes.Select(GetNameAndUrl)).ToList();
        }

        private TvHeadendChannelInfo GetNameAndUrl(Mux mux)
        {
            return new TvHeadendChannelInfo
            {
                Name = mux.Services.FirstOrDefault()?.svcname ?? mux.iptv_muxname,
                Url = mux.iptv_url,
                UUID = mux.uuid
            };
        }

        public void AddChannel(string id, string newUrl, bool epg)
        {
            _logger.Info($"Add channel '{id}' with url '{newUrl}' to TvHeadend");
        }

        public void RemoveChannel(string tvhId, string id)
        {
            _logger.Info($"Remove channel '{id}' with UUID '{tvhId}' from TvHeadend");
        }

        public void UpdateChannel(string tvhId, string id, string newUrl, bool epg)
        {
            _logger.Info($"Update channel '{id}' with UUID '{tvhId}' in TvHeadend");
            var mux = ResolveMux(tvhId);
            if(mux.iptv_url != newUrl)
            {
                _logger.Info($"Update mux ({id}) url from '{mux.iptv_url}' to '{newUrl}'");
                mux.iptv_url = newUrl;
                UpdateOnTvh(mux);
            }
            //TODO: Update EPG tag
        }

        private List<T> ReadFromWeb<T>() where T : TvhObject, new()
        {
            _logger.Debug($"Read {typeof(T).Name} from web at {_settings.WebUrl}");
            var instance = new T();
            var web = _communicationFactory();
            var result = web.GetTableResult<T>(instance.Urls.List);
            if (result?.entries == null)
            {
                _logger.WarnFormat($"Received no awnser from the web interface at {_settings.WebUrl}");
                return new List<T>();
            }
            result.entries.ForEach(obj => obj.State = State.Loaded);
            _logger.InfoFormat("Read {0} {1} from the tvheadend web interface", result.total, typeof(T).Name);
            return result.entries;
        } 
        
        public Mux ResolveMux(string name)
        {
            var mux = _networks.SelectMany(n => n.Muxes).FirstOrDefault(m => m.iptv_muxname == name || m.uuid == name);
            return mux ?? CreateMux(name);
        }

        public Channel ResolveChannel(string name)
        {
            var channel = _channels.FirstOrDefault(c => c.name == name);
            return channel ?? CreateChannel(name);
        }

        public Tag ResolveTag(string name)
        {
            var tag = _tags.FirstOrDefault(c => c.name == name);
            return tag ?? CreateTag(name);
        }

        public Epg FindEpg(string name)
        {
            return _epgs.FirstOrDefault(e => String.Compare(e.name, name, StringComparison.OrdinalIgnoreCase) == 0) ??
                   _epgs.FirstOrDefault(e => String.Compare(e.uuid, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        private Tag CreateTag(string name)
        {
            _logger.InfoFormat("Create new TVH tag for {0}", name);
            var tag = new Tag { name = name };
            _tags.Add(tag);
            return tag;
        }

        private Channel CreateChannel(string name)
        {
            _logger.InfoFormat("Create new TVH channel for {0}", name);
            var channel = new Channel { name = name };
            _channels.Add(channel);
            return channel;
        }

        private Mux CreateMux(string name)
        {
            _logger.InfoFormat("Create new TVH mux with service for {0}", name);
            var mux = new Mux {network_uuid = DefaultNetwork.uuid};
            DefaultNetwork.Muxes.Add(mux);
            return mux;
        }

        private Network CreateNetwork(string name)
        {
            var network = new Network { networkname = name };
            _networks.Add(network);
            return network;
        }
    }
}
