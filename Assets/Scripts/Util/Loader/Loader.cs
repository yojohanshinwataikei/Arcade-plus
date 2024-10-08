
using System;
using System.IO;
using NAudio.Wave;
using NVorbis;
using NLayer;
using UnityEngine;
using Dummiesman;

namespace Arcade.Util.Loader
{
	public static class Loader
	{

		private class AudioDataHost
		{
			public float[] audioData;
			public int position = 0;

			public int channels = 2;
			public AudioDataHost(float[] audioData, int channels)
			{
				this.audioData = audioData;
				this.channels = channels;
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
				position = newPosition * channels;
			}
		}
		// This is used to load audio files that supported by NLayer like mp3
		public static AudioClip LoadMp3AudioFile(string path)
		{
			float[] audioData = null;
			int channels = 0;
			int sampleRate = 0;
			try
			{
				using (MpegFile file = new MpegFile(path))
				{
					//Note: to be simple, we do not load large file with samples count larger than int limit
					//This will be enough for most file, especially the sound effects in the skin folder
					if (file.Length > 0x7FFFFFFFL * sizeof(float))
					{
						return null;
					}
					float[] data = new float[file.Length / sizeof(float)];
					file.ReadSamples(data, 0, (int)(file.Length / sizeof(float)));
					channels = file.Channels;
					sampleRate = file.SampleRate;
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
			AudioDataHost dataHost = new AudioDataHost(audioData, channels);
			AudioClip clip = AudioClip.Create(path, audioData.Length / channels, channels, sampleRate, true, dataHost.PCMReaderCallback, dataHost.PCMSetPositionCallback);
			return clip;
		}
		// This is used to load audio files that supported by NAudio like wav/mp3(on windows)
		public static AudioClip LoadWavOrMp3AudioFile(string path)
		{
			float[] audioData = null;
			int channels = 0;
			int sampleRate = 0;
			try
			{
				using (AudioFileReader reader = new AudioFileReader(path))
				{
					//Note: to be simple, we do not load large file with samples count larger than int limit
					//This will be enough for most file, especially the sound effects in the skin folder
					if (reader.Length > 0x7FFFFFFFL * sizeof(float))
					{
						return null;
					}
					float[] data = new float[reader.Length / sizeof(float)];
					reader.Read(data, 0, (int)(reader.Length / sizeof(float)));
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
			AudioDataHost dataHost = new AudioDataHost(audioData, channels);
			AudioClip clip = AudioClip.Create(path, audioData.Length / channels, channels, sampleRate, true, dataHost.PCMReaderCallback, dataHost.PCMSetPositionCallback);
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
					if (reader.TotalSamples * reader.Channels > 0x7FFFFFFFL)
					{
						return null;
					}
					float[] data = new float[reader.TotalSamples * reader.Channels];
					reader.ReadSamples(data, 0, (int)(reader.TotalSamples * reader.Channels));
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
			AudioDataHost dataHost = new AudioDataHost(audioData, channels);
			AudioClip clip = AudioClip.Create(path, audioData.Length / channels, channels, sampleRate, true, dataHost.PCMReaderCallback, dataHost.PCMSetPositionCallback);
			return clip;
		}
		// This try all decoders used in arcade
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
			clip = LoadMp3AudioFile(path);
			if (clip != null)
			{
				return clip;
			}
			return clip;
		}

		public static Texture2D LoadTexture2D(string path)
		{
			byte[] file;
			try
			{
				file = File.ReadAllBytes(path);
			}
			catch
			{
				return null;
			}
			//TODO: Completly remove mipmap after GPU optimize
			Texture2D texture = new Texture2D(1, 1);
			bool success = ImageConversion.LoadImage(texture, file, true);
			if (success)
			{
				texture.wrapMode = TextureWrapMode.Clamp;
				texture.name = path;
				texture.mipMapBias = -4;
				return texture;
			}
			else
			{
				UnityEngine.Object.Destroy(texture);
				return null;
			}
		}

		public static Mesh LoadObjMesh(string path)
		{
			try
			{
				OBJLoader loader = new OBJLoader
				{
					SplitMode = SplitMode.None
				};
				GameObject obj = loader.Load(path);
				MeshRenderer renderer = obj.GetComponentInChildren<MeshRenderer>();
				MeshFilter filter = obj.GetComponentInChildren<MeshFilter>();
				Mesh mesh = filter.sharedMesh;
				foreach (var material in renderer.sharedMaterials)
				{
					UnityEngine.Object.Destroy(material);
				}
				UnityEngine.Object.Destroy(obj);
				return mesh;
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"Can not load obj mesh, path: {path} ex:{ex}");
			}
			return null;
		}
	}
}