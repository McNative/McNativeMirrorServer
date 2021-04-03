using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using McNativeMirrorServer;
using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace MirrorServer.Controllers
{
    [Route("resources/v1/licenses")]
    public class LicenseController : Controller
    {
        private readonly ResourceContext _context;
        
        public LicenseController(ResourceContext context)
        {
            _context = context;
        }

        [HttpGet("download/{key}")]
        public async Task<IActionResult> Download(string key)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(key);
            await writer.FlushAsync();
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, "application/octet-stream","license.key");
        }


        [HttpGet("{resourceId}/checkout")]
        public async Task<IActionResult> Checkout(string resourceId, [FromHeader] string deviceId,
            [FromHeader] string networkId, [FromHeader] string networkSecret
            ,[FromHeader] string rolloutServerId, [FromHeader] string rolloutServerSecret
            ,[FromHeader] string licenseKey)
        {
            if (deviceId == null) return BadRequest();
            Resource resource = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));
            if (resource == null)
            {
                return NotFound();
            }
            if(resource.Licensed)
            {
                LicenseActive license;
                if (licenseKey != null)
                {
                    license = await _context.ActiveLicense.FirstOrDefaultAsync(l => l.Key == licenseKey);
                    if (license == null) return Unauthorized("Invalid license key");
                }
                else
                {
                    Organisation organisation = await Organisation.FindByCredentials(_context, networkId, networkSecret, rolloutServerId, rolloutServerSecret);
                    if (organisation == null) return Unauthorized("Missing or wrong authentication credentials");
                    
                    license = await organisation.FindLicense(_context, resourceId);
                    if (license == null) return Unauthorized("Resource not licensed to organization");
                }

                var now = DateTime.Now;
                var expiry = now.AddDays(7);
                if (license.Expiry != null && expiry > license.Expiry) expiry = license.Expiry.Value;

                var preferredRefreshTime = now.AddDays(3);

                LicenseIssued issuedLicense = await _context.LicenceIssued.Where(a => a.ActiveLicenseId == license.Id &&  a.DeviceId == deviceId).FirstOrDefaultAsync();
                if (issuedLicense == null)
                {
                    issuedLicense = new LicenseIssued
                    {
                        ActiveLicenseId = license.Id,
                        DeviceId = deviceId,
                        RequestAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        Expiry = expiry,
                        CheckoutTime = now
                    };
                    await _context.AddAsync(issuedLicense);
                }
                else
                {
                    issuedLicense.RequestAddress = HttpContext.Connection.RemoteIpAddress.ToString();
                    issuedLicense.CheckoutTime = now;
                    issuedLicense.Expiry = expiry;
                    _context.Update(issuedLicense);
                }

                await _context.SaveChangesAsync();
                
                var properties = new Properties();
                properties.set("Id", issuedLicense.Id);
                properties.set("Issuer", "licensing.mcnative.org");
                properties.set("CheckoutTime", now.Ticks);
                if (license.OrganisationId != null)
                {
                    properties.set("OrganisationId", license.OrganisationId);
                    properties.set("OrganisationName", license.Organisation.Name);
                }
                properties.set("DeviceId", deviceId);
                properties.set("ResourceId", resourceId);
                properties.set("Expiry", expiry.Ticks);
                properties.set("PreferredRefreshTime", preferredRefreshTime.Ticks);
                if(license.Comment != null) properties.set("Comment", license.Comment);

                var bytes = Encoding.UTF8.GetBytes(properties.ToString());
                var base64 = Convert.ToBase64String(bytes);
                RSACryptoServiceProvider rsa = ImportPrivateKey(resource.PrivateKey);
                var signedBase64 = SignData(bytes, rsa.ExportParameters(true));
                
                return Ok(base64 + ";" + Convert.ToBase64String(signedBase64));
            }
            return BadRequest("Resource not licensed");
        }

        [HttpGet("{resourceId}/alive")]
        public async Task<IActionResult> Alive(string resourceId, [FromHeader] string serverId, [FromHeader] string serverSecret)
        {
            if (serverId == null || serverSecret == null)
            {
                return Unauthorized("Server id or secret is missing");
            }

            Resource resource = _context.Resources.SingleOrDefault(resource => resource.Id.Equals(resourceId));
            if (resource == null) return NotFound();

            if (resource.AliveReportingEnabled)
            {
                Server server = _context.Servers.SingleOrDefault(server => server.Id == serverId);
                if (server == null || !server.Secret.Equals(serverSecret)) return Unauthorized("Invalid server id or secret");

                int hour = DateTime.Now.Hour;
                AliveReport report = await _context.AliveReports.FirstOrDefaultAsync(r => r.OrganisationId == server.OrganisationId 
                        && r.ResourceId == resourceId && r.FirstContact.Date >= DateTime.Now.Date && r.Hour == hour);
                //@Todo check license
                if (report == null)
                {
                    report = new AliveReport();
                    report.OrganisationId = server.OrganisationId;
                    report.ResourceId = resource.Id;
                    report.FirstContact = DateTime.Now;
                    report.LastContact = report.FirstContact;
                    report.Hour = hour;
                    report.Count = 1;
                    await _context.AddAsync(report);
                }
                else
                {
                    if ((DateTime.Now-report.LastContact).Minutes < 8)
                    {
                        return Ok();
                    }
                    report.LastContact = DateTime.Now;
                    report.Count += 1;
                    _context.Update(report);
                }

                await _context.SaveChangesAsync();
                return Ok();
            }

            return BadRequest("Resource not reportable");
        }

        public static RSACryptoServiceProvider ImportPrivateKey(string pem) {
            PemReader pr = new PemReader(new StringReader(pem));
            RsaPrivateCrtKeyParameters KeyPair = (RsaPrivateCrtKeyParameters)pr.ReadObject();
            RSAParameters rsaParams = DotNetUtilities.ToRSAParameters(KeyPair);

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider();
            csp.ImportParameters(rsaParams);
            return csp;
        }
        
        public static byte[] SignData(byte[] data, RSAParameters privateKey)
        {

            byte[] signedBytes;

            using var rsa = new RSACryptoServiceProvider();
            // Write the message to a byte array using ASCII as the encoding.
            try
            {
                // Import the private key used for signing the message
                rsa.ImportParameters(privateKey);

                // Sign the data, using SHA512 as the hashing algorithm 
                signedBytes = rsa.SignData(data, CryptoConfig.MapNameToOID("SHA512"));
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return null;
            }
            finally
            {
                // Set the keycontainer to be cleared when rsa is garbage collected.
                rsa.PersistKeyInCsp = false;
            }

            // Convert the byte array back to a string message
            return signedBytes;
        }
    }
}