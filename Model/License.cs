using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class License
    {
        public string Id { get; set; }

        public string OrganisationId { get; set; }

        public string ResourceId { get; set; }

        public bool Disabled { get; set; }

        public string ManagedBySubscriptionId { get; set; }

        public DateTime? Expiry { get; set; }

        public int MaxInstances { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }

        [JsonIgnore]
        [ForeignKey("OrganisationId")]
        public virtual Organisation Organisation { get; set; }
    }
}
