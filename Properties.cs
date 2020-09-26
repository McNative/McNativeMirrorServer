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
                    builder.Append(key + "=" + value);
            }

            return builder.ToString();
        }

        public void load(string file)
        {
            foreach (string line in System.IO.File.ReadAllLines(file))
            {
                if ((!string.IsNullOrEmpty(line)) &&
                    (!line.StartsWith(";")) &&
                    (!line.StartsWith("#")) &&
                    (!line.StartsWith("'")) &&
                    (line.Contains('=')))
                {
                    int index = line.IndexOf('=');
                    string key = line.Substring(0, index).Trim();
                    string value = line.Substring(index + 1).Trim();

                    if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                        (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    try
                    {
                        //ignore dublicates
                        list.Add(key, value);
                    }
                    catch { }
                }
            }
        }
    }
}