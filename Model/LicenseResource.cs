using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class LicenseResource
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        public string ResourceId { get; set; }

        public string LicenseId { get; set; }

        [ForeignKey("LicenseId")]
        public virtual Resource License { get; set; }

        [ForeignKey("ResourceId")]
        public virtual Resource Resource { get; set; }

    }
}
