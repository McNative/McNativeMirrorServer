
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using McNativeMirrorServer.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McNativeMirrorServer.Services
{
    public class ResourceLoaderBuildService : IHostedService, IDisposable
    {

        private readonly ILogger<ResourceLoaderBuildService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private Timer _timer;

        public ResourceLoaderBuildService(ILogger<ResourceLoaderBuildService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Resource loader build service is starting.");

            _timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));

            return Task.CompletedTask;
        }

        private void Execute(object state)
        {
            using var scope = _scopeFactory.CreateScope();
            ResourceContext context = scope.ServiceProvider.GetRequiredService<ResourceContext>();

            string version = GetLatestLoaderVersion();

            IQueryable<string> loaders = context.SystemLoaders.Select(l => l.ResourceId);
            List<Resource> resources = context.Resources.Where(r => (r.Public || r.BuildLoader) && !loaders.Contains(r.Id)).Take(1000).ToList();//@Todo Temp solution, change to partial loading

            foreach (Resource resource in resources)
            {
                SystemLoaders loader = new SystemLoaders
                {
                    ResourceId = resource.Id,
                    Version = version,
                    Status = "RUNNING",
                    FirstBuild = null,
                    LastBuild = null
                }; 
                context.Add(loader);
                context.SaveChanges();
                Organisation owner = resource.Owner;
                TriggerBuild(resource.Name, resource.Id, owner.Name, owner.Website, resource.Description, version, resource.LoaderInstallMcNative.ToString().ToLower());
                Thread.Sleep(2000);
            }

            List<SystemLoaders> loadersToUpdate = context.SystemLoaders.Where(l => l.Version != version).Take(1000).ToList();
            foreach (SystemLoaders loader in loadersToUpdate)
            {
                loader.Version = version;
                loader.Status = "Running";
                context.Update(loader);
                context.SaveChanges();
                Resource resource = loader.Resource;

                if (resource == null)
                {
                    context.Remove(loader);
                    context.SaveChanges();
                    continue;
                }

                Organisation owner = resource.Owner;
                TriggerBuild(resource.Name, resource.Id, owner.Name, owner.Website, resource.Description,version,resource.LoaderInstallMcNative.ToString().ToLower());
                Thread.Sleep(2000);
            }

        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Resource loader build service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void TriggerBuild(string name,string resourceId,string author, string website, string description,string version,string installMcNative)
        {
            string url = "https://ci.pretronic.net/job/McNativeLoaderGenerationTemplate/buildWithParameters?token=wYiyVzmjBT1J4GsuiBtBjtSOKbfcYvg7h&name="+name+"&author="+author+"&resourceId="+resourceId+"&website="+website + "&version=" + version + "&installMcNative=" + installMcNative + "&description="+description;
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";

            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("ci:115ef04d26cb8555f6e378c1d2793c8cac"));
            request.Headers.Add("Authorization", "Basic " + encoded);

            request.GetResponse().Close();
        }
        private string GetLatestLoaderVersion()
        {
            HttpWebRequest request = WebRequest.CreateHttp("https://repository.pretronic.net/repository/pretronic/org/mcnative/loader/McNativeResourceLoader/maven-metadata.xml");
            request.Method = "GET";
            WebResponse response = request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            XDocument meta = XDocument.Parse(reader.ReadToEnd());
            return meta.Element("metadata").Element("versioning").Element("release").Value;
        }
    }
}
