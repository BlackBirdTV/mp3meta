using System.Text;
using mp3meta.Classes;
using System.Reflection;
using System.Runtime.InteropServices;

namespace mp3meta;

class Cli {

    public static void Main(string[] args) {
        if (args.Length < 2) {
            System.Console.WriteLine($"Expected 2 args but received {args.Length}");
            return;
        }
        
        byte[] file = File.ReadAllBytes(args[0]);
        var metadata = new Metadata();
        List<byte> musicData;
        if (Encoding.UTF8.GetString(file[..3]) == "ID3") {
            var majorVersion = file[4];
            var minorVersion = file[5];
            var flags = file[6];
            var lengthBytes = file[6..10];
            var length = lengthBytes[0] << 21 | lengthBytes[1] << 14 | lengthBytes[2] << 7 | lengthBytes[3];
            var metadataBytes = file[10..(length+10)];
            metadata = Metadata.Parse(metadataBytes);
            musicData = file[(length-1)..].ToList();
        }
        else {
            musicData = file.ToList();
        }

        switch (args[1]) {
            case "get":
                if (args.Length < 3) {
                    System.Console.WriteLine($"Expected 3 args but received {args.Length}");
                    return;
                }
                if (!metadata.frames.ContainsKey(args[2])) {
                    System.Console.WriteLine($"Metadata frame \"{args[2]}\" was not set.");
                    return;
                }
                foreach (var v in metadata.frames[args[2]]) {
                    System.Console.WriteLine(Encoding.UTF8.GetString(v));
                }
                break;
            case "set":
                if (args.Length < 4) {
                    System.Console.WriteLine($"Expected 4 args but received {args.Length}");
                    return;
                }

                System.Console.WriteLine(musicData.Count);

                metadata.frames[args[2]] = new() { Encoding.UTF8.GetBytes(args[3]) };
                metadata.frames["APIC"] = new() {};
                musicData.Reverse();
                musicData.AddRange(metadata.ToBytes().Reverse());
                musicData.Reverse();
                
                foreach (byte b in musicData.ToArray()[..100]) {
                    System.Console.Write(((char)b));
                }
                System.Console.WriteLine();

                File.WriteAllBytes(args[0], musicData.ToArray());
                break;
            case "add":
                if (args.Length < 4) {
                    System.Console.WriteLine($"Expected 4 args but received {args.Length}");
                    return;
                }
                if (!metadata.frames.ContainsKey(args[2])) metadata.frames.Add(args[2], new());
                metadata.frames[args[2]].Add(Encoding.UTF8.GetBytes(args[3]));
                musicData.Reverse();
                musicData.AddRange(metadata.ToBytes().Reverse());
                musicData.Reverse();
                File.WriteAllBytes(args[0], musicData.ToArray());
                break;
            case "list":
                foreach (var k in metadata.frames.Keys) {
                    System.Console.WriteLine(k);
                }
                break;
        }
    }   
}