using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arcade.Util.Misc
{
	public static class MaskUtil
	{
		public static uint SetMask(uint origin, uint mask, bool value)
		{
			if (value)
			{
				return origin | (mask);
			}
			else
			{
				return origin & (~mask);
			}
		}
	}
}
