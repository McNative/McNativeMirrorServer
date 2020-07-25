using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class ResourceVersion
    {

        public int Id { get; set; }

        [JsonIgnore]
        public int? ResourceId { get; set; }

        public string Name { get; set; }

        public string Qualifier { get; set; }

        public int BuildNumber { get; set; }

        public DateTime? Time { get; set; }

        [JsonIgnore]
        [ForeignKey("ResourceId")]
        public virtual Resource? Resource { get; set; }

    }
}
