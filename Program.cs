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
        const string file = "CallingForHelpWithGain~4";
        static int bitsPerSample = 16;
        static bool getDuty = true;
        const float sampleRate = 1000000;
        const float clockRate = 60000000;
        const int highSpeedClockDiv = 3;
        static float maxValue;
        static int periodForSampleRate = (int)(((1/sampleRate) * clockRate) / (highSpeedClockDiv));
        static void Main(string[] args)
        {
            int[] left;
            int[] right;
            openWav(file + ".wav", out left, out right);
            FileStream output = File.Create(file + ".txt");
            StreamWriter streamWriter = new StreamWriter(output);

            if (left != null)
            {
                foreach (var d in left)
                    streamWriter.Write(d + ", ");
            
            }
            Console.Out.Write("Done.  Length is " + left.Length);
            output.Close();
            Console.ReadKey();            
        }

        // convert two bytes to one double in the range 0 to 1
        static double bytesToInt(byte firstByte, byte secondByte, bool offset)
        {
            // convert two bytes to one short (little endian)
            short s = (short) ((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            if (offset == false) return s; // 32768;

            else
            {
                double answer = (s + (Math.Pow(2, bitsPerSample) / 2)) / 2;
                return (int) answer;
            }
        }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        public static void openWav(string filename, out int[] left, out int[] right)
        {
            byte[] wav = File.ReadAllBytes(filename);

            // Determine if mono or stereo
            int channels = wav[22];     // Forget byte 23 as most of WAVs are 1 or 2 channels

            bitsPerSample = (int) bytesToInt(wav[34], wav[35], false);
            Console.WriteLine("bits per sample is " + bitsPerSample);
            if (bitsPerSample == 16) maxValue = 32767;
            else if (bitsPerSample == 8) maxValue = 127;

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
            if (channels == 2) samples /= 2;        // 4 bytes per sample (16 bit stereo)

            // Allocate memory (right will be null if only mono sound)
            left = new int[samples];
            if (channels == 2) right = new int[samples];
            else right = null;

            // Write to double array/s:
            int i = 0;
            Console.Out.Write("data segment length is " + (wav.Length - pos));
            while (pos < wav.Length)
            {
                if (bitsPerSample == 16)
                {
                    if (!getDuty)
                        left[i] = (int) bytesToInt(wav[pos], wav[pos + 1], true);
                    else
                        left[i] =(int) Math.Round(((float)((bytesToInt(wav[pos], wav[pos + 1], true) / maxValue) * (float)periodForSampleRate)));//32767
                    pos += 2;
                }
                else if (bitsPerSample == 8)
                {
                    double answer = (wav[pos] + (((Math.Pow(2, bitsPerSample)) / 2))) / 2;
                    left[i] = (int)answer;
                    if (getDuty)
                    {
                        left[i] = (int) Math.Round((float)((left[i] / maxValue) * (float)periodForSampleRate));
                    }
                    pos += 1;
                }
                if (channels == 2 && bitsPerSample == 16)
                {
                    if (!getDuty)
                        right[i] = (int) bytesToInt(wav[pos], wav[pos + 1], true);
                    else
                        right[i] = (int) Math.Round((float)((bytesToInt(wav[pos], wav[pos + 1], true) / maxValue) * (float)periodForSampleRate));
                    pos += 2;
                }
                else if (channels == 2 && bitsPerSample == 8)
                {
                    right[i] = (int) Math.Round(wav[pos] + (((Math.Pow(2, bitsPerSample)) / 2)) / 2);
                    if (getDuty)
                    {
                        right[i] = (int) Math.Round((float)((right[i] / maxValue) * (float)periodForSampleRate));
                    }
                    pos += 1;
                }
                i++;
            }
        }
    }
}
