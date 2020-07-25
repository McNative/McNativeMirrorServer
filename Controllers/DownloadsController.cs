using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;

namespace MirrorServer.Controllers
{

    [Route("[controller]/")]
    public class DownloadsController : Controller
    {

        private readonly string _rootPath;
        private readonly ResourceContext _context;

        public DownloadsController(ResourceContext context)
        {
            _context = context;
            _rootPath = Environment.GetEnvironmentVariable("PRETRONIC_PATH");
        }

        [HttpGet("{name}")]
        public Task<ActionResult> download(string name)
        {
            return download(name, null);
        }


        [HttpGet("{name}/{edition}")]
        public async Task<ActionResult> download(string name, string edition)
        {
            Resource result = _context.Resources.SingleOrDefault(resource => resource.Name.ToLower().Equals(name.ToLower()));
            if (result == null)
            {
                return NotFound();
            }

            if(edition == null)
            {
                if (result.DefaultDownloadEdition == null)
                {
                    return NotFound();
                }
                else
                {
                    edition = result.DefaultDownloadEdition;
                }
            }

            ResourceVersion version = result.Versions.Where(version => version.Qualifier == "RELEASE").OrderByDescending(version => version.BuildNumber).FirstOrDefault();
            if (version == null)
            {
                return NotFound();
            }

            ResourceEdition edition0 = result.Editions.Where(edition0 => edition0.Name.ToLower().Equals(edition)).FirstOrDefault();
            if (edition0 == null || !edition0.IsAvailableAsDownload)
            {
                return NotFound();
            }

            string path = Path.Combine(_rootPath, "resource-" + result.PublicId + "-" + version.Id + "-" + edition0.Id + ".jar");

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
                stream.Close();
            }
            memory.Position = 0;
            return File(memory, MediaTypeNames.Application.Zip, result.Name + " v" + version.Name + ".jar");
        }
    }
}