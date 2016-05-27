using System;

namespace SharedComponents.Module
{
    public class TvHeadendIntegrationInfo : IModuleInfo, ICloneable
    {
        public DateTime? LastEpgUpdate;
        public bool LastEpgUpdateSuccessfull;
        public object Clone()
        {
            return new TvHeadendIntegrationInfo
            {
                LastEpgUpdate = LastEpgUpdate,
                LastEpgUpdateSuccessfull = LastEpgUpdateSuccessfull
            };
        }
    }
}