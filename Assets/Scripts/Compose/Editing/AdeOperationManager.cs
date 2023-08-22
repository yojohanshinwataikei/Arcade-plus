using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Arcade.Compose
{
	public interface IAdeOngoingOperation
	{
		/// <returns>True if the operation is finished</returns>
		public bool Update();
		public void Cancel();
	}
	public struct AdeOperationResult
	{
		public bool operationExecuted;
		public IAdeOngoingOperation startedOperation;
		public static implicit operator AdeOperationResult(bool operationExecuted) => new AdeOperationResult { operationExecuted = operationExecuted };
		public static AdeOperationResult FromOngoingOperation(IAdeOngoingOperation startedOperation) => new AdeOperationResult { operationExecuted = true, startedOperation = startedOperation };
	}
	public abstract class AdeOperation : MonoBehaviour
	{
		public abstract AdeOperationResult TryExecuteOperation();
		public virtual void Reset() { }
	}
	public class AdeOperationManager : MonoBehaviour
	{
		public static AdeOperationManager Instance { get; private set; }
		private IAdeOngoingOperation ongoingOperation;
		public bool HasOngoingOperation{get=>ongoingOperation!=null;}
		public AdeOperation[] operations;
		private void Awake()
		{
			Instance = this;
		}
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

		public delegate IAdeOngoingOperation OperationExecutor();
		public void TryExecuteOperation(OperationExecutor executor)
		{
			if (ongoingOperation != null)
			{
				AdeToast.Instance.Show("有正在进行的操作，不能执行其他操作");
			}
			else
			{
				var ongoingOperation = executor();
				this.ongoingOperation = ongoingOperation;
			}
		}
	}
}