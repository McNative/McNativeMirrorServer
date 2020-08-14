using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace MirrorServer.Controllers
{
    [Route("[controller]/v1/")]
    [ApiController]
    public class ResourceController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ResourceContext _context;
        private readonly string _rootPath;
        private readonly string _token;

        public ResourceController(IConfiguration config, ResourceContext context)
        {
            _config = config;
            _context = context;
            _rootPath = Environment.GetEnvironmentVariable("PRETRONIC_PATH");
            _token = Environment.GetEnvironmentVariable("PRETRONIC_TOKEN");
        }


        [HttpGet("{resourceId}")]
        public ActionResult Get(string resourceId)
        {
            Resource result = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }


        [HttpGet("{resourceId}/versions/")]
        public ActionResult GetVersions(string resourceId, string qualifier, int limit)
        {
            if (limit == 0) limit = 150;
            Resource result = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));

            if (result == null)
            {
                return NotFound();
            }

            IEnumerable<ResourceVersion> versions = result.Versions;
            if(qualifier != null) versions = versions.Where(v => v.Qualifier == qualifier);
            versions = versions.Take(limit);

            return Ok(versions);
        }

        [HttpGet("{resourceId}/editions/")]
        public ActionResult GetEditions(string resourceId)
        {
            Resource result = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result.Editions);
        }

        [HttpGet("{resourceId}/versions/latest")]
        public ActionResult getLatestVersion(string resourceId, bool plain, string qualifier, bool stable, bool beta)
        {
            Resource result = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));

            if (result == null)
            {
                return NotFound();
            }

            ResourceVersion latest;

            if (stable)
            {
                qualifier = "RELEASE";
            }

            if (beta)
            {
                qualifier = "BETA";
            }

            if (qualifier == null)
            {
                latest = result.Versions.OrderByDescending(version => version.BuildNumber).FirstOrDefault();
            }
            else
            {
                latest = result.Versions.Where(version => version.Qualifier.Equals(qualifier, StringComparison.OrdinalIgnoreCase)).OrderByDescending(version => version.BuildNumber).FirstOrDefault();
            }

            if (latest == null)
            {
                return NotFound();
            }

            if (plain)
            {
                return Content(latest.Name + ";" + latest.BuildNumber + ";" + latest.Qualifier + ";" + latest.Time);
            }

            return Ok(latest);
        }

        [HttpGet("{resourceId}/versions/{buildId}/download")]
        public async Task<ActionResult> DownloadVersion(string resourceId,int buildId,string edition
            ,[FromHeader] string serverId, [FromHeader] string serverSecret
            ,[FromHeader] string rolloutId, [FromHeader] string rolloutSecret)
        {
            Resource resource = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));
            if (resource == null)
            {
                return NotFound();
            }

            if(resource.Licensed)
            {
                Organisation organisation = null;
                if(rolloutId != null)
                {
                    if(rolloutSecret == null) return Unauthorized("Rollout id or secret is missing");
                    RolloutServer server = _context.RolloutServers.SingleOrDefault(server => server.Id == rolloutId);
                    if (server == null || !server.Secret.Equals(rolloutSecret)) return Unauthorized("Invalid server id or secret");
                    organisation = server.Organisation;
                }
                else if(serverId != null)
                {
                    if(serverSecret == null) return Unauthorized("Server id or secret is missing");
                    Server server = _context.Servers.SingleOrDefault(server => server.Id == serverId);
                    if (server == null || !server.Secret.Equals(serverSecret)) return Unauthorized("Invalid server id or secret");
                    organisation = server.Organisation;
                }
                else
                {
                    return Unauthorized("Missing authentication credentials");
                }


                License license = organisation.Licenses.FirstOrDefault(license => license.ResourceId == resource.Id);

                if(license == null || license.Disabled || (license.Expiry != null && license.Expiry < DateTime.Now))
                {
                    return Unauthorized("Resource not licensed to server");
                }
            } 

            ResourceVersion version = resource.Versions.Where(version => version.BuildNumber == buildId).FirstOrDefault();
            if (version == null)
            {
                return NotFound();
            }

            if (edition == null) edition = "default";

            ResourceEdition edition0 = resource.Editions.Where(edition0 => string.Equals(edition0.Name, edition, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (edition0 == null)
            {
                return NotFound();
            }

            string path = Path.Combine(_rootPath, resource.PublicId, "resource-" + version.Id + "-" + edition0.Id + ".jar");

            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
                stream.Close();
            }
            memory.Position = 0;
            return File(memory, MediaTypeNames.Application.Zip, resource.Name+" v"+version.Name+".jar");
        }


        [HttpPost("{resourceId}/versions/create")]
        public async Task<ActionResult> CreateVersion(string resourceId, string name, string qualifier, int buildNumber,[FromHeader]string token)
        {
            if (!_token.Equals(token))return Unauthorized();

            Resource result = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));
            if (result == null)
            {
                return NotFound();
            }
            ResourceVersion version = new ResourceVersion();
            version.Name = name;
            version.Qualifier = qualifier;
            version.BuildNumber = buildNumber;
            version.ResourceId = result.Id;
            version.Time = DateTime.Now;

            await _context.AddAsync(version);
            await _context.SaveChangesAsync();

            return Ok(version);
        }


        [HttpPost("{resourceId}/versions/{buildId}/publish")]
        public async Task<ActionResult> PublishVersion(string resourceId, int buildId, [FromForm(Name = "file")] IFormFile upload, string edition, [FromHeader]string token)
        {
            if (!_token.Equals(token)) return Unauthorized();

            Resource result = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));
            if (result == null)
            {
                return NotFound();
            }

            ResourceVersion version = result.Versions.Where(version => version.BuildNumber == buildId).FirstOrDefault();
            if (version == null)
            {
                return NotFound();
            }

            ResourceEdition edition0 = result.Editions.Where(edition0 => string.Equals(edition0.Name, edition, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (edition0 == null)
            {
                return NotFound();
            }

            string path = Path.Combine(_rootPath, result.PublicId, "resource-" + version.Id + "-" + edition0.Id + ".jar");

            string directory = Path.Combine(_rootPath, result.PublicId);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            FileStream stream = System.IO.File.OpenWrite(path);
            await upload.CopyToAsync(stream);

            stream.Close();

            return Ok();
        }

    }
}
