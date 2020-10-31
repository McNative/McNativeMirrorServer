using System;
using System.ComponentModel.DataAnnotations;

namespace McNativeMirrorServer.Model
{
    public class AliveReport
    {
        [Key]
        public int Id { get; set; }

        public string OrganisationId { get; set; }

        public string ResourceId { get; set; }

        public int Hour { get; set; }

        public DateTime FirstContact { get; set; }

        public DateTime LastContact { get; set; }

        public int Count { get; set; }
    }
}
