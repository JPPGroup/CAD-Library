using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using System;

namespace JPP.Core
{
    /// <inheritdoc />
    /// <summary>
    /// Stub command class for dealing wih button presses. All autocad button presses are sent to the editor to be executed.
    /// </summary>
    public class RibbonCommandHandler : System.Windows.Input.ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true; //return true means the button always enabled
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            //TODO: Add authentication check here
            RibbonCommandItem cmd = parameter as RibbonCommandItem;
            Document dwg = Application.DocumentManager.MdiActiveDocument;
            if (cmd != null) dwg.SendStringToExecute((string) cmd.CommandParameter, true, false, false);
        }
    }
}
