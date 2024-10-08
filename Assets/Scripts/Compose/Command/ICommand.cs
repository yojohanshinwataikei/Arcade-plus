using System.Linq;

namespace Arcade.Compose.Command
{
	public interface ICommand
	{
		string Name
		{
			get;
		}
		void Do();
		void Undo();
	}
	public class BatchCommand : ICommand
	{
		public string Name { get; private set; }
		private readonly ICommand[] commands = null;
		public BatchCommand(ICommand[] commands, string description)
		{
			this.commands = commands;
			Name = description;
		}
		public void Do()
		{
			foreach (var c in commands)
			{
				c.Do();
			}
		}
		public void Undo()
		{
			foreach (var c in commands.Reverse())
			{
				c.Undo();
			}
		}
	}
}
