using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Navigation;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using ClosedXML.Excel;
using JPP.Core;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: CommandClass(typeof(ProjectManager))]

namespace JPP.Core
{
    public class ProjectManager
    {
        public static ProjectManager Current
        {
            get { return _Current; }
        }

        private static ProjectManager _Current;

        private Dictionary<string, Project> Projects;

        public ProjectManager() : this(true)
        {
        }
        
        public ProjectManager(bool registerEvents)
        {
            _Current = this;
            Projects = new Dictionary<string, Project>();

            if (registerEvents)
            {
                RegisterEvents();
            }
        }

        private void RegisterEvents()
        {
            //TODO: Verify that the events dont fire multiple times
            Application.DocumentManager.DocumentBecameCurrent += (sender, args) =>
            {
                Document acDoc = Application.DocumentManager.MdiActiveDocument;
                if (acDoc == null) return;
                string databaseName = acDoc.Database.Filename;

                /*acDoc.LayoutSwitched += (sender1, args1) =>
                {
                    VerifyTitleBlock();
                };*/

                //IdentifyProject(databaseName);
            };
        }

        [CommandMethod("M_VerifyLayouts")]
        public static void VerifyLayouts()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            string currentDrawing = ProjectManager.Current.IdentifyProject(db.Filename);
            if (!ProjectManager.Current.Projects.ContainsKey(currentDrawing))
            {

            }

            Project current = ProjectManager.Current.Projects[currentDrawing];

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;

                foreach (ObjectId id in bt)
                {
                    BlockTableRecord btr;
                    btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;

                    // if this is a layout
                    if (btr.IsLayout)
                    {
                        ObjectId lid = btr.LayoutId;
                        Layout lt = tr.GetObject(lid, OpenMode.ForWrite) as Layout;
                        current.VerifyTitleBlock(lt);
                    }
                }

                tr.Commit();
            }
        }

        public string IdentifyProject(string Path)
        {
            //Remove the actual file and strip back
            string[] parts = Path.Split('\\');


            int offset = 2;

            while (parts.Length - offset > 0)
            {
                string id = parts[parts.Length - offset];
                string[] subparts = id.Split(' ');
                string jobNumber = subparts[0];

                //Check fits the job code pattern
                if (Regex.IsMatch(jobNumber, @"\d+[A-Za-z]"))
                {
                    return jobNumber;
                }
                else
                {
                    offset++;
                }
            }

            return null;
        }
    }
}
