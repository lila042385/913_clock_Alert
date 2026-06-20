// v1.01 20260620 10:25
// 履歴: 生成WAV波形を30秒に拡張し、15秒かけて音量が徐々に大きくなるフェードイン処理を追加
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
            // Generates a 30-second beep sound (monaural, 44100Hz, 16-bit PCM)
            // Pattern: 4 beeps (0.1s sound, 0.1s silence) followed by 1.2s silence per 2.0s cycle.
            // Volume fades in over 15 seconds.
            int sampleRate = 44100;
            int bitsPerSample = 16;
            int channels = 1;
            double durationSeconds = 30.0;
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

                // 2秒ごとのループ周期で鳴動を判定
                double periodicTime = time % 2.0;
                bool soundOn = false;
                if (periodicTime < 0.8)
                {
                    int cycle = (int)(periodicTime / 0.2);
                    double cycleTime = periodicTime - (cycle * 0.2);
                    if (cycleTime < 0.1)
                    {
                        soundOn = true;
                    }
                }

                if (soundOn)
                {
                    // Sine Wave
                    double angle = 2.0 * Math.PI * frequency * time;

                    // 音量フェードインロジック:
                    // 0.0s - 5.0s: 最大音量の 10% から 20% へ線形補間
                    // 5.0s - 15.0s: 最大音量の 20% から 100% へ線形補間
                    // 15.0s - 30.0s: 最大音量 (100%)
                    double volumeCoeff;
                    if (time < 5.0)
                    {
                        volumeCoeff = 0.1 + (time / 5.0) * 0.1;
                    }
                    else if (time < 15.0)
                    {
                        volumeCoeff = 0.2 + ((time - 5.0) / 10.0) * 0.8;
                    }
                    else
                    {
                        volumeCoeff = 1.0;
                    }

                    double maxVolume = 16000.0;
                    value = (short)(Math.Sin(angle) * maxVolume * volumeCoeff);
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
// v1.01 20260620 10:25
