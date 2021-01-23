using System.Threading.Tasks;
using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MirrorServer.Controllers
{
    [Route("resources/v1/templates")]
    public class TemplateController : Controller
    {
        private readonly ResourceContext _context;
        
        public TemplateController(ResourceContext context)
        {
            _context = context;
        }

        [HttpGet("{templateName}")]
        public async Task<IActionResult> GetTemplate(string templateName,
            [FromHeader] string networkId, [FromHeader] string networkSecret
            ,[FromHeader] string rolloutServerId, [FromHeader] string rolloutServerSecret, bool plain) {
            Organisation organisation = await Organisation.FindByCredentials(_context,networkId,networkSecret,rolloutServerId, rolloutServerSecret);
            if (organisation == null)  return Unauthorized("Missing or wrong credentials");

            Template template = await  _context.Templates.FirstOrDefaultAsync(t => t.OrganisationId == organisation.Id && t.Name == templateName);
            if (template == null) return NotFound();

            if (plain) return Ok(template.TemplateDefinition);
            else return Ok(template.Configuration);
        }
    }
}