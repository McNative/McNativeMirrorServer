using System;

namespace McNativeMirrorServer.Model
{
    public class SubscriptionActive
    {
        public string Id { get; set; }

        public string OrganisationId { get; set; }

        public string SubscriptionId { get; set; }

        public DateTime? Expiry { get; set; }

        public bool Disabled { get; set; }
    }
}
