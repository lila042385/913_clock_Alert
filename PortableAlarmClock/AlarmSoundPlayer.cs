// v1.00 20260617 23:40
using System;
using System.IO;
using System.Media;

namespace PortableAlarmClock
{
    public static class AlarmSoundPlayer
    {
        private static SoundPlayer? _player;
        private static MemoryStream? _wavStream;
        private static readonly object _lock = new object();

        static AlarmSoundPlayer()
        {
            try
            {
                _wavStream = GenerateAlarmWavStream();
                if (_wavStream != null)
                {
                    _player = new SoundPlayer(_wavStream);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize AlarmSoundPlayer wav stream.", ex);
            }
        }

        public static void Play()
        {
            lock (_lock)
            {
                try
                {
                    if (_player != null)
                    {
                        _player.PlayLooping();
                        Logger.Info("Started looping alarm sound.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to play alarm sound.", ex);
                }
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                try
                {
                    if (_player != null)
                    {
                        _player.Stop();
                        Logger.Info("Stopped alarm sound.");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Failed to stop alarm sound.", ex);
                }
            }
        }

        private static MemoryStream GenerateAlarmWavStream()
        {
            // Generates a 2-second beep sound (monaural, 44100Hz, 16-bit PCM)
            // Pattern: 4 beeps (0.1s sound, 0.1s silence) followed by 1.2s silence. Total 2.0s.
            int sampleRate = 44100;
            int bitsPerSample = 16;
            int channels = 1;
            double durationSeconds = 2.0;
            int numSamples = (int)(sampleRate * durationSeconds);
            int dataLength = numSamples * channels * (bitsPerSample / 8);

            byte[] wavBytes = new byte[44 + dataLength];

            // 1. RIFF Header
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, wavBytes, 0, 4);
            int fileSize = 36 + dataLength;
            Array.Copy(BitConverter.GetBytes(fileSize), 0, wavBytes, 4, 4);
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, wavBytes, 8, 4);

            // 2. Format Chunk
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, wavBytes, 12, 4);
            int fmtChunkSize = 16;
            Array.Copy(BitConverter.GetBytes(fmtChunkSize), 0, wavBytes, 16, 4);
            short formatType = 1; // PCM
            Array.Copy(BitConverter.GetBytes(formatType), 0, wavBytes, 20, 2);
            short numChannels = (short)channels;
            Array.Copy(BitConverter.GetBytes(numChannels), 0, wavBytes, 22, 2);
            Array.Copy(BitConverter.GetBytes(sampleRate), 0, wavBytes, 24, 4);
            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            Array.Copy(BitConverter.GetBytes(byteRate), 0, wavBytes, 28, 4);
            short blockAlign = (short)(channels * (bitsPerSample / 8));
            Array.Copy(BitConverter.GetBytes(blockAlign), 0, wavBytes, 32, 2);
            short bitsPerSampleVal = (short)bitsPerSample;
            Array.Copy(BitConverter.GetBytes(bitsPerSampleVal), 0, wavBytes, 34, 2);

            // 3. Data Chunk
            Array.Copy(System.Text.Encoding.ASCII.GetBytes("data"), 0, wavBytes, 36, 4);
            Array.Copy(BitConverter.GetBytes(dataLength), 0, wavBytes, 40, 4);

            // Generate Wave Data (Sine wave at 1500Hz)
            double frequency = 1500.0;
            int writePos = 44;

            for (int i = 0; i < numSamples; i++)
            {
                double time = (double)i / sampleRate;
                short value = 0;

                // Determine if we should play sound based on pattern (4 beeps of 0.1s on / 0.1s off)
                // 0.0 - 0.1s: ON
                // 0.1 - 0.2s: OFF
                // 0.2 - 0.3s: ON
                // 0.3 - 0.4s: OFF
                // 0.4 - 0.5s: ON
                // 0.5 - 0.6s: OFF
                // 0.6 - 0.7s: ON
                // 0.7 - 2.0s: OFF
                bool soundOn = false;
                if (time < 0.8)
                {
                    int cycle = (int)(time / 0.2);
                    double cycleTime = time - (cycle * 0.2);
                    if (cycleTime < 0.1)
                    {
                        soundOn = true;
                    }
                }

                if (soundOn)
                {
                    // Sine Wave
                    double angle = 2.0 * Math.PI * frequency * time;
                    value = (short)(Math.Sin(angle) * 16000); // 16000 volume amplitude
                }

                byte[] sampleBytes = BitConverter.GetBytes(value);
                Array.Copy(sampleBytes, 0, wavBytes, writePos, 2);
                writePos += 2;
            }

            var stream = new MemoryStream(wavBytes);
            return stream;
        }
    }
}
// v1.00 20260617 23:40
