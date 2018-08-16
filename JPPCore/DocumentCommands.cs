using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace JPP.Core
{
    internal static class DocumentCommands
    {
        #region Command Methods
        /// <summary>
        /// Clean document and set to read only
        /// </summary>
        [CommandMethod("Finalise", CommandFlags.Session)]
        public static void Finalise()
        {
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            acDoc.Finalise();
        }

        /// <summary>
        /// Clean docjument and set to readonly
        /// </summary>
        /// <param name="document">Document to be finalised</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void Finalise(this Document document)
        {
            using (DocumentLock dl = document.LockDocument())
            {
                using (Transaction tr = document.Database.TransactionManager.StartTransaction())
                {
                    //Run the cleanup commands
                    document.Purge();
                    document.Database.Audit(true, false);
                }
            }

            string path = document.Database.Filename;

            document.Database.SaveAs(path, DwgVersion.Current);
            document.CloseAndDiscard();

            FileInfo fi = new FileInfo(path)
            {
                IsReadOnly = true
            };
        }

        /// <summary>
        /// Purge all unused elements from the drawing
        /// </summary>
        /// <param name="document">Document to be purged</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void Purge(this Document document)
        {
            using (Transaction tr = document.Database.TransactionManager.StartTransaction())
            {
                bool toBePurged = true;

                while (toBePurged)
                {
                    // Create the list of objects to "purge"
                    ObjectIdCollection collection = new ObjectIdCollection();

                    LayerTable lt = tr.GetObject(document.Database.LayerTableId, OpenMode.ForRead) as LayerTable;
                    foreach (ObjectId layer in lt)
                    {
                        collection.Add(layer);
                    }

                    LinetypeTable ltt = tr.GetObject(document.Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
                    foreach (ObjectId linetype in ltt)
                    {
                        collection.Add(linetype);
                    }

                    TextStyleTable tst = tr.GetObject(document.Database.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
                    foreach (ObjectId text in tst)
                    {
                        collection.Add(text);
                    }

                    BlockTable bt = tr.GetObject(document.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                    foreach (ObjectId block in bt)
                    {
                        collection.Add(block);
                    }

                    DBDictionary tsd = tr.GetObject(document.Database.TableStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
                    foreach (DBDictionaryEntry ts in tsd)
                    {
                        collection.Add(ts.Value);
                    }

                    // Call the Purge function to filter the list
                    document.Database.Purge(collection);

                    if (collection.Count > 0)
                    {
                        // Erase each of the objects we've been allowed to
                        foreach (ObjectId id in collection)
                        {
                            DBObject obj = tr.GetObject(id, OpenMode.ForWrite);
                            obj.Erase();
                        }
                    }
                    else
                    {
                        toBePurged = false;
                    }
                }

                tr.Commit();
            }
        }

        #endregion
    }
}