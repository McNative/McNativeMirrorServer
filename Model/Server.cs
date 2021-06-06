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

        public string OrganisationId { get; set; }

        public string Name { get; set; }

        public string PublicIp { get; set; }

        [JsonIgnore]
        public string Secret { get; set; }

        [JsonIgnore]
        public string SecretHash { get; set; }

        [JsonIgnore]
        [ForeignKey("OrganisationId")]
        public virtual Organisation? Organisation { get; set; }
    }
}
