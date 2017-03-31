using System.Collections.Generic;

namespace TvHeadendIntegration.TvHeadend.Web
{
    public class TvhTable<T>
    {
        public List<T> entries { get; set; }
        public int total { get; set; }
    }
}