using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.Windows;
using Autodesk.Civil.ApplicationServices;
using Autodesk.Windows;
using JPP.Core;
using JPP.Core.Properties;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using Exception = System.Exception;
using Orientation = System.Windows.Controls.Orientation;
using StatusBar = Autodesk.AutoCAD.Windows.StatusBar;

// ReSharper disable MemberCanBePrivate.Global

[assembly: ExtensionApplication(typeof(CoreMain))]
[assembly: CommandClass(typeof(CoreMain))]

namespace JPP.Core
{
    /// <summary>
    /// Loader class, the main entry point for the full application suite. Implements IExtensionApplication is it
    /// automatically initialised and terminated by AutoCad.
    /// </summary>
    public class CoreMain : IExtensionApplication
    {
        #region Public Variables

        /// <summary>
        /// Returns true if currently running under the Core Console
        /// </summary>
        public static bool CoreConsole
        {
            get
            {
                if (_coreConsole != null) return _coreConsole.Value;
                try
                {
                    StatusBar unused = Application.StatusBar;
                    _coreConsole = true;
                }
                catch (Exception)
                {
                    _coreConsole = false;
                }

                return _coreConsole.Value;
            }
        }

        /// <summary>
        /// Returns true if currently running under Civil 3D
        /// </summary>
        public static bool Civil3D
        {
            get
            {
                if (_civil3D != null) return _civil3D.Value;
                try
                {
                    //StatusBar testBar = Autodesk.AutoCAD.ApplicationServices.Application.StatusBar;
                    CivilDocument unused = CivilApplication.ActiveDocument;
                    _civil3D = true;
                }
                catch (Exception)
                {
                    _civil3D = false;
                }

                return _civil3D.Value;
            }
        }

        /// <summary>
        /// Current Log implementation
        /// </summary>
        public static ILogger Log { get; private set; }

        #endregion

        #region Private variables

        /// <summary>
        /// PaletteSet containing the settings window
        /// </summary>
        private static PaletteSet _settingsWindow;

        /// <summary>
        /// Ribon toggle button for displaying settings window
        /// </summary>
        private static RibbonToggleButton _settingsButton;

        /// <summary>
        /// Keep a reference to handler to prevent GC
        /// </summary>
        // ReSharper disable once NotAccessedField.Local
        private static ClickOverride _clickOverride;

        /// <summary>
        /// Is software running under the core console? Null when this has not yet been checked
        /// </summary>
        private static bool? _coreConsole;

        /// <summary>
        /// Is software running under civil 3d? Null when this has not yet been checked
        /// </summary>
        private static bool? _civil3D;

        #endregion

        #region Autocad Extension Lifecycle

        /// <summary>
        /// Implement the Autocad extension api to load the additional libraries we need. Main library entry point
        /// </summary>
        public void Initialize()
        {
            //Upgrade and load the app settings
            //TODO: Verify this is actually required
            Settings.Default.Upgrade();

            //If not running in console only, detect if ribbon is currently loaded, and if not wait until the application is Idle.
            //Throws an error if try to add to the menu with the ribbon unloaded
            if (CoreConsole)
                InitExtension();
            else
            {
                if (ComponentManager.Ribbon == null)
                    Autodesk.AutoCAD.ApplicationServices.Core.Application.Idle += Application_Idle;
                else
                {
                    //Ribbon existis, call the initialize method directly
                    InitExtension();
                }
            }
        }

        /// <summary>
        /// Implement the Autocad extension api to terminate the application
        /// </summary>
        public void Terminate()
        {
        }

        /// <summary>
        /// Event handler to detect when the program is fully loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Application_Idle(object sender, EventArgs e)
        {
            //Unhook the event handler to prevent multiple calls
            Autodesk.AutoCAD.ApplicationServices.Core.Application.Idle -= Application_Idle;
            //Call the initialize method now the application is loaded
            InitExtension();
        }

        #endregion

        #region Extension Setup

        /// <summary>
        /// Init JPP command loads all essential elements of the program, including the helper DLL files.
        /// </summary>
        public static void InitExtension()
        {
            //TODO: Add code here for choosing log type
            Log = new Logger();

            Log.Entry(Resources.Inform_LoadingMain);
            
            if(!CoreConsole)
                CreateUI();

            //Load the additional DLL files, but only not if running in debug mode
            #if !DEBUG
            Update();
            #endif
            LoadModules();

            Log.Entry(Resources.Inform_LoadedMain);
        }

