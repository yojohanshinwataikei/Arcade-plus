using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		public static implicit operator AdeOperationResult(bool operationExecuted) => new AdeOperationResult { operationExecuted = operationExecuted };
	}
	public abstract class AdeOperation: MonoBehaviour
	{
		public abstract AdeOperationResult TryExecuteOperation();
		public virtual void Reset() { }
	}
	public class AdeOperationManager : MonoBehaviour
	{
		private IAdeOngoingOperation ongoingOperation;
		public AdeOperation[] operations;
		void Update()
		{
			if (ongoingOperation != null)
			{
				if (ongoingOperation.Update())
				{
					ongoingOperation = null;
				}
				else
				{
					return;
				}
			}
			foreach (var operation in operations)
			{
				var result = operation.TryExecuteOperation();
				if (result.operationExecuted)
				{
					SetOngoingOperation(result.startedOperation);
					return;
				}
			}
		}

		private void SetOngoingOperation(IAdeOngoingOperation ongoingOperation)
		{
			if (ongoingOperation != null)
			{
				foreach (var operation in operations)
				{
					operation.Reset();
				}
				AdeSelectionManager.Instance.DeselectAllNotes();
			}
			this.ongoingOperation = ongoingOperation;
		}
	}
}