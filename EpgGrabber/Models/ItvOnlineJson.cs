using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpgGrabber.Models
{
    public class PackageList
    {
        public string packageId { get; set; }
        public string packageName { get; set; }
        public string packageType { get; set; }
    }

    public class ProgramList
    {
        public string channelId { get; set; }
        public string contentDescription { get; set; }
        public int contentId { get; set; }
        public int duration { get; set; }
        public int endTime { get; set; }
        public string episodeId { get; set; }
        public string eventType { get; set; }
        public string externalChannelId { get; set; }
        public string externalContentId { get; set; }
        public bool isCatchUp { get; set; }
        public string isEncrypted { get; set; }
        public string isHD { get; set; }
        public string isMultiAngleEvent { get; set; }
        public bool isRecordable { get; set; }
        public bool isStartOver { get; set; }
        public List<PackageList> packageList { get; set; }
        public string pcExtendedRatings { get; set; }
        public int pcLevelEpg { get; set; }
        public int programId { get; set; }
        public string season { get; set; }
        public string seriesId { get; set; }
        public string shortDescription { get; set; }
        public int startTime { get; set; }
        public int subscriptionId { get; set; }
        public string subtitle { get; set; }
        public string title { get; set; }
        public string trailerId { get; set; }
        public string urlPictures { get; set; }
    }

    public class ChannelList
    {
        public string channelId { get; set; }
        public string channelName { get; set; }
        public List<ProgramList> programList { get; set; }
    }

    public class ResultObj
    {
        public List<ChannelList> channelList { get; set; }
    }

    public class ItvOnlineJson
    {
        public string errorDescription { get; set; }
        public string message { get; set; }
        public string resultCode { get; set; }
        public ResultObj resultObj { get; set; }
        public int systemTime { get; set; }
    }
}