        public static void CreateUI()
        {
            //Create the main UI
            RibbonTab jppTab = CreateTab();
            CreateCoreMenu(jppTab);

            //Create settings window
            //TODO: move common window creation code to utilities method
            _settingsWindow = new PaletteSet("JPP", new Guid("9dc86012-b4b2-49dd-81e2-ba3f84fdf7e3"))
            {
                Size = new Size(600, 800),
                Style = (PaletteSetStyles)((int)PaletteSetStyles.ShowAutoHideButton +
                                           (int)PaletteSetStyles.ShowCloseButton),
                DockEnabled = (DockSides)((int)DockSides.Left + (int)DockSides.Right)
            };

            ElementHost settingsWindowHost = new ElementHost();
            settingsWindowHost.AutoSize = true;
            settingsWindowHost.Dock = DockStyle.Fill;
            settingsWindowHost.Child = new SettingsUserControl();
            _settingsWindow.Add("Settings", settingsWindowHost);
            _settingsWindow.KeepFocus = false;
            
            //Check for registry key for autoload
            if (!RegistryHelper.IsAutoload())
            {
                //No autoload found
                //TODO: try to condense this into a helper method
                TaskDialog autoloadPrompt = new TaskDialog();
                autoloadPrompt.WindowTitle = Resources.Core_FriendlyName;
                autoloadPrompt.MainInstruction = Resources.Core_Autoload_QueryEnable;
                autoloadPrompt.MainIcon = TaskDialogIcon.Information;
                autoloadPrompt.FooterText = Resources.Core_Autoload_Warn;
                autoloadPrompt.FooterIcon = TaskDialogIcon.Warning;
                autoloadPrompt.Buttons.Add(new TaskDialogButton(0, Resources.Core_Autoload_Skip));
                autoloadPrompt.Buttons.Add(new TaskDialogButton(1, Resources.Core_Autoload_Enable));
                autoloadPrompt.DefaultButton = 0;
                autoloadPrompt.Callback = delegate (ActiveTaskDialog atd, TaskDialogCallbackArgs e, object sender)
                {
                    if (e.Notification != TaskDialogNotification.ButtonClicked) return false;
                    if (e.ButtonId == 1)
                    {
                        //TODO: Disable when registry is ok
                        //RegistryHelper.CreateAutoload();
                        Autodesk.AutoCAD.ApplicationServices.Core.Application.ShowAlertDialog(
                            "Autload creation currently disabled.");
                    }

                    return false;
                };
                autoloadPrompt.Show(Autodesk.AutoCAD.ApplicationServices.Core.Application.MainWindow.Handle);
            }

            //Load click handler;
            _clickOverride = ClickOverride.Current;
        }

        /// <summary>
        /// Creates the JPP tab and adds it to the ribbon
        /// </summary>
        /// <returns>The created tab</returns>
        public static RibbonTab CreateTab()
        {
            RibbonControl rc = ComponentManager.Ribbon;
            RibbonTab jppTab = new RibbonTab();

            //Pull names from constant file as used in all subsequent DLL's
            jppTab.Name = Constants.JPP_TAB_TITLE;
            jppTab.Title = Constants.JPP_TAB_TITLE;
            jppTab.Id = Constants.JPP_TAB_ID;

            rc.Tabs.Add(jppTab);
            return jppTab;
        }

        /// <summary>
        /// Add the core elements of the ui
        /// </summary>
        /// <param name="jppTab">The tab to add the ui elements to</param>
        public static void CreateCoreMenu(RibbonTab jppTab)
        {
            RibbonPanel panel = new RibbonPanel();
            RibbonPanelSource source = new RibbonPanelSource {Title = "General"};
            RibbonRowPanel stack = new RibbonRowPanel();

            /*RibbonButton finaliseButton = Utilities.CreateButton("Finalise Drawing", Properties.Resources.package, RibbonItemSize.Standard, "Finalise");
            stack.Items.Add(finaliseButton);
            stack.Items.Add(new RibbonRowBreak());*/

            /*RibbonButton authenticateButton = Utilities.CreateButton("Authenticate", Properties.Resources.Locked, RibbonItemSize.Standard, "");
            stack.Items.Add(authenticateButton);
            stack.Items.Add(new RibbonRowBreak());*/

            //Create the button used to toggle the settings on or off
            _settingsButton = new RibbonToggleButton
            {
                ShowText = true,
                ShowImage = true,
                Text = Resources.Core_Menu_SettingsText,
                Name = Resources.Core_Menu_SettingsName,
                Size = RibbonItemSize.Standard,
                Orientation = Orientation.Horizontal,
                Image = Utilities.LoadImage(Resources.settings)
            };
            _settingsButton.CheckStateChanged += settingsButton_CheckStateChanged;
            
            stack.Items.Add(_settingsButton);
            stack.Items.Add(new RibbonRowBreak());

            //Add the new tab section to the main tab
            source.Items.Add(stack);
            panel.Source = source;
            jppTab.Panels.Add(panel);
        }

