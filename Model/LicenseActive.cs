using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class LicenseActive
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public string Key { get; set; }

        public string LicenseId { get; set; }

        public string OrganisationId { get; set; }

        public bool Disabled { get; set; }

        public DateTime? Expiry { get; set; }

        [ForeignKey("LicenseId")]
        public virtual License License { get; set; }

        [JsonIgnore]
        [ForeignKey("OrganisationId")]
        public virtual Organisation Organisation { get; set; }

    }
}
