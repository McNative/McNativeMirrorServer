using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace McNativeMirrorServer.Model
{
    public class Organisation
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [InverseProperty("Organisation")]
        public virtual ICollection<License> Licenses { get; set; }
    }
}
