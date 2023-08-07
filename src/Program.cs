using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Resources;

namespace resx
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "--help")
            {
                // HELP
                return;
            }
            
            if (args.Length < 2)
            {
                Console.WriteLine("Invalid arguments.");
                return;
            }
            
            string file = args[1];
            
            if (args[0] == "new")
            {
                CreateResx(file);
                return;
            }
            
            if (args[0] == "add")
            {
                AddToResx(file);
                return;
            }
            
            Console.WriteLine($"Invalid argument {args[0]}.");
        }
        
        private static void CreateResx(string path, ListDictionary starting = null)
        {
            ResXResourceWriter rrw = new ResXResourceWriter(new FileStream(path, FileMode.Create));
            
            if (starting != null)
            {
                AddStarting(rrw, starting);
            }
            
            string fullPath = Path.GetFullPath(path);
            
            while (true)
            {
                string type;
                while (true)
                {
                    Console.Write("Resource type (str/txt/bin): ");
                    ConsoleKeyInfo cki = Console.ReadKey();
                    // Exit
                    if (cki.Key == ConsoleKey.Q &&
                        cki.Modifiers == ConsoleModifiers.Control)
                    {
                        rrw.Close();
                        ExitInfo(path);
                        return;
                    }
                    
                    type = cki.KeyChar + Console.ReadLine().ToLower().Trim();
                    if (type != "str" && type != "txt" && type != "bin")
                    {
                        Console.WriteLine($"Invalid type.");
                        continue;
                    }
                    break;
                }
                
                Console.Write("Resource name: ");
                string name = Console.ReadLine();
                if (type == "str")
                {
                    Console.Write("Resource value: ");
                    string value = Console.ReadLine();
                    
                    rrw.AddResource(name, value);
                    continue;
                }
                
                while (true)
                {
                    Console.Write("Resource path: ");                
                    string rPath = Console.ReadLine();
                    
                    if (!File.Exists(rPath))
                    {
                        Console.WriteLine("File does not exist.");
                        continue;
                    }
                    
                    rrw.AddResource(name,
                        new ResXDataNode(name,
                            new ResXFileRef
                            (
                                Path.GetRelativePath(Path.GetFullPath(rPath), fullPath),
                                type == "txt" ? typeof(string).FullName : typeof(byte[]).FullName
                            )
                        )
                    );
                    break;
                }
            }
        }
        
        private static void AddStarting(ResXResourceWriter rrw, ListDictionary starting)
        {
            foreach (DictionaryEntry entry in starting)
            {
                rrw.AddResource((string)entry.Key, entry.Value);
            }
        }
        
        private static void AddToResx(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"File {path} does not exist.");
                return;
            }
            
            FileStream stream = new FileStream(path, FileMode.Open);
            ResXResourceReader rrr = new ResXResourceReader(stream);
            // Read data to dictionary
            rrr.GetEnumerator();
            rrr.Close();
            stream.Close();
            
            CreateResx(path, rrr._resData);
        }
        
        private static void ExitInfo(string path)
        {
            string name = Path.GetFileName(path);
            string file = name.Remove(name.Length - Path.GetExtension(path).Length);
            string dir = Path.GetRelativePath(Path.GetFullPath(path), Directory.GetCurrentDirectory());
            
            Console.WriteLine(@$"
<ItemGroup>
  <Compile Update=""{dir}/{file}.Designer.cs"">
    <DesignTime>True</DesignTime>
    <AutoGen>True</AutoGen>
    <DependentUpon>{name}</DependentUpon>
  </Compile>
</ItemGroup>

<ItemGroup>
  <EmbeddedResource Update=""{dir}/{name}"">
    <Generator>PublicResXFileCodeGenerator</Generator>
    <LastGenOutput>{file}.Designer.cs</LastGenOutput>
  </EmbeddedResource>
</ItemGroup>");
        }
    }
}
