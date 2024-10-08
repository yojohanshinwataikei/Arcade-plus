using UnityEngine;
using Arcade.Gameplay.Chart;

namespace Arcade.Gameplay
{
	public static class ArcAlgorithm
	{
		public static float ArcXToWorld(float x)
		{
			return -8.5f * x + 4.25f;
		}
		public static float ArcYToWorld(float y)
		{
			return 1 + 4.5f * y;
		}
		public static float WorldXToArc(float x)
		{
			return (x - 4.25f) / -8.5f;
		}
		public static float WorldYToArc(float y)
		{
			return (y - 1) / 4.5f;
		}
		public static float S(float start, float end, float t)
		{
			return (1 - t) * start + end * t;
		}
		public static float O(float start, float end, float t)
		{
			return start + (end - start) * (1 - Mathf.Cos(1.5707963f * t));
		}
		public static float I(float start, float end, float t)
		{
			return start + (end - start) * (Mathf.Sin(1.5707963f * t));
		}
		public static float B(float start, float end, float t)
		{
			float o = 1 - t;
			return Mathf.Pow(o, 3) * start + 3 * Mathf.Pow(o, 2) * t * start + 3 * o * Mathf.Pow(t, 2) * end + Mathf.Pow(t, 3) * end;
		}

		public static float X(float start, float end, float t, ArcLineType type)
		{
			switch (type)
			{
				default:
				case ArcLineType.S:
					return S(start, end, t);
				case ArcLineType.B:
					return B(start, end, t);
				case ArcLineType.Si:
				case ArcLineType.SiSi:
				case ArcLineType.SiSo:
					return I(start, end, t);
				case ArcLineType.So:
				case ArcLineType.SoSi:
				case ArcLineType.SoSo:
					return O(start, end, t);
			}
		}
		public static float Y(float start, float end, float t, ArcLineType type)
		{
			switch (type)
			{
				default:
				case ArcLineType.S:
				case ArcLineType.Si:
				case ArcLineType.So:
					return S(start, end, t);
				case ArcLineType.B:
					return B(start, end, t);
				case ArcLineType.SiSi:
				case ArcLineType.SoSi:
					return I(start, end, t);
				case ArcLineType.SiSo:
				case ArcLineType.SoSo:
					return O(start, end, t);
			}
		}

		public static float Qi(float value)
		{
			return value * value * value;
		}
		public static float Qo(float value)
		{
			return (value = value - 1) * value * value + 1;
		}
	}
}
