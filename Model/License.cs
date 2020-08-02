using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace McNativeMirrorServer.Model
{
    public class License
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string ServerId { get; set; }

        public int ResourceId { get; set; }

        public bool Disabled { get; set; }

        public DateTime? Expiry { get; set; }

        public DateTime? Registered { get; set; }

        public int MaxInstances { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }

        [JsonIgnore]
        [ForeignKey("ServerId")]
        public virtual Server? Server { get; set; }


       // [InverseProperty("License")]
        //public virtual ICollection<ActiveLicense> ActiveLicenses { get; set; }
    }
}
