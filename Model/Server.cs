using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace McNativeMirrorServer.Model
{
    public class Server
    {
        public string Id { get; set; }

        public string OwnerId { get; set; }

        public string Name { get; set; }

        public string PublicIp { get; set; }

        [JsonIgnore]
        public string Secret { get; set; }

        [InverseProperty("Server")]
        public virtual ICollection<License> Licenses { get; set; }
    }
}
