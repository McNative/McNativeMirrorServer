using System.Linq;
using System.Threading.Tasks;
using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace McNativeMirrorServer.Controllers
{
    [Route("resources/v1/rollout")]
    public class RolloutController : Controller
    {

        private readonly ResourceContext _context;

        public RolloutController(ResourceContext context)
        {
            _context = context;
        }

        [HttpGet("checkout")]
        public async Task<IActionResult> Checkout([FromHeader] string rolloutServerId, [FromHeader] string rolloutServerSecret, [FromHeader] string rolloutServerEndpoint)
        { 
            if(rolloutServerId == null || rolloutServerSecret == null) return Unauthorized("Missing or wrong authentication credentials");

            RolloutServer server = await _context.RolloutServers.SingleOrDefaultAsync(server => server.Id == rolloutServerId);
            if (server == null || !server.Secret.Equals(rolloutServerSecret)) return Unauthorized("Missing or wrong authentication credentials");

            Organisation organisation0 = server.Organisation;
            if (organisation0 == null) return Unauthorized("Missing or wrong authentication credentials");

            server.Endpoint = rolloutServerEndpoint;
            _context.Update(server);
            await _context.SaveChangesAsync();

            string organisation = organisation0.Id;

            var templates = _context.Templates.Where(o => o.OrganisationId == organisation)
                .Select(o => new { o.Id, o.Name, Configuration = JObject.Parse(o.Configuration), Definition = o.TemplateDefinition }).ToList();

            var profiles = _context.Profiles.Where(o => o.OrganisationId == organisation)
                .Select(o => new { o.Id, o.Name, Configuration = JObject.Parse(o.Configuration), Definition = o.ProfileDefinition }).ToList();

            var credentials = _context.Servers.Where(o => o.OrganisationId == organisation)
                .Select(o => new { o.Id, Hash = o.SecretHash }).ToList();

            return Ok(new {templates,profiles,credentials});
        }
    }
}