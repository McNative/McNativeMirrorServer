using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace McNativeMirrorServer.Model
{
    public class Organisation
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Website { get; set; }

        [InverseProperty("Organisation")]
        public virtual ICollection<License> Licenses { get; set; }

        public async Task<License> FindLicense(ResourceContext context,string resourceId)
        {
            License license = Licenses.FirstOrDefault(l => l.ResourceId == resourceId && !l.Disabled && (l.Expiry == null || l.Expiry > DateTime.Now));

            if (license != null)
            {
                return license;
            }

            IQueryable<string> containing = context.SubscriptionResources.Where(c => c.ResourceId == resourceId).Select(c => c.SubscriptionId);

            SubscriptionActive subscription =
                await context.SubscriptionActives.Where(s => s.OrganisationId == Id && containing.Contains(s.SubscriptionId) && !s.Disabled && (s.Expiry == null || s.Expiry > DateTime.Now)).FirstOrDefaultAsync();

            if (subscription == null)
            {
                return null;
            }

            license = new License()
            {
                OrganisationId = Id,
                ResourceId = resourceId,
                Expiry = subscription.Expiry,
                Disabled = false,
                ManagedBySubscriptionId = subscription.SubscriptionId
            };
            await context.AddAsync(license);
            await context.SaveChangesAsync();
            return license;
        }

    }
}
