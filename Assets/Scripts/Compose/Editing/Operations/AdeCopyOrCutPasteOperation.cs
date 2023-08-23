using System.Collections.Generic;
using UnityEngine;
using Arcade.Gameplay.Chart;
using Arcade.Compose.MarkingMenu;
using Arcade.Gameplay;
using Arcade.Compose.Command;
using System.Linq;
using Arcade.Compose.Editing;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

namespace Arcade.Compose.Operation
{
	public class AdeCopyOrCutPasteOperation : AdeOperation, IMarkingMenuItemProvider
	{
		public static AdeCopyOrCutPasteOperation Instance { get; private set; }

		public MarkingMenuItem CopyItem;
		public MarkingMenuItem CutItem;

		public bool IsOnly => false;
		public MarkingMenuItem[] Items
		{
			get
			{
				if (!ArcGameplayManager.Instance.IsLoaded) return null;
				if (AdeCursorManager.Instance == null) return null;
				if (AdeSelectionManager.Instance.SelectedNotes.Count == 0) return null;
				return new MarkingMenuItem[] { CopyItem, CutItem };
			}
		}

		private void Awake()
		{
			Instance = this;
		}
		private void Start()
		{
			AdeMarkingMenuManager.Instance.Providers.Add(this);
		}
		private void OnDestroy()
		{
			AdeMarkingMenuManager.Instance.Providers.Remove(this);
		}

		private async UniTask ExecuteCopyOrCut(bool isCut, CancellationToken cancellationToken)
		{
			var oldNotes = AdeSelectionManager.Instance.SelectedNotes.ToArray();
			AdeSelectionManager.Instance.DeselectAllNotes();
			if (oldNotes.Length == 0) return;
			List<ICommand> commands = new List<ICommand>();
			List<ArcNote> newNotes = new List<ArcNote>();
			foreach (var oldNote in oldNotes)
			{
				ArcEvent newNote = oldNote.Clone();
				if (newNote is ArcArcTap)
				{
					if (oldNotes.Contains((oldNote as ArcArcTap).Arc))
					{
						continue;
					}
					commands.Add(new AddArcTapCommand((oldNote as ArcArcTap).Arc, newNote as ArcArcTap));
					if (isCut)
					{
						commands.Add(new RemoveArcTapCommand((oldNote as ArcArcTap).Arc, oldNote as ArcArcTap));
					}
				}
				else
				{
					commands.Add(new AddArcEventCommand(newNote));
					if (isCut)
					{
						commands.Add(new RemoveArcEventCommand(oldNote));
					}
					if (newNote is ArcArc)
					{
						foreach (var at in (newNote as ArcArc).ArcTaps)
							newNotes.Add(at);
					}
				}
				newNotes.Add(newNote as ArcNote);
			}
			CommandManager.Instance.Prepare(new BatchCommand(commands.ToArray(), isCut ? "剪切" : "复制"));

			Action<int> updateTiming = (int timing) =>
			{
				int beginTiming = newNotes.Min((n) => n.Timing);
				if (beginTiming != timing)
				{
					foreach (var n in newNotes)
					{
						n.Judged = false;
						int diff = n.Timing - beginTiming;
						switch (n)
						{
							case ArcLongNote note:
								int duration = note.EndTiming - note.Timing;
								note.Timing = timing + diff;
								note.EndTiming = timing + duration + diff;
								note.Judging = false;
								(note as ArcArc)?.Rebuild();
								if (note is ArcArc)
								{
									ArcArcManager.Instance.CalculateArcRelationship();
								}
								(note as ArcArc)?.CalculateJudgeTimings();
								(note as ArcHold)?.CalculateJudgeTimings();
								break;
							case ArcArcTap note:
								note.RemoveArcTapConnection();
								note.Timing = timing + diff;
								note.Relocate();
								break;
							case ArcTap note:
								note.Timing = timing + diff;
								note.SetupArcTapConnection();
								break;
							default:
								n.Timing = timing + diff;
								break;
						}
					}
				}
			};

			try
			{
				while (true)
				{
					var newTiming = await AdeCursorManager.Instance.SelectTiming(Progress.Create(updateTiming), cancellationToken);
					updateTiming(newTiming);
					bool hasIllegalArcTap = false;
					foreach (var n in newNotes)
					{
						switch (n)
						{
							case ArcArcTap note:
								if (note.Arc.Timing > note.Timing || note.Arc.EndTiming < note.Timing)
								{
									hasIllegalArcTap = true;
								}
								break;
							default:
								break;
						}
					}

					if (!hasIllegalArcTap)
					{
						break;
					}
					else
					{
						AdeToast.Instance.Show("粘贴的 Arctap 中有一部分超出了所在 Arc 的时间范围，无法粘贴");
					}
				}
			}
			catch (OperationCanceledException ex)
			{
				CommandManager.Instance.Cancel();
				throw ex;
			}
			CommandManager.Instance.Commit();
			if (AdeInputManager.Instance.Inputs.MultipleSelection.IsPressed())
			{
				foreach (var note in newNotes)
				{
					AdeSelectionManager.Instance.SelectNote(note);
				}
			}
		}

		public override AdeOperationResult TryExecuteOperation()
		{
			if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.Copy))
			{
				var cancellation = new CancellationTokenSource();
				return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
				{
					task = ExecuteCopyOrCut(false, cancellation.Token),
					cancellation = cancellation,
				});
			}
			else if (AdeInputManager.Instance.CheckHotkeyActionPressed(AdeInputManager.Instance.Hotkeys.Cut))
			{
				var cancellation = new CancellationTokenSource();
				return AdeOperationResult.FromOngoingOperation(new AdeOngoingOperation
				{
					task = ExecuteCopyOrCut(true, cancellation.Token),
					cancellation = cancellation,
				});
			}
			return false;
		}

		public void ManuallyExecuteCopyOperation()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				var cancellation = new CancellationTokenSource();
				return new AdeOngoingOperation
				{
					task = ExecuteCopyOrCut(false, cancellation.Token),
					cancellation = cancellation,
				};
			});
		}

		public void ManuallyExecuteCutOperation()
		{
			AdeOperationManager.Instance.TryExecuteOperation(() =>
			{
				var cancellation = new CancellationTokenSource();
				return new AdeOngoingOperation
				{
					task = ExecuteCopyOrCut(true, cancellation.Token),
					cancellation = cancellation,
				};
			});
		}
	}
}
