using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace McNativeMirrorServer.Model
{
    public class Resource
    {

        public string Id { get; set; }

        public string OwnerId { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        [JsonIgnore]
        public string PrivateKey { get; set; }

        public bool Public { get; set; }
        public bool Licensed { get; set; }

        [JsonIgnore]
        public bool BuildLoader { get; set; }

        [JsonIgnore]
        public bool AliveReportingEnabled { get; set; }

        [JsonIgnore]
        public string DeploySecret { get; set; }

        [ForeignKey("OwnerId")]
        public virtual Organisation Owner { get; set; }

        [InverseProperty("Resource")]
        public virtual ICollection<ResourceVersion> Versions { get; set; }

        [InverseProperty("Resource")]
        public virtual ICollection<ResourceEdition> Editions { get; set; }

    }
}
