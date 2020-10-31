﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace McNativeMirrorServer.Model
{
    public class Resource
    {

        public string Id { get; set; }
        public string Name { get; set; }

        public string PrivateKey { get; set; }
        public string DefaultDownloadEdition { get; set; }

        public bool Licensed { get; set; }

        public bool AliveReportingEnabled { get; set; }

        public string DeploySecret { get; set; }

        [InverseProperty("Resource")]
        public virtual ICollection<ResourceVersion> Versions { get; set; }

        [InverseProperty("Resource")]
        public virtual ICollection<ResourceEdition> Editions { get; set; }
    }
}
