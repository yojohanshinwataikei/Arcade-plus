using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityScript.Lang;

namespace Arcade.Compose.Command
{
	public class CommandManager : MonoBehaviour
	{
		public static CommandManager Instance { get; private set; }
		private void Awake()
		{
			Instance = this;
		}
		private bool undoClickable, redoClickable;
		private bool UndoClickable
		{
			get
			{
				return undoClickable;
			}
			set
			{
				if (undoClickable != value)
				{
					UndoButton.interactable = value;
					undoClickable = value;
				}
			}
		}
		private bool RedoClickable
		{
			get
			{
				return redoClickable;
			}
			set
			{
				if (redoClickable != value)
				{
					RedoButton.interactable = value;
					redoClickable = value;
				}
			}
		}
		public Button UndoButton, RedoButton;

		public uint bufferSize=200;

		//TODO: use a generic deque when dotnet library has one
		private UnityScript.Lang.Array undo = new UnityScript.Lang.Array();
		private UnityScript.Lang.Array redo = new UnityScript.Lang.Array();

		private void Update()
		{
			if (Input.GetKey(KeyCode.LeftControl))
			{
				if (Input.GetKeyDown(KeyCode.Z)) Undo();
				else if (Input.GetKeyDown(KeyCode.Y)) Redo();
			}
			UndoClickable = undo.Count != 0;
			RedoClickable = redo.Count != 0;
		}

		public void Add(ICommand command)
		{
			command.Do();
			AdeToast.Instance.Show($"执行了 {command.Name}");
			undo.Push(command);
			if(undo.Count>bufferSize){
				undo.Shift();
			}
			redo.Clear();
		}
		public void Undo()
		{
			if (undo.Count == 0) return;
			ICommand cmd = undo.Pop() as ICommand;
			cmd.Undo();
			AdeToast.Instance.Show($"撤销了 {cmd.Name}");
			redo.Push(cmd);
		}
		public void Redo()
		{
			if (redo.Count == 0) return;
			ICommand cmd = redo.Pop() as ICommand;
			cmd.Do();
			AdeToast.Instance.Show($"重做了 {cmd.Name}");
			undo.Push(cmd);
		}
		public void Clear()
        {
            undo.Clear();
            redo.Clear();
        }

		public void SetBufferSize(uint size)
		{
			bufferSize=size;
			while(undo.Count+redo.Count>bufferSize){
				if(undo.Count>0){
					undo.shift();
				}else{
					redo.shift();
				}
			}
		}
	}
}
