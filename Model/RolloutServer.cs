using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class RolloutServer
    {
        public string Id { get; set; }

        [JsonIgnore]
        public string Secret { get; set; }

        [JsonIgnore]
        [ForeignKey("OrganisationId")]
        public virtual Organisation Organisation { get; set; }
    }
}
