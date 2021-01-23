using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class SubscriptionActive
    {
        [Key]
        public string Id { get; set; }

        public string OrganisationId { get; set; }

        public string SubscriptionId { get; set; }

        public DateTime? Expiry { get; set; }

        public bool Disabled { get; set; }

        [ForeignKey("SubscriptionId")]
        public virtual Subscription Subscription { get; set; }
    }
}
