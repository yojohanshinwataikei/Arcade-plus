using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Arcade.Util.UniTaskHelper
{
	public static class UniTaskHelper
	{
		public static async UniTask WithExceptionLogger(this UniTask task)
		{
			try
			{
				await task;
			}
			catch (Exception ex) when (!(ex is OperationCanceledException))
			{
				Debug.LogException(ex);
			}
		}
	}
}