using System;
using System.IO;
using UnityEngine;

// 간단한 PCM16 WAV 인코더 (마이크 업로드용)
public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null) return Array.Empty<byte>();

        var samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // float -1..1 -> PCM16
        byte[] pcm = FloatToPCM16(samples);
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            WriteWavHeader(bw, clip.channels, clip.frequency, pcm.Length);
            bw.Write(pcm);
            return ms.ToArray();
        }
    }

    private static byte[] FloatToPCM16(float[] samples)
    {
        var pcm = new byte[samples.Length * 2];
        int i = 0; int j = 0;
        while (i < samples.Length)
        {
            short v = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), short.MinValue, short.MaxValue);
            pcm[j++] = (byte)(v & 0xFF);
            pcm[j++] = (byte)((v >> 8) & 0xFF);
            i++;
        }
        return pcm;
    }

    private static void WriteWavHeader(BinaryWriter bw, int channels, int sampleRate, int dataLength)
    {
        int byteRate = sampleRate * channels * 2;
        int blockAlign = channels * 2;
        int chunkSize = 36 + dataLength;

        // RIFF 헤더
        bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(chunkSize);
        bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt 서브청크
        bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16);                // Subchunk1Size (PCM)
        bw.Write((short)1);         // AudioFormat (1 = PCM)
        bw.Write((short)channels);
        bw.Write(sampleRate);
        bw.Write(byteRate);
        bw.Write((short)blockAlign);
        bw.Write((short)16);        // BitsPerSample

        // data 서브청크
        bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        bw.Write(dataLength);
    }
}

