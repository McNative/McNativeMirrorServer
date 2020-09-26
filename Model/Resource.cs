using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace McNativeMirrorServer.Model
{
    public class Resource
    {

        [JsonIgnore]
        public int Id { get; set; }
        public string PublicId { get; set; }
        public string Name { get; set; }
        public string DefaultDownloadEdition { get; set; }

        public bool Licensed { get; set; }

        [InverseProperty("Resource")]
        public virtual ICollection<ResourceVersion> Versions { get; set; }

        [InverseProperty("Resource")]
        public virtual ICollection<ResourceEdition> Editions { get; set; }
    }
}
