﻿using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace McNativeMirrorServer.Model
{
    public class RolloutProfile
    {

        [Key]
        public string Id { get; set; }

        [JsonIgnore]
        public string OrganisationId { get; set; }
        public string Name { get; set; }

        public string Configuration { get; set; }

        public string ProfileDefinition { get; set; }

    }
}
