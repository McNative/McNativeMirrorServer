using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MirrorServer.Controllers
{
    [Route("resources/v1/")]
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
            Resource result = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));

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
            Resource result = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));

            if (result == null)
            {
                return NotFound();
            }

            IEnumerable<ResourceVersion> versions = result.Versions.OrderByDescending(t => t.BuildNumber);
            if(qualifier != null) versions = versions.Where(v => v.Qualifier == qualifier);
            versions = versions.Take(limit);

            return Ok(versions);
        }

        [HttpGet("{resourceId}/editions/")]
        public ActionResult GetEditions(string resourceId)
        {
            Resource result = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result.Editions);
        }

        [HttpGet("{resourceId}/versions/latest")]
        public ActionResult GetLatestVersion(string resourceId, bool plain, string qualifier, bool stable, bool beta)
        {
            Resource result = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));

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
            Resource resource = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));
            if (resource == null)
            {
                return NotFound();
            }

            if(resource.Licensed)
            {
                Organisation organisation;
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

            string path = Path.Combine(_rootPath, resource.Id, "resource-" + version.Id + "-" + edition0.Id + ".jar");

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

            Resource result = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));
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

            Resource result = await _context.Resources.FirstOrDefaultAsync(resource => resource.Id.Equals(resourceId));//@Todo check for public
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

            string path = Path.Combine(_rootPath, result.Id, "resource-" + version.Id + "-" + edition0.Id + ".jar");

            string directory = Path.Combine(_rootPath, result.Id);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            FileStream stream = System.IO.File.OpenWrite(path);
            await upload.CopyToAsync(stream);

            stream.Close();

            return Ok();
        }

        [HttpPost("{resourceId}/deploy")]
        public async Task<ActionResult> DeployResource(string resourceId, string name, string qualifier, string edition, int buildNumber, [FromForm(Name = "File")] IFormFile upload, [FromHeader] string secret)
        {
            Resource result = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));
            if (result == null)
            {
                return NotFound("Resource not found");
            }

            if (!result.DeploySecret.Equals(secret))
            {
                return Unauthorized("Invalid resource secret");
            }

            ResourceVersion version = result.Versions.FirstOrDefault(version => version.Name == name);
            if (version == null)
            { 
                version = new ResourceVersion();
                version.Name = name;
                version.Qualifier = qualifier.ToUpper();
                version.BuildNumber = buildNumber == -1 ? GetNextBuildNumber(resourceId) : buildNumber;
                version.ResourceId = result.Id;
                version.Time = DateTime.Now;

                await _context.AddAsync(version);
                await _context.SaveChangesAsync();
            }
            else if(!version.Qualifier.Equals(qualifier,StringComparison.InvariantCultureIgnoreCase) || (version.BuildNumber != -1 &&  version.BuildNumber != buildNumber))
            {
                return BadRequest("There is already an existing version with a different qualifier or build number");
            }

            ResourceEdition edition0 = result.Editions.FirstOrDefault(edition0 => edition0.Name == edition);
            if (edition0 == null)
            {
                return NotFound("Edition "+edition+" does not exist (Please create a new edition in the McNative console)");
            }

            string path = Path.Combine(_rootPath, result.Id, "resource-" + version.Id + "-" + edition0.Id + ".jar");

            string directory = Path.Combine(_rootPath, result.Id);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            FileStream stream = System.IO.File.OpenWrite(path);
            await upload.CopyToAsync(stream);

            stream.Close();

            return Ok();
        }

        private int GetNextBuildNumber(string resourceId)
        {
            ResourceVersion version = _context.ResourceVersions.OrderByDescending(v => v.BuildNumber)
                .FirstOrDefault(v => v.ResourceId == resourceId);

            if (version == null)
            {
                return 1;
            }

            return version.BuildNumber+1;
        }

    }
}
