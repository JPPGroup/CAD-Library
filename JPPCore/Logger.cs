﻿using Autodesk.AutoCAD.EditorInput;

namespace JPP.Core
{
    public static class Logger
    {
        public static void Log(string Message)
        {
            Log(Message, Severity.Information);
        }

        public static void Log(string Message, Severity sev)
        {
            Editor ed = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.CurrentDocument.Editor;
            ed.WriteMessage(Message);
        }

        public enum Severity
        {
            Debug,
            Information,
            Warning,
            Error,
            Crash
        }
    }
}
