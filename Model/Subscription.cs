using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace McNativeMirrorServer.Model
{
    public class Subscription
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}
