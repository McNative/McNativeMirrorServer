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
        public virtual ICollection<LicenseActive> Licenses { get; set; }


        public static async Task<Organisation> FindByRolloutCredentials(ResourceContext context, string rolloutServerId, string rolloutServerSecret)
        {
            RolloutServer server = await context.RolloutServers.SingleOrDefaultAsync(server => server.Id == rolloutServerId);
            if (server == null || !server.Secret.Equals(rolloutServerSecret)) return null;
            return server.Organisation;
        }

        public static async Task<Organisation> FindByNetworkCredentials(ResourceContext context, string networkId, string networkSecret)
        {
            Server server = await context.Servers.SingleOrDefaultAsync(server => server.Id == networkId);
            if (server == null || !server.Secret.Equals(networkSecret)) return null;
            return server.Organisation;
        }

        public static async Task<Organisation> FindByCredentials(ResourceContext context, string networkId, string networkSecret, string rolloutServerId, string rolloutServerSecret)
        {
            if (rolloutServerId != null) return await FindByRolloutCredentials(context, rolloutServerId, rolloutServerSecret);
            if (networkId != null) return await FindByNetworkCredentials(context, networkId, networkSecret);
            return null;
        }

        public async Task<LicenseActive> FindLicense(ResourceContext context, string resourceId)
        {
            return (from active in context.ActiveLicense
                join resource in context.LicenseResources on active.LicenseId equals resource.LicenseId
                where active.OrganisationId == Id && resource.Id == resourceId && !active.Disabled && (active.Expiry == null || active.Expiry > DateTime.Now)
                select active).FirstOrDefault();
        }
    }
}
