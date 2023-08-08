using System;
using System.Linq;
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
            
            if (!File.Exists(file))
            {
                Console.WriteLine($"File {file} does not exist.");
                return;
            }
            
            file = Path.GetFullPath(file);
            
            if (args[0] == "new")
            {
                if (args.Length > 2)
                {
                    Console.WriteLine($"Unnknown argument {args[2]}.");
                }
                
                CreateResx(file);
                return;
            }
            
            if (args[0] == "add")
            {
                if (args.Length > 2)
                {
                    Console.WriteLine($"Unnknown argument {args[2]}.");
                }
                
                AddToResx(file);
                return;
            }
            
            if (args[0] == "rm")
            {
                string[] names = new string[args.Length - 2];
                Array.Copy(args, 2, names, 0, names.Length);
                RemoveResx(file, names);
                return;
            }
            
            if (args[0] == "ls")
            {
                bool valid = LsArguments(args, out (bool, bool, bool) opts);
                
                if (!valid) { return; }
                
                if (opts.Item1 == opts.Item2)
                {
                    opts.Item1 = true;
                    opts.Item2 = true;
                }
                
                ListResx(file, opts.Item1, opts.Item2, opts.Item3);
                return;
            }
            
            if (args[0] == "include")
            {
                if (args.Length != 3)
                {
                    Console.WriteLine("Invalid arguments.");
                    return;
                }
                
                Include(file, args[2]);
                return;
            }
            
            Console.WriteLine($"Invalid argument {args[0]}.");
        }
        
        private static bool LsArguments(string[] args, out (bool, bool, bool) opts)
        {
            opts = (false, false, false);
            
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "-n" || args[i] == "--names")
                {
                    opts.Item1 = true;
                    continue;
                }
                if (args[i] == "-v" || args[i] == "--values")
                {
                    opts.Item2 = true;
                    continue;
                }
                if (args[i] == "-o" || args[i] == "--open-files")
                {
                    opts.Item3 = true;
                    continue;
                }
                
                Console.WriteLine($"Unknown argument {args[i]}.");
                return false;
            }
            
            return true;
        }
        
        private static void CreateResx(string path, ListDictionary starting = null)
        {
            ResXResourceWriter rrw = new ResXResourceWriter(new FileStream(path, FileMode.Create));
            
            string fullPath = Path.GetDirectoryName(path);
            
            if (starting != null)
            {
                AddStarting(fullPath, rrw, starting);
            }
            
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
                        //ExitInfo(path);
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
                    ConsoleKeyInfo cki = Console.ReadKey();
                    // Exit
                    if (cki.Key == ConsoleKey.Q &&
                        cki.Modifiers == ConsoleModifiers.Control)
                    {
                        rrw.Close();
                        //ExitInfo(path);
                        return;
                    }
                    string value = cki.KeyChar + Console.ReadLine();
                    
                    rrw.AddResource(name, value);
                    continue;
                }
                
                while (true)
                {
                    Console.Write("Resource path: ");
                    ConsoleKeyInfo cki = Console.ReadKey();
                    // Exit
                    if (cki.Key == ConsoleKey.Q &&
                        cki.Modifiers == ConsoleModifiers.Control)
                    {
                        rrw.Close();
                        //ExitInfo(path);
                        return;
                    }
                    string rPath = cki.KeyChar + Console.ReadLine();
                    
                    if (!File.Exists(rPath))
                    {
                        Console.WriteLine("File does not exist.");
                        continue;
                    }
                    
                    Console.WriteLine(fullPath);
                    Console.WriteLine(Path.GetFullPath(rPath));
                    
                    rrw.AddResource(name,
                        new ResXDataNode(name,
                            new ResXFileRef
                            (
                                Path.GetRelativePath(fullPath, Path.GetFullPath(rPath)),
                                type == "txt" ? typeof(string).FullName : typeof(byte[]).FullName
                            )
                        )
                    );
                    break;
                }
            }
        }
        
        private static void AddStarting(string refPath, ResXResourceWriter rrw, ListDictionary starting)
        {
            foreach (DictionaryEntry entry in starting)
            {
                ResXDataNode rdn = (ResXDataNode)entry.Value;
                
                if (rdn.FileRef != null)
                {
                    ResXFileRef rfr = rdn.FileRef;
                    
                    rdn = new ResXDataNode(
                        rdn.Name,
                        new ResXFileRef(
                            Path.GetRelativePath(refPath, rfr.FileName),
                            rfr.TypeName,
                            rfr.TextFileEncoding
                        )
                    );
                }
                
                rrw.AddResource(rdn);
            }
        }
        
        private static void AddToResx(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open);
            ResXResourceReader rrr = new ResXResourceReader(stream);
            // Read data to dictionary
            rrr.BasePath = Path.GetDirectoryName(path);
            rrr.UseResXDataNodes = true;
            rrr.GetEnumerator();
            rrr.Close();
            stream.Close();
            
            CreateResx(path, rrr._resData);
        }
        
        private static void RemoveResx(string path, string[] names)
        {
            if (names.Length == 0)
            {
                Console.WriteLine("No remove resources were provided");
                return;
            }
            
            FileStream stream = new FileStream(path, FileMode.Open);
            ResXResourceReader rrr = new ResXResourceReader(stream);
            // Read data to dictionary
            rrr.BasePath = Path.GetDirectoryName(path);
            rrr.UseResXDataNodes = true;
            rrr.GetEnumerator();
            rrr.Close();
            stream.Close();
            
            ListDictionary values = rrr._resData;
            
            foreach (string key in names)
            {
                if (!values.Contains(key))
                {
                    Console.WriteLine($"No resource named {key} is contained in {path}");
                    continue;
                }
                
                values.Remove(key);
                Console.WriteLine($"Successfully removed {key}.");
            }
            
            ResXResourceWriter rrw = new ResXResourceWriter(new FileStream(path, FileMode.Create));
            
            string fullPath = Path.GetDirectoryName(path);
            AddStarting(fullPath, rrw, values);
            
            rrw.Close();
        }
        
        private static void ListResx(string path, bool keys, bool values, bool open)
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
            
            foreach (DictionaryEntry entry in rrr)
            {
                WriteResXNode(refPath, (ResXDataNode)entry.Value, keys, values, open);
            }
            
            Console.ForegroundColor = ConsoleColor.White;
        }
        
        private static void WriteResXNode(string refPath, ResXDataNode rdn, bool keys, bool values, bool open)
        {
            if (!keys && !values)
            {
                throw new Exception("Big problem!");
            }
            
            if (!values)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(rdn.Name);
                return;
            }
            
            object value = rdn.GetValue((System.ComponentModel.Design.ITypeResolutionService)null);
            
            if (!open && rdn.FileRef != null)
            {
                value = Path.GetRelativePath(refPath, rdn.FileRef.FileName);
            }
            
            if (!keys)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(value);
                return;
            }
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(rdn.Name);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($": {value}");
        }
        
        private static void Include(string path, string csproj)
        {
            if (!File.Exists(csproj))
            {
                Console.WriteLine($"File {csproj} does not exist.");
                return;
            }
            
            string project = File.ReadAllText(csproj);
            
            int end = project.IndexOf("</Project>");
            
            if (end < 1)
            {
                Console.WriteLine($"File {csproj} is not a valid csproj file.");
                return;
            }
            
            string name = Path.GetFileName(path);
            string file = name.Remove(name.Length - Path.GetExtension(path).Length);
            string dir = Path.GetRelativePath(Path.GetDirectoryName(path), Directory.GetCurrentDirectory());
            
            if (project.Contains($"<Compile Update=\"{dir}\\{file}.Designer.cs\">") ||
                project.Contains($"<EmbeddedResource Update=\"{dir}\\{name}\">"))
            {
                Console.WriteLine($"{csproj} already includes {path}");
                return;
            }
            
            project = project.Insert(end - 1, @$"  <ItemGroup>
    <Compile Update=""{dir}\{file}.Designer.cs"">
      <DesignTime>True</DesignTime>
      <AutoGen>True</AutoGen>
      <DependentUpon>{name}</DependentUpon>
    </Compile>
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Update=""{dir}\{name}"">
      <Generator>PublicResXFileCodeGenerator</Generator>
      <LastGenOutput>{file}.Designer.cs</LastGenOutput>
    </EmbeddedResource>
  </ItemGroup>
  ");
            
            File.WriteAllText(csproj, project);
        }
    }
}
