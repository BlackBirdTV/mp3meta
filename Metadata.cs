using System.Text;

namespace mp3meta.Classes;

public class Metadata {
    public Dictionary<string, List<byte[]>> frames = new();

    public static Metadata Parse(byte[] bytes) {
        var res = new Metadata();
        var state = 0;
        var iteration = 0;
        var lengthBuf = new byte[4];
        var length = 0;
        var name = new byte[4];
        var buf = new List<byte>();
        foreach (byte b in bytes) {
            if (state == 0) {
                name[iteration] = b;
                if (iteration == 3) {
                    state = 1;
                    iteration = -1;
                }
            }
            else if (state == 1) {
                lengthBuf[iteration] = b;
                if (iteration == 3) {
                    state = 2;
                    iteration = -1;
                    if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBuf);
                    length = BitConverter.ToInt32(lengthBuf, 0) - 1;
                }
            }
            else if (state == 2) {
                if (iteration == 2) {
                    iteration = -1;
                    state = 3;
                }
            }
            else if (state == 3) {
                length--;
                buf.Add(b);
                if (length == 0) {
                    state = 0;
                    iteration = -1;
                    var frameName = Encoding.UTF8.GetString(name);
                    if (frameName != "????") {
                        if (!res.frames.ContainsKey(frameName)) res.frames.Add(frameName, new());
                        res.frames[frameName].Add(buf.ToArray());
                    }

                    buf = new();
                }
            }

            iteration++;
        }

        return res;
    }

    public byte[] ToBytes() {
        var bytes = new List<byte>();
        foreach (var frame in frames) 
            foreach (var frameValue in frame.Value) {
                bytes.AddRange(Encoding.UTF8.GetBytes(frame.Key));
                bytes.AddRange(BitConverter.GetBytes(frameValue.Length+1).Reverse());
                bytes.AddRange(new byte[] {0, 0, 0});
                bytes.AddRange(frameValue);
            }
        bytes.Reverse();
        var lengthBytes = BitConverter.GetBytes(bytes.Count);
        lengthBytes = BitConverter.GetBytes(lengthBytes[0] << 24 | lengthBytes[1] << 17 | lengthBytes[2] << 10 | lengthBytes[3]);
        bytes.AddRange(lengthBytes.Reverse());
        bytes.Add(0);
        bytes.Add(0);
        bytes.Add(3);
        bytes.AddRange(Encoding.UTF8.GetBytes("ID3").Reverse());
        bytes.Reverse();
        return bytes.ToArray();
    }
}