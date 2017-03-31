using System;
using System.Collections.Generic;
using System.Linq;
using TvHeadendIntegration.TvHeadend.Web;
using log4net;

namespace TvHeadendIntegration.TvHeadend
{
    public enum State { New, Loaded, Created, Updated, Removed }

    public class TvhConfiguration
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TvhConfiguration));
        
        private List<Network> _networks = new List<Network>();
        private List<Channel> _channels = new List<Channel>();
        private List<Tag> _tags = new List<Tag>();
        private List<Epg> _epgs = new List<Epg>(); 
        private string _tvhFolder = string.Empty;
        private string _defaultNetworkName = string.Empty;

        private Network DefaultNetwork
        {
            get
            {
                var network = _networks.FirstOrDefault(n => n.networkname.Equals(_defaultNetworkName, StringComparison.OrdinalIgnoreCase));
                return network ?? CreateNetwork(_defaultNetworkName);
            }
        }

        public static TvhConfiguration ReadFromWeb(Settings settings)
        {
            var config = new TvhConfiguration
            {
                _defaultNetworkName = settings.TvhNetworkName,
                _networks = ReadFromWeb<Network>(settings),
                _channels = ReadFromWeb<Channel>(settings),
                _tags = ReadFromWeb<Tag>(settings),
                _epgs = ReadFromWeb<Epg>(settings)
                //TODO: Ik probeer de EPG tabel op te halen via het web
            };
            var muxes = ReadFromWeb<Mux>(settings);
            var services = ReadFromWeb<Service>(settings);
            muxes.ForEach(mux => mux.Services.AddRange(services.Where(service => service.multiplex_uuid == mux.uuid)));
            config._networks.ForEach(network => network.Muxes.AddRange(muxes.Where(mux => mux.network_uuid == network.uuid)));
            return config;
        }

        private static List<T> ReadFromWeb<T>(object tvheadendHostAddress)
        {
            throw new NotImplementedException();
        }

        private static List<T> ReadFromWeb<T>(Settings settings) where T : TvhObject, new()
        {
            Logger.Debug($"Read {typeof(T).Name} from web at {settings.WebUrl}");
            var instance = new T();
            var web = new TvhCommunication(Logger, settings);
            var result = web.GetTableResult<T>(instance.Urls.List);
            if (result == null)
            {
                Logger.WarnFormat($"Received no awnser from the web interface at {settings.WebUrl}");
                return new List<T>();
            }
            result.entries.ForEach(obj => obj.State = State.Loaded);
            Logger.InfoFormat("Read {0} {1} from the tvheadend web interface", result.total, typeof(T).Name);
            return result.entries;
        } 
        
        public Mux ResolveMux(string name, int nrOfExtraServices)
        {
            var mux = _networks.SelectMany(n => n.Muxes).FirstOrDefault(m => m.iptv_sname == name);
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
            Logger.InfoFormat("Create new TVH tag for {0}", name);
            var tag = new Tag { name = name };
            _tags.Add(tag);
            return tag;
        }

        private Channel CreateChannel(string name)
        {
            Logger.InfoFormat("Create new TVH channel for {0}", name);
            var channel = new Channel { name = name };
            _channels.Add(channel);
            return channel;
        }

        private Mux CreateMux(string name)
        {
            Logger.InfoFormat("Create new TVH mux with service for {0}", name);
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
