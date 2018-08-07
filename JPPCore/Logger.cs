using Autodesk.AutoCAD.EditorInput;

namespace JPP.Core
{
    public class Logger : ILogger
    {
        public void Entry(string message)
        {
            Entry(message, Severity.Information);
        }

        public void Entry(string message, Severity sev)
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument.Editor;
            ed.WriteMessage(message);
        }
    }
}
