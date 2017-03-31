using System.IO;
using Newtonsoft.Json;

namespace TvHeadendIntegration.TvHeadend
{
    public class TvhJsonTextWriter : JsonTextWriter
    {
        public string NewLine { get; set; }

        public TvhJsonTextWriter(TextWriter textWriter)
            : base(textWriter)
        {
            NewLine = "\n";
            Indentation = 1;
        }

        protected override void WriteIndent()
        {
            if (Formatting != Formatting.Indented) return;

            WriteWhitespace(NewLine);
            for (var i = 0; i < Top; i++)
                WriteRaw("\t");
        }
    }
}