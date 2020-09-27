using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace McNativeMirrorServer
{
    public class Properties
    {
        
        private Dictionary<string, string> list = new Dictionary<string, string>();

        public string get(string field, string defValue)
        {
            return (get(field) == null) ? (defValue) : (get(field));
        }
        public string get(string field)
        {
            return (list.ContainsKey(field))?(list[field]):(null);
        }

        public void set(string field, Object value)
        {
            if (!list.ContainsKey(field))
                list.Add(field, value.ToString());
            else
                list[field] = value.ToString();
        }

        public void Save(string filename)
        {
            if (!System.IO.File.Exists(filename))
                System.IO.File.Create(filename);

            System.IO.StreamWriter file = new System.IO.StreamWriter(filename);

            file.WriteLine(ToString());

            file.Close();
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (String key in list.Keys.ToArray())
            {
                string value = list[key];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    builder.Append(key).Append("=").Append(value).Append("\n");
                }
            }

            return builder.ToString();
        }
    }
}