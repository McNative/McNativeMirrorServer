using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MirrorServer.Controllers
{

    [Route("[controller]/")]
    public class ResourceLoaderBuildCallbackController : Controller
    {
        private readonly ResourceContext _context;
        private readonly string _rootPath;
        private readonly string _token;

        public ResourceLoaderBuildCallbackController(ResourceContext context)
        {
            _context = context;
            _rootPath = Environment.GetEnvironmentVariable("PRETRONIC_PATH");
            _token = Environment.GetEnvironmentVariable("PRETRONIC_TOKEN");
            
            string directory = Path.Combine(_rootPath, "loaders"); 
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }

        [HttpPost]
        public async Task<ActionResult> Index(string resourceId, [FromForm(Name = "File")] IFormFile upload, [FromHeader] string secret)
        {
            if (!_token.Equals(secret))
            {
                return Unauthorized("Invalid resource secret");
            }

            SystemLoaders loader = await _context.SystemLoaders.FirstOrDefaultAsync(l => l.ResourceId == resourceId);
            if (loader == null)
            {
                return NotFound();
            }

            string path = Path.Combine(_rootPath, "loaders", resourceId + ".jar");
            FileStream stream = System.IO.File.OpenWrite(path);
            await upload.CopyToAsync(stream);
            stream.Close();

            if(loader.FirstBuild == null) loader.FirstBuild = DateTime.Now;
            loader.LastBuild = DateTime.Now;
            loader.Status = "FINISHED";
            _context.Update(loader);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}