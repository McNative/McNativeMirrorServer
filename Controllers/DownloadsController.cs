using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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

            HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var address);
            HttpContext.Request.Headers.TryGetValue("CF-IPCountry", out var country);
            await Task.Run(() => IncrementDownload(result.Id, address, country));

            return File(memory, MediaTypeNames.Application.Zip, result.Name.ToLower().Replace(" ", "_") + ".jar");
        }

        [HttpGet("id/{id}")]
        public async Task<ActionResult> downloadById(string id)
        {
            Resource result = _context.Resources.FirstOrDefault(resource => resource.Id == id);

            if (result == null || !(result.BuildLoader || result.Public))
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

            HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var address);
            HttpContext.Request.Headers.TryGetValue("CF-IPCountry", out var country);
            await Task.Run(() => IncrementDownload(result.Id, address, country));

            return File(memory, MediaTypeNames.Application.Zip, result.Name.ToLower().Replace(" ", "_") + ".jar");
        }

        private async Task IncrementDownload(string resourceId, string address,string country)
        {
            string hash = sha256(address);
            ResourceDownload download = await _context.ResourceDownloads.FirstOrDefaultAsync(r => r.ResourceId == resourceId && r.IpAddressHash == hash);
            if (download == null)
            {
                download = new ResourceDownload {ResourceId = resourceId, IpAddressHash = hash,Country = country, Time = DateTime.Now};
                await _context.ResourceDownloads.AddAsync(download);
                await _context.SaveChangesAsync();
            }
        }
        static string sha256(string randomString)
        {
            var crypt = new SHA256Managed();
            string hash = String.Empty;
            byte[] crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(randomString));
            foreach (byte theByte in crypto)
            {
                hash += theByte.ToString("x2");
            }
            return hash;
        }
    }
}