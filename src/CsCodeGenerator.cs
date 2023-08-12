using System.Collections;
using System.ComponentModel.Design;
using System.IO;
using System.Resources;

namespace resx
{
    static class CsCodeGenerator
    {
        public static void Generate(string path, string np, string csFile)
        {
            FileStream stream = new FileStream(path, FileMode.Open);
            ResXResourceReader rrr = new ResXResourceReader(stream);
            // Read data to dictionary
            rrr.BasePath = Path.GetDirectoryName(path);
            rrr.UseResXDataNodes = true;
            rrr.GetEnumerator();
            rrr.Close();
            stream.Close();
            
            string refPath = Path.GetDirectoryName(path);
            
            string name = Path.GetFileName(path);
            string file = name.Remove(name.Length - Path.GetExtension(path).Length);
            name = Path.GetFileName(csFile);
            string cs = name.Remove(name.Length - Path.GetExtension(csFile).Length);
            
            StreamWriter sw = new StreamWriter(csFile);
            WriteHead(sw, np, file, cs);
            
            foreach (DictionaryEntry entry in rrr)
            {
                WriteResource(sw, (ResXDataNode)entry.Value);
            }
            
            WriteFoot(sw);
            sw.Close();
        }
        
        private static void WriteHead(StreamWriter sw, string np, string name, string c)
        {
            sw.Write(@$"namespace {np}
{{
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    public class {c}
    {{
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        internal {c}()
        {{
            
        }}
        
        public static global::System.Resources.ResourceManager ResourceManager {{
            get {{
                if (object.ReferenceEquals(resourceMan, null)) {{
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager(""{np}.{name}"", typeof({c}).Assembly);
                    resourceMan = temp;
                }}
                return resourceMan;
            }}
        }}
        
        public static global::System.Globalization.CultureInfo Culture {{
            get => resourceCulture;
            set => resourceCulture = value;
        }}");
        }
        
        private static void WriteResource(StreamWriter sw, ResXDataNode rdn)
        {
            DataNodeInfo dni = rdn._nodeInfo;
            string tn = rdn.GetValueTypeName((ITypeResolutionService)null);
            
            int i;
            if ((i = tn.IndexOf(',')) != -1)
            {
                tn = tn.Remove(i);
            }
            
            sw.Write(@$"
        
        public static {tn} {dni.Name} => ({tn})ResourceManager.GetObject(""{dni.Name}"", resourceCulture);");
        }
        
        private static void WriteFoot(StreamWriter sw)
        {
            sw.Write(@"
    }
}");
        }
    }
}