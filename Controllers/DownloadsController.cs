using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto;

namespace MirrorServer.Controllers
{

    [Route("[controller]/")]
    public class DownloadsController : Controller
    {

        private Stream _loaderJarCache;

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

            return File(memory, "application/java-archive", result.Name.ToLower().Replace(" ", "_") + ".jar");
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

            return File(memory, "application/java-archive", result.Name.ToLower().Replace(" ", "_") + ".jar");
        }

        [HttpPost("custom")]
        public async Task<ActionResult> Custom([FromBody] JObject json)
        {
            _loaderJarCache ??= await GetLatestLoader();

            var stream = new MemoryStream();
            await _loaderJarCache.CopyToAsync(stream);
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, true))
            {
                ZipArchiveEntry credentials = archive.CreateEntry("credentials.properties");
                using (StreamWriter writer = new StreamWriter(credentials.Open()))
                {
                    await writer.WriteLineAsync("networkId: " + json["networkId"]);
                    await writer.WriteLineAsync("secret: " + json["secret"]);
                }

                ZipArchiveEntry loader = archive.CreateEntry("loader.yml");
                using (StreamWriter writer2 = new StreamWriter(loader.Open()))
                {
                    await writer2.WriteLineAsync("endpoint: " + json["endpoint"]);
                    await writer2.WriteLineAsync("template: " + json["template"]);
                    await writer2.WriteLineAsync("profile: " + json["profile"]);
                    //await writer2.WriteLineAsync("localProfile: ");
                }

                ReplaceName(archive,"plugin.yml", json["name"].ToString());
                ReplaceName(archive, "bungee.yml", json["name"].ToString());

            }

            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/java-archive", "mcnative-custom-loader.jar");
        }

        private void ReplaceName(ZipArchive archive,string file, string name)
        {
            StringBuilder document;
            ZipArchiveEntry entry = archive.GetEntry(file);
            using (StreamReader reader = new StreamReader(entry.Open()))
            {
                document = new StringBuilder(reader.ReadToEnd());
            }

            entry.Delete();
            entry = archive.CreateEntry(file);
            document.Replace("McNative-Template-Loader", name);

            using (StreamWriter writer = new StreamWriter(entry.Open()))
            {
                writer.Write(document);
            }
        }

        private async Task<Stream> GetLatestLoader()
        {
            HttpWebRequest request = WebRequest.CreateHttp("https://repository.pretronic.net/repository/pretronic/org/mcnative/loader/McNativeResourceLoader/maven-metadata.xml");
            request.Method = "GET";
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            XDocument meta = XDocument.Parse(reader.ReadToEnd());
            string version = meta.Element("metadata").Element("versioning").Element("release").Value;
            reader.Dispose();

            HttpWebRequest request2 = WebRequest.CreateHttp("https://repository.pretronic.net/repository/pretronic/org/mcnative/loader/McNativeResourceLoader/" + version + "/McNativeResourceLoader-" + version + ".jar");
            request2.Method = "GET";

            MemoryStream stream = new MemoryStream();
            WebResponse response2 = request2.GetResponse();
            Stream input = response2.GetResponseStream();
            await input.CopyToAsync(stream);
            await input.DisposeAsync();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private async Task IncrementDownload(string resourceId, string address, string country)
        {
            string hash = sha256(address);
            ResourceDownload download = await _context.ResourceDownloads.FirstOrDefaultAsync(r => r.ResourceId == resourceId && r.IpAddressHash == hash);
            if (download == null)
            {
                download = new ResourceDownload { ResourceId = resourceId, IpAddressHash = hash, Country = country, Time = DateTime.Now };
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