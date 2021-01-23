using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace MirrorServer.Controllers
{
    [Route("resources/v1/profiles")]
    public class ProfileController : Controller
    {
        private readonly ResourceContext _context;
        
        public ProfileController(ResourceContext context)
        {
            _context = context;
        }

        [HttpGet("/")]
        public async Task<IActionResult> GetProfiles([FromHeader] string networkId, [FromHeader] string networkSecret
            , [FromHeader] string rolloutServerId, [FromHeader] string rolloutServerSecret) {
            Organisation organisation = await Organisation.FindByCredentials(_context, networkId, networkSecret, rolloutServerId, rolloutServerSecret);
            if (organisation == null) return Unauthorized("Missing or wrong credentials");

            List<RolloutProfile> profiles = await _context.Profiles.Where(p => p.OrganisationId == organisation.Id).ToListAsync();

            JObject data = new JObject();

            JObject profiles2 = new JObject();
            foreach (RolloutProfile profile in profiles)
            {
                profiles2[profile.Name] = JObject.Parse(profile.Configuration);
            }

            data["profiles"] = profiles2;
            
            return Ok(data);
        }

        [HttpGet("{profileName}")]
        public async Task<IActionResult> GetProfile(string profileName,
            [FromHeader] string networkId, [FromHeader] string networkSecret
            ,[FromHeader] string rolloutServerId, [FromHeader] string rolloutServerSecret, bool plain) {
            Organisation organisation = await Organisation.FindByCredentials(_context,networkId,networkSecret,rolloutServerId, rolloutServerSecret);
            if (organisation == null)  return Unauthorized("Missing or wrong credentials");

            RolloutProfile profile = await _context.Profiles.FirstOrDefaultAsync(p => p.OrganisationId == organisation.Id && p.Name == profileName);
            if (plain) return Ok(profile.ProfileDefinition);
            else return Ok(profile.Configuration);

        }
    }
}