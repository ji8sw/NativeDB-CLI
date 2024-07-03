using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

class Program
{
    public struct Param
    {
        public string Type;
        public string Name;
    };

    public struct Native
    {
        public string Hash;

        public string Name;
        public string JHash;
        public string Comment;
        public List<Param> Params;
        public string ReturnType;
        public string FirstSeen;
    };

    public struct Namespace
    {
        public string Name;
        public List<Native> Natives;
    };

    public static List<Namespace> Namespaces = new List<Namespace>();

    public static Namespace GetNamespaceByName(string Name)
    {
        foreach (Namespace Space in Namespaces)
            if (Space.Name.ToLower() == Name.ToLower()) return Space;

        Namespace InvalidNamespace = new Namespace();
        InvalidNamespace.Name = "Invalid";
        return InvalidNamespace;
    }

    public static Native GetNativeByNameFromNamespace(string Name, Namespace Space)
    {
        foreach (Native Ntv in Space.Natives)
            if (Ntv.Name.ToLower() == Name.ToLower()) return Ntv;

        Native InvalidNative = new Native();
        InvalidNative.Name = "Invalid";
        return InvalidNative;
    }


    async static Task Main()
    {
        string Natives = string.Empty;
        Console.BufferHeight = 10000;

        using (HttpClient client = new HttpClient())
        {
            try
            {
                Console.WriteLine("Downloading natives...\n");
                Natives = await client.GetStringAsync("https://raw.githubusercontent.com/alloc8or/gta5-nativedb-data/master/natives.json");
            }
            catch (HttpRequestException Error)
            {
                Console.WriteLine($"Failed to download natives.json: {Error.Message}");
                return;
            }
        }

        using (JsonDocument document = JsonDocument.Parse(Natives))
        {
            JsonElement Root = document.RootElement;
            
            if (Root.ValueKind == JsonValueKind.Object)
            {

                foreach (JsonProperty Namespace in Root.EnumerateObject())
                {
                    Namespace NewNamespace = new Namespace();
                    NewNamespace.Natives = new List<Native>();
                    NewNamespace.Name = Namespace.Name;
                    foreach (JsonProperty Native in Namespace.Value.EnumerateObject())
                    {
                        Native NewNative = new Native();
                        NewNative.Hash = Native.Name;
                        foreach (JsonProperty NativeData in Native.Value.EnumerateObject())
                        {
                            switch (NativeData.Name)
                            {
                                case "name":
                                    NewNative.Name = NativeData.Value.ToString();
                                    break;
                                case "jhash":
                                    NewNative.JHash = NativeData.Value.ToString();
                                    break;
                                case "comment":
                                    NewNative.Comment = NativeData.Value.ToString();
                                    break;
                                case "params":
                                    NewNative.Params = new List<Param>();
                                    foreach (JsonElement Param in NativeData.Value.EnumerateArray())
                                    {
                                        Param Parameter = new Param();
                                        foreach (JsonProperty ParamElement in Param.EnumerateObject())
                                        {
                                            if (ParamElement.Name == "type")
                                                Parameter.Type = ParamElement.Value.ToString();
                                            else if (ParamElement.Name == "name")
                                                Parameter.Name = ParamElement.Value.ToString();
                                        }
                                        NewNative.Params.Add(Parameter);
                                    }
                                    break;
                                case "return_type":
                                    NewNative.ReturnType = NativeData.Value.ToString();
                                    break;
                                case "build":
                                    NewNative.FirstSeen = NativeData.Value.ToString();
                                    break;
                            };

                        }
                        NewNamespace.Natives.Add(NewNative);
                    }
                    Namespaces.Add(NewNamespace);
                }

                int NativeCount = 0;
                foreach (Namespace Space in Namespaces)
                    foreach (Native Ntv in Space.Natives)
                        NativeCount++;
                Console.WriteLine("Gathered " + NativeCount + " natives across " + Namespaces.Count + " namespaces.");

                Namespace CurrentNamespace = Namespaces.FirstOrDefault();
                Native CurrentNative = CurrentNamespace.Natives[0];
                Console.WriteLine("Type 'help' for a list of commands.");
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("Command: ");
                    string RawCommand = Console.ReadLine();
                    Console.WriteLine();

                    if (string.IsNullOrEmpty(RawCommand)) continue;
                    string[] CommandParameters = RawCommand.Split(' ');
                    string Command = CommandParameters[0];
                    switch (Command.ToLower())
                    {
                        case "setnamespace":
                            Namespace NewCurrentNamespace = GetNamespaceByName(CommandParameters[1]);
                            if (NewCurrentNamespace.Name != "Invalid")
                                CurrentNamespace = NewCurrentNamespace;
                            break;
                        case "setnative":
                            Native NewCurrentNative = GetNativeByNameFromNamespace(CommandParameters[1], CurrentNamespace);
                            if (NewCurrentNative.Name != "Invalid")
                                CurrentNative = NewCurrentNative;
                            break;
                        case "getallnamespaces":
                            foreach (Namespace Space in Namespaces)
                                Console.WriteLine(Space.Name + ": " + Space.Natives.Count + " natives.");
                            break;
                        case "getallnatives":
                            foreach (Native Ntv in CurrentNamespace.Natives)
                            {
                                string ParamList = "";
                                for (int ParamIndex = 0; ParamIndex < Ntv.Params.Count; ParamIndex++)
                                {
                                    Param Parameter = Ntv.Params[ParamIndex];
                                    ParamList = ParamList + Parameter.Type + " " + Parameter.Name;
                                    if (ParamIndex != Ntv.Params.Count - 1)
                                        ParamList = ParamList + ", ";
                                }
                                Console.WriteLine(Ntv.ReturnType + " " + Ntv.Name + "(" + ParamList + ")");
                            }
                            break;
                        case "nativeinfo":
                            string ParamList2 = ""; // cant create 2 variables with same name in switch because its in the same scope?, (bullpoo)
                            for (int ParamIndex = 0; ParamIndex < CurrentNative.Params.Count; ParamIndex++)
                            {
                                Param Parameter = CurrentNative.Params[ParamIndex];
                                ParamList2 = ParamList2 + Parameter.Type + " " + Parameter.Name;
                                if (ParamIndex != CurrentNative.Params.Count - 1)
                                    ParamList2 = ParamList2 + ", ";
                            }
                            Console.WriteLine(CurrentNative.ReturnType + " " + CurrentNamespace.Name + "::" + CurrentNative.Name + "(" + ParamList2 + ")");
                            Console.WriteLine("Invokable Hash: " + CurrentNative.Hash);
                            Console.WriteLine("Jenkins Hash: " + CurrentNative.JHash);
                            Console.WriteLine("Introduced In: " + CurrentNative.FirstSeen);
                            Console.WriteLine("Comment: " + CurrentNative.Comment);

                            break;
                        case "getallnativesever":
                            foreach (Namespace Space in Namespaces)
                            {
                                foreach (Native Ntv in Space.Natives)
                                {
                                    string ParamList = "";
                                    for (int ParamIndex = 0; ParamIndex < Ntv.Params.Count; ParamIndex++)
                                    {
                                        Param Parameter = Ntv.Params[ParamIndex];
                                        ParamList = ParamList + Parameter.Type + " " + Parameter.Name;
                                        if (ParamIndex != Ntv.Params.Count - 1)
                                            ParamList = ParamList + ", ";
                                    }
                                    Console.WriteLine(Ntv.ReturnType + " " + Space.Name + "::" + Ntv.Name + "(" + ParamList + ")");
                                }
                            }
                            break;
                        case "quit":
                        case "exit":
                            Environment.Exit(0);
                            break;
                        case "help":
                        default:
                            Console.WriteLine("setnamespace [namespacename]: Sets the current namespace using its name");
                            Console.WriteLine("setnative [nativename]: Sets the current native using its name, must be apart of the current namespace");
                            Console.WriteLine("getallnamespaces: Lists all namespaces");
                            Console.WriteLine("getallnatives: Lists all natives in the current namespace");
                            Console.WriteLine("getallnativesever: Lists all natives from every namespace");
                            Console.WriteLine("nativeinfo: Lists all info on the current native");
                            break;
                    }
                }
            }
        }
    }
}
