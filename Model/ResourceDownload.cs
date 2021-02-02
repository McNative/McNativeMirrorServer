using System;

namespace McNativeMirrorServer.Model
{
    public class ResourceDownload
    {

        public string ResourceId { get; set; }

        public string IpAddressHash { get; set; }

        public string Country { get; set; }

        public DateTime Time { get; set; }
    }
}
