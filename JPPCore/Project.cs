using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using ClosedXML.Excel;

namespace JPP.Core
{
    class Project
    {
        public string ProjectID { get; set; }
        public string ProjectName { get; set; }
        public string Client { get; set; }
        public string Root;

        public void IdentifyProject(string Path)
        {
            //Remove the actual file and strip back
            string[] parts = Path.Split('\\');


            bool found = false;
            int offset = 2;

            while (!found && parts.Length - offset > 0)
            {
                string id = parts[parts.Length - offset];
                string[] subparts = id.Split(' ');
                string jobNumber = subparts[0];

                //Check fits the job code pattern
                if (Regex.IsMatch(jobNumber, @"\d+[A-Za-z]"))
                {
                    found = true;
                    ProjectID = jobNumber;
                }
                else
                {
                    offset++;
                }
            }

            if (!found) return;

            //Review project sheet 
            var tfs = new FileStream(Properties.Settings.Default.ProjectWorkbook, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            FileStream tempFile = new FileStream("temp.xlsx", FileMode.Create);

            tfs.CopyTo(tempFile);
            tfs.Dispose();
            UriFixer.FixInvalidUri(tempFile, s => new Uri("http://broken-link/"));

            using (var readonlytemp = new FileStream("temp.xlsx", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var workbook = new XLWorkbook(readonlytemp);
                var sheet = workbook.Worksheet("Schemes");

                foreach (IXLRow row in sheet.Rows())
                {
                    string contentsProject = row.Cell(2).GetString();
                    string contentsCostCentre = row.Cell(4).GetString();
                    string ProjectNumber = ProjectID.Substring(0, ProjectID.Length - 1);
                    string CostCentre = ProjectID.Substring(ProjectID.Length - 1, 1);
                    if (contentsProject.Equals(ProjectNumber) && contentsCostCentre.Equals(CostCentre))
                    {
                        ProjectName = row.Cell(6).GetString();
                        Client = row.Cell(9).GetString();
                        break;
                    }
                }
            }

            File.Delete("temp.xlsx");
        }
        
        public void VerifyTitleBlock(Layout layout)
        {
            Transaction tr = layout.Database.TransactionManager.TopTransaction;

            BlockTableRecord btr = tr.GetObject(layout.BlockTableRecordId, OpenMode.ForRead) as BlockTableRecord;

            foreach (ObjectId oid in btr)
            {
                DBObject e = tr.GetObject(oid, OpenMode.ForRead);
                if (e is BlockReference)
                {
                    BlockReference br = e as BlockReference;
                    bool isMask = false;

                    string blockName;

                    if (br.IsDynamicBlock)
                    {
                        //get the real dynamic block name.

                        blockName = (tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord).Name;
                    }
                    else
                    {
                        blockName = br.Name;
                    }
                    switch (blockName)
                    {
                        case "A0 Mask":
                            isMask = true;
                            break;

                        case "A1 Mask":
                            isMask = true;
                            break;

                        case "A2 Mask":
                            isMask = true;
                            break;

                        case "A3 Mask":
                            isMask = true;
                            break;

                        case "A4 Mask":
                            isMask = true;
                            break;

                        case "A0mask (portrait)":
                            isMask = true;
                            break;

                        case "A1mask (portrait)":
                            isMask = true;
                            break;

                        case "A2mask (portrait)":
                            isMask = true;
                            break;

                        case "A3mask (portrait)":
                            isMask = true;
                            break;

                        default:
                            isMask = false;
                            break;
                    }

                    if (isMask)
                    {
                        foreach (ObjectId arId in br.AttributeCollection)
                        {

                            DBObject obj = tr.GetObject(arId, OpenMode.ForRead);

                            AttributeReference ar = obj as AttributeReference;

                            if (ar != null)
                            {
                                if (ar.Tag.ToUpper() == "CLIENT1")
                                {
                                    ar.UpgradeOpen();
                                    ar.TextString = Client;
                                    ar.DowngradeOpen();
                                }
                                if (ar.Tag.ToUpper() == "PROJECT1")
                                {
                                    ar.UpgradeOpen();
                                    ar.TextString = ProjectName;
                                    ar.DowngradeOpen();
                                }
                                if (ar.Tag.ToUpper() == "PROJECTNO")
                                {
                                    ar.UpgradeOpen();
                                    ar.TextString = ProjectID;
                                    ar.DowngradeOpen();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
