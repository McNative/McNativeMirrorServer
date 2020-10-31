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
        
        [HttpGet("{resourceId}/checkout")]//licensing.mcnative.org/<id>/checkout
        public async Task<IActionResult> Checkout(string resourceId, [FromHeader] string deviceId,
            [FromHeader] string serverId, [FromHeader] string serverSecret
            ,[FromHeader] string rolloutId, [FromHeader] string rolloutSecret)//ResourceId DeviceId // ServerId/Secret OR RolloutId/Secret
        {
            RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            string str = RSA.ToXmlString(true);
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

                var now = DateTime.Now;

                var expiry = now.AddDays(14);
                if (license.Expiry != null && expiry > license.Expiry) expiry = license.Expiry.Value;

                var preferredRefreshTime = now.AddDays(3);

                LicenseActive activeLicense = await _context.LicenceActives.Where(a => a.DeviceId == deviceId).FirstOrDefaultAsync();
                if (activeLicense == null)
                {
                    activeLicense = new LicenseActive
                    {
                        LicenseId = license.Id,
                        DeviceId = deviceId,
                        RequestAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                        Expiry = expiry,
                        CheckoutTime = now
                    };
                    await _context.AddAsync(activeLicense);
                }
                else
                {
                    activeLicense.RequestAddress = HttpContext.Connection.RemoteIpAddress.ToString();
                    activeLicense.CheckoutTime = now;
                    activeLicense.Expiry = expiry;
                    _context.Update(activeLicense);
                }

                await _context.SaveChangesAsync();
                
                var properties = new Properties();
                properties.set("Id",activeLicense.Id);
                properties.set("Issuer", "licensing.mcnative.org");
                properties.set("CheckoutTime", now.Ticks);
                properties.set("OrganisationId", organisation.Id);
                properties.set("OrganisationName", organisation.Name);
                properties.set("DeviceId", deviceId);
                properties.set("ResourceId", resourceId);
                properties.set("Expiry", expiry.Ticks);
                properties.set("PreferredRefreshTime", preferredRefreshTime.Ticks);

                var bytes = Encoding.UTF8.GetBytes(properties.ToString());
                var base64 = Convert.ToBase64String(bytes);
                RSACryptoServiceProvider rsa = ImportPrivateKey(resource.PrivateKey);
                var signedBase64 = SignData(bytes, rsa.ExportParameters(true));
                
                return Ok(base64 + ";" + Convert.ToBase64String(signedBase64));
            }
            return BadRequest("Resource not licensed");
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

            using (var rsa = new RSACryptoServiceProvider())
            {
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
            }
            // Convert the byte array back to a string message
            return signedBytes;
        }
    }
}