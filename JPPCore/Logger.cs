using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.EditorInput;

namespace JPP.Core
{
    public class Logger : ILogger
    {
        #region Interfaces

        public void Entry(string message)
        {
            Entry(message, Severity.Information);
        }

        public void Entry(string message, Severity sev)
        {
            Editor ed = Application.DocumentManager.CurrentDocument.Editor;
            ed.WriteMessage(message);
        }

        #endregion
    }
}