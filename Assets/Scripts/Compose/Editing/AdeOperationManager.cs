using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Arcade.Compose.MarkingMenu;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Arcade.Compose
{
	public struct AdeOngoingOperation
	{
		public UniTask task;
		public CancellationTokenSource cancellation;
	}
	public struct AdeOperationResult
	{
		public bool operationExecuted;
		public AdeOngoingOperation? startedOperation;
		public static implicit operator AdeOperationResult(bool operationExecuted) => new AdeOperationResult { operationExecuted = operationExecuted, startedOperation = null };
		public static AdeOperationResult FromOngoingOperation(AdeOngoingOperation startedOperation) => new AdeOperationResult { operationExecuted = true, startedOperation = startedOperation };
	}
	public abstract class AdeOperation : AdeMarkingMenuItemProvider
	{
		public abstract AdeOperationResult TryExecuteOperation();
		public virtual void Reset() { }
	}
	public class AdeOperationManager : AdeMarkingMenuItemProvider
	{
		public static AdeOperationManager Instance { get; private set; }
		private AdeOngoingOperation? ongoingOperation = null;
		public bool HasOngoingOperation { get => ongoingOperation != null; }
		public AdeOperation[] operations;


		public MarkingMenuItem CancelMenuEntry;
		public override bool IsOnlyMarkingMenu => HasOngoingOperation;
		public override MarkingMenuItem[] MarkingMenuItems
		{
			get
			{
				if (HasOngoingOperation) {
					List<MarkingMenuItem> items = new List<MarkingMenuItem>
                    {
                        CancelMenuEntry
                    };
					items.AddRange(AdeGridManager.Instance.MarkingMenuItems);
					return items.ToArray();
				};
				return null;
			}
		}
		private void Awake()
		{
			Instance = this;
		}
		void Update()
		{
			if (ongoingOperation != null)
			{
				if (ongoingOperation.Value.task.Status != UniTaskStatus.Pending)
				{
					ongoingOperation = null;
				}
				return;
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

		private void SetOngoingOperation(AdeOngoingOperation? ongoingOperation)
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

		public delegate AdeOngoingOperation? OperationExecutor();
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
		public void CancelOngoingOperation(){
			if (ongoingOperation != null)
			{
				if (ongoingOperation.Value.task.Status == UniTaskStatus.Pending)
				{
					ongoingOperation.Value.cancellation.Cancel();
				}
				ongoingOperation = null;
			}
		}
	}
}