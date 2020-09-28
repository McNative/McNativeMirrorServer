using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace McNativeMirrorServer.Model
{
    public class ResourceEdition
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public string ResourceId { get; set; }

        public bool IsAvailableAsDownload { get; set; }

        [JsonIgnore]
        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }
    }
}
