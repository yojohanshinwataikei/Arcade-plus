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

		public static float X(float start, float end, float t, ArcCurveType type)
		{
			switch (type)
			{
				default:
				case ArcCurveType.S:
					return S(start, end, t);
				case ArcCurveType.B:
					return B(start, end, t);
				case ArcCurveType.Si:
				case ArcCurveType.SiSi:
				case ArcCurveType.SiSo:
					return I(start, end, t);
				case ArcCurveType.So:
				case ArcCurveType.SoSi:
				case ArcCurveType.SoSo:
					return O(start, end, t);
			}
		}
		public static float Y(float start, float end, float t, ArcCurveType type)
		{
			switch (type)
			{
				default:
				case ArcCurveType.S:
				case ArcCurveType.Si:
				case ArcCurveType.So:
					return S(start, end, t);
				case ArcCurveType.B:
					return B(start, end, t);
				case ArcCurveType.SiSi:
				case ArcCurveType.SoSi:
					return I(start, end, t);
				case ArcCurveType.SiSo:
				case ArcCurveType.SoSo:
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
