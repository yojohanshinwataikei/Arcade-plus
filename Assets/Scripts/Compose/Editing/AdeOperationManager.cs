using System.Collections;
using System.Collections.Generic;
using Arcade.Compose.Command;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Gameplay.Chart;
using Arcade.Util.UnityExtension;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Arcade.Compose
{
	public interface IAdeOngoingOperation
	{
		/// <returns>True if the operation is finished</returns>
		bool Update();
	}
	public struct AdeOperationResult
	{
		public bool operationExecuted;
		public IAdeOngoingOperation startedOperation;
	}
	public interface IAdeOperation
	{
		public AdeOperationResult TryExecuteOperation();
	}
	public class AdeOperationManager : MonoBehaviour
	{
		private IAdeOngoingOperation ongoingOperation;
		public IAdeOperation[] operations;
		void Update()
		{
			if (ongoingOperation != null)
			{
				if(ongoingOperation.Update()){
					ongoingOperation=null;
				}else{
					return;
				}
			}
			foreach (var operation in operations)
			{
				var result = operation.TryExecuteOperation();
				if (result.operationExecuted)
				{
					ongoingOperation = result.startedOperation;
					return;
				}
			}
		}
	}
}