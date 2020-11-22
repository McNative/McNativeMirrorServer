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

            Resource result = _context.Resources.FirstOrDefault(resource => resource.Name.ToLower().Equals(name.ToLower()));
            if (result == null || !result.Public)
            {
                 return NotFound();
            }

            string path = Path.Combine(_rootPath, "loaders", result.Id + ".jar");

            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            await using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
                stream.Close();
            }
            memory.Position = 0;
            return File(memory, MediaTypeNames.Application.Zip, result.Name.ToLower().Replace(" ", "_") + ".jar");
        }
    }
}