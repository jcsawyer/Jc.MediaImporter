using System;
using System.Windows.Input;

namespace Jc.MediaImporter;

public partial class App
{
    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public Command(Action<object> action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter) => _action != null;
        public void Execute(object parameter) => _action?.Invoke(parameter);

        private Action<object> _action = null;
    }

    public static readonly Command OpenAppDataDirCommand = new Command(_ => Native.OS.OpenInFileManager(Native.OS.DataDir));
    public static readonly Command QuitCommand = new Command(_ => Quit(0));
}