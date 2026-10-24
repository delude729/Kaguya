using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace KaguyaNative {
    internal static class EmbeddedAssets {
        const string Prefix="KaguyaEmbedded/";
        static readonly Assembly assembly=Assembly.GetExecutingAssembly();
        static readonly HashSet<string> names=new HashSet<string>(assembly.GetManifestResourceNames(),StringComparer.Ordinal);
        static string Relative(string path) {
            string relative=path;
            if(Path.IsPathRooted(path)) {
                string root=Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
                string absolute=Path.GetFullPath(path);
                if(!absolute.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Asset path is outside the application directory");
                relative=absolute.Substring(root.Length);
            }
            return relative.Replace('\\','/');
        }
        internal static Stream Open(string path) {
            Stream stream=assembly.GetManifestResourceStream(Prefix+Relative(path));
            if(stream==null)throw new FileNotFoundException("Missing embedded Kaguya asset",path);
            return stream;
        }
        internal static string ReadAllText(string path) {
            using(Stream stream=Open(path))using(StreamReader reader=new StreamReader(stream))return reader.ReadToEnd();
        }
        internal static bool Exists(string path) {return names.Contains(Prefix+Relative(path));}
        internal static string[] UiDirectories(string path) {
            string prefix=Prefix+Relative(path).TrimEnd('/')+"/";
            HashSet<string> folders=new HashSet<string>(StringComparer.Ordinal);
            foreach(string name in names) {
                if(!name.StartsWith(prefix,StringComparison.Ordinal))continue;
                string remainder=name.Substring(prefix.Length);
                int slash=remainder.IndexOf('/');
                if(slash>0&&remainder.StartsWith("dpi-",StringComparison.Ordinal))folders.Add(Path.Combine(path,remainder.Substring(0,slash)));
            }
            string[] result=new string[folders.Count];folders.CopyTo(result);Array.Sort(result,StringComparer.Ordinal);return result;
        }
    }
}
