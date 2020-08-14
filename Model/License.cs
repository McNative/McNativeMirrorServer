using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class License
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public int ResourceId { get; set; }

        public bool Disabled { get; set; }

        public DateTime? Expiry { get; set; }

        public DateTime? Registered { get; set; }

        public DateTime? ActivationTime { get; set; }

        public int MaxInstances { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }

        [JsonIgnore]
        [ForeignKey("OrganisationId")]
        public virtual Organisation? Organisation { get; set; }
    }
}
