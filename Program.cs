using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WAVRipper
{
    class Program
    {
        const string file = "1000HzSine";
        static void Main(string[] args)
        {
            double[] left;
            double[] right;
            openWav(file + ".wav", out left, out right);
            FileStream output = File.Create(file + ".txt");
            StreamWriter streamWriter = new StreamWriter(output);

            if (left != null)
            {
                foreach (var d in left)
                    streamWriter.Write(d + ", ");
            }
            Console.ReadKey();
            
        }

        // convert two bytes to one double in the range 0 to 1
        static int bytesToDouble(byte firstByte, byte secondByte, bool offset)
        {
            // convert two bytes to one short (little endian)
            short s = (short) ((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            if (offset == false) return s; // 32768;

            else
            {
                double answer = (s + 32768) / 2;
                return (int) answer;
            }
        }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        public static void openWav(string filename, out double[] left, out double[] right)
        {
            byte[] wav = File.ReadAllBytes(filename);

            // Determine if mono or stereo
            int channels = wav[22];     // Forget byte 23 as most of WAVs are 1 or 2 channels

            int bitsPerSample = (int) bytesToDouble(wav[34], wav[35], false);
            Console.WriteLine("bits per sample is " + bitsPerSample);

            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            // Pos is now positioned to start of actual sound data.
            int samples = (wav.Length - pos) / (bitsPerSample / 8);     // 2 bytes per sample (16 bit sound mono)
            if (channels == 2) samples /= (bitsPerSample / 8);        // 4 bytes per sample (16 bit stereo)

            // Allocate memory (right will be null if only mono sound)
            left = new double[samples];
            if (channels == 2) right = new double[samples];
            else right = null;

            // Write to double array/s:
            int i = 0;
            while (pos < wav.Length)
            {
                left[i] = bytesToDouble(wav[pos], wav[pos + 1], true);
                pos += 2;
                if (channels == 2)
                {
                    right[i] = bytesToDouble(wav[pos], wav[pos + 1], true);
                    pos += 2;
                }
                i++;
            }
        }
    }
}
