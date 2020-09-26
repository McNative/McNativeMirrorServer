using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using McNativeMirrorServer;
using McNativeMirrorServer.Model;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace MirrorServer.Controllers
{
    [Route("[controller]/")]
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
            Console.WriteLine(str);
            /*
             * Credentials überprüfen -
             *
             * Lizenz prüfen (Hat er überhaupt eine / Expiry / Disabled etc.) -
             *
             * Lizenz austellen
             * Insert Database (+ Request Informationen)
             * 
             * License File bauen
             *
             * License FIle signieren + Siginatur hinzufügen (private key mit base64)
             */
            Resource resource = _context.Resources.SingleOrDefault(resource => resource.PublicId.Equals(resourceId));
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
                var expiry = now.AddDays(30);
                var preferedRefreshTime = now.AddDays(3);

                var activeLicense = new LicenseActive
                {
                    LicenseId = license.Id,
                    DeviceId = deviceId,
                    RequestAddress = HttpContext.Connection.RemoteIpAddress.ToString(),
                    Expiry = expiry,
                    CheckoutTime = now
                };
                await _context.AddAsync(activeLicense);
                await _context.SaveChangesAsync();
                
                var properties = new Properties();
                properties.set("Issuer", "licensing.mcnative.org");
                properties.set("CheckoutTime", now.ToString(CultureInfo.InvariantCulture));
                properties.set("OrganisationId", organisation.Id);
                properties.set("DeviceId", deviceId);
                properties.set("ResourceId", resourceId);
                properties.set("Expiry", expiry.ToString(CultureInfo.InvariantCulture));
                properties.set("PreferedRefreshTime", preferedRefreshTime.ToString(CultureInfo.InvariantCulture));

                var bytes = Encoding.UTF8.GetBytes(properties.ToString());
                
                var base64 = Convert.ToBase64String(bytes);

                var privateKey = resource.PrivateKey;
                
                RSACryptoServiceProvider rsa = ImportPrivateKey(resource.PrivateKey);
                
                

                var signedBase64 = SignData(base64, rsa.ExportParameters(true));

                return Ok(base64 + ";" + signedBase64);
            }
            return BadRequest("Resource not licensed");
        }
        
        public static RSACryptoServiceProvider ImportPrivateKey(string pem) {
            PemReader pr = new PemReader(new StringReader(pem));
            AsymmetricCipherKeyPair KeyPair = (AsymmetricCipherKeyPair)pr.ReadObject();
            RSAParameters rsaParams = DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters)KeyPair.Private);

            RSACryptoServiceProvider csp = new RSACryptoServiceProvider();// cspParams);
            csp.ImportParameters(rsaParams);
            return csp;
        }
        
        public static string SignData(string message, RSAParameters privateKey)
        {
            ASCIIEncoding byteConverter = new ASCIIEncoding();

            byte[] signedBytes;

            using (var rsa = new RSACryptoServiceProvider())
            {
                // Write the message to a byte array using ASCII as the encoding.
                byte[] originalData = byteConverter.GetBytes(message);                

                try
                {
                    // Import the private key used for signing the message
                    rsa.ImportParameters(privateKey);

                    // Sign the data, using SHA512 as the hashing algorithm 
                    signedBytes = rsa.SignData(originalData, CryptoConfig.MapNameToOID("SHA512"));
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
            return byteConverter.GetString(signedBytes);
        }
    }
}