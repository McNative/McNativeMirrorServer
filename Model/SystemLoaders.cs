using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class SystemLoaders
    {
        [Key]
        public string ResourceId { get; set; }

        public string Status { get; set; }

        public string Version { get; set; }

        public DateTime? FirstBuild { get; set; }

        public DateTime? LastBuild { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }
    }
}
