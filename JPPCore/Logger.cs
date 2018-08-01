using Autodesk.AutoCAD.EditorInput;

namespace JPP.Core
{
    public class Logger : ILogger
    {
        public void Log(string message)
        {
            Log(message, Severity.Information);
        }

        public void Log(string message, Severity sev)
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument.Editor;
            ed.WriteMessage(message);
        }
    }
}