        private static void settingsButton_CheckStateChanged(object sender, EventArgs e)
        {
            _settingsWindow.Visible = _settingsButton.CheckState == true;
        }

        #endregion

        #region Updater

        /// <summary>
        /// Find all assemblies in the subdirectory, and load them into memory
        /// </summary>
        public static void LoadModules()
        {
            #if DEBUG
            string path = Assembly.GetExecutingAssembly().Location;
            #else
            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library";
            #endif

            //Check if authenticated, otherwise block the auto loading
            if (Authentication.Current.Authenticated())
            {
                //Iterate over every dll found in bin folder
                if (Directory.Exists(path))
                {
                    foreach (string dll in Directory.GetFiles(path, "*.dll"))
                    {
                        //Load the additional libraries found
                        ExtensionLoader.Load(dll);
                    }
                }
                else
                {
                    Log.Entry(Resources.Error_ModuleDirectoryMissing, Severity.Error);
                }
            }
            else
            {
                Log.Entry(Resources.Error_ModuleLoadFailedAuthentication, Severity.Error);
            }
        }

        //TODO: Trigger update method somehow
        public static void Update()
        {
            bool updateRequired = false;
            bool installUpdateRequired = false;
            string installerPath = "";

            string root = Properties.Settings.Default.UpdateLocation;

            //Get manifest file from known location
            if (File.Exists(root + "manifest.txt"))
            {
                string archivePath;
                using (TextReader tr = File.OpenText(root + "manifest.txt"))
                {
                    //Currently manifest file contains version of zip file to pull data from
                    archivePath = Properties.Settings.Default.ArchivePath + tr.ReadLine() + ".zip";
                    if (tr.Peek() != -1) installerPath = root + tr.ReadLine() + ".exe";
                }

                if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library\\manifest.txt"))
                {
                    using (TextReader tr = File.OpenText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library\\manifest.txt"))
                    {
                        //Currently manifest file contians version of zip file to pull data from
                        if (archivePath != Properties.Settings.Default + tr.ReadLine() + ".zip") updateRequired = true;
                        if (tr.Peek() != -1)
                        {
                            if (installerPath != root + tr.ReadLine() + ".exe")
                                installUpdateRequired = true;
                        }
                    }
                }
                else
                {
                    updateRequired = true;
                    installUpdateRequired = true;
                }

                //Get the current version for comparison
                using (StreamWriter sw = new StreamWriter(File.Open(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library\\manifest.txt", FileMode.Create)))
                {
                    //Download the latest resources update
                    try
                    {
                        if (updateRequired)
                        {
                            string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\JPP Consulting\\JPP AutoCad Library";
                            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                            using (ZipArchive archive = ZipFile.OpenRead(archivePath))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    entry.ExtractToFile(Path.Combine(path, entry.FullName), true);
                                }
                            }

                            sw.WriteLine(archivePath);
                        }

                        //if there is a new installer...
                        if (installerPath != "" && installUpdateRequired)
                        {
                            TaskDialog autoloadPrompt = new TaskDialog();
                            autoloadPrompt.WindowTitle = Resources.Core_FriendlyName;
                            autoloadPrompt.MainInstruction = Resources.Core_Installer_NewVersion;
                            autoloadPrompt.MainIcon = TaskDialogIcon.Information;
                            autoloadPrompt.Buttons.Add(new TaskDialogButton(0, Resources.Core_Installer_ExitAndInstall));
                            autoloadPrompt.Buttons.Add(new TaskDialogButton(1, Resources.Core_Installer_SkipInstall));
                            autoloadPrompt.DefaultButton = 0;
                            autoloadPrompt.Callback = delegate(ActiveTaskDialog atd, TaskDialogCallbackArgs e,
                                object sender)
                            {
                                if (e.Notification == TaskDialogNotification.ButtonClicked)
                                {
                                    if (e.ButtonId == 0)
                                    {
                                        Process.Start(installerPath);
                                        Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager
                                            .MdiActiveDocument.SendStringToExecute("quit ", true, false, true);
                                    }
                                }

                                return false;
                            };
                            autoloadPrompt.Show(Autodesk.AutoCAD.ApplicationServices.Core.Application.MainWindow
                                .Handle);

                            sw.WriteLine(installerPath);
                        }
                    }
                    catch (Exception)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        #endregion
    }
}