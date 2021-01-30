using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class LicenseIssued
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        
        public string ActiveLicenseId { get; set; }

        public string DeviceId { get; set; }
        
        public string RequestAddress { get; set; }
        
        public virtual DateTime Expiry { get; set; }
        
        public virtual DateTime CheckoutTime { get; set; }

        [ForeignKey("ActiveLicenseId")]
        public virtual LicenseActive ActiveLicense { get; set; }
    }
}