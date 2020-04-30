
using System;
using NAudio.Wave;
using NVorbis;
using UnityEngine;

namespace Arcade.Util.Loader
{
	public static class Loader
	{

		private class AudioDataHost
		{
			public float[] audioData;
			public int position = 0;

			public int channels =2;
			public AudioDataHost(float[] audioData,int channels)
			{
				this.audioData = audioData;
				this.channels=channels;
			}
			public void PCMReaderCallback(float[] buffer)
			{
				if (position <= audioData.Length)
				{
					if (position + buffer.Length <= audioData.Length)
					{
						Array.Copy(audioData, position, buffer, 0, buffer.Length);
					}
					else
					{
						Array.Copy(audioData, position, buffer, 0, audioData.Length - position);
					}
				}
				position += buffer.Length;
			}
			public void PCMSetPositionCallback(int newPosition)
			{
				position = newPosition*channels;
			}
		}
		// This is used to load audio files that supported by NAudio like wav/mp3
		public static AudioClip LoadWavOrMp3AudioFile(string path)
		{
			float[] audioData = null;
			int channels = 0;
			int sampleRate = 0; try
			{
				using (AudioFileReader reader = new AudioFileReader(path))
				{
					//Note: to be simple, we do not load large file with samples count larger than int limit
					//This will be enough for most file, especially the sound effects in the skin folder
					if (reader.Length > 0x7FFFFFFFL*sizeof(float))
					{
						return null;
					}
					float[] data = new float[reader.Length/sizeof(float)];
					reader.Read(data, 0, (int)(reader.Length/sizeof(float)));
					channels = reader.WaveFormat.Channels;
					sampleRate = reader.WaveFormat.SampleRate;
					audioData = data;
				}
			}
			catch
			{
				return null;
			}
			if (audioData == null)
			{
				return null;
			}
			AudioDataHost dataHost = new AudioDataHost(audioData,channels);
			AudioClip clip = AudioClip.Create(path, audioData.Length/channels, channels, sampleRate, true, dataHost.PCMReaderCallback, dataHost.PCMSetPositionCallback);
			return clip;
		}
		// This is used to load audio files that supported by NVorbis like ogg
		public static AudioClip LoadOggAudioFile(string path)
		{
			float[] audioData = null;
			int channels = 0;
			int sampleRate = 0; try
			{
				using (VorbisReader reader = new VorbisReader(path))
				{
					//Note: Same here
					if (reader.TotalSamples*reader.Channels > 0x7FFFFFFFL)
					{
						return null;
					}
					float[] data = new float[reader.TotalSamples*reader.Channels];
					reader.ReadSamples(data, 0, (int)(reader.TotalSamples*reader.Channels));
					channels = reader.Channels;
					sampleRate = reader.SampleRate;
					audioData = data;
				}
			}
			catch
			{
				return null;
			}
			if (audioData == null)
			{
				return null;
			}
			AudioDataHost dataHost = new AudioDataHost(audioData,channels);
			AudioClip clip = AudioClip.Create(path, audioData.Length/channels, channels, sampleRate, true, dataHost.PCMReaderCallback, dataHost.PCMSetPositionCallback);
			return clip;
		}
		// This try both nvorbis and naudio
		public static AudioClip LoadAudioFile(string path)
		{
			AudioClip clip = null;
			clip = LoadOggAudioFile(path);
			if (clip != null)
			{
				return clip;
			}
			clip = LoadWavOrMp3AudioFile(path);
			if (clip != null)
			{
				return clip;
			}
			return clip;
		}
	}
}