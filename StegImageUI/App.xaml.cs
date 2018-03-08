using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using log4net;


namespace StegImageUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        // Make this unique!
        private const string _UNIQUE = "StegImageUI_MilAndSH";

        private static void createWorkingFolders()
        {
            const string FOLDERNAME = "StegImageUI";
            string DOCSFOLDERNAME = "Docs";
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appMainFolder = Path.Combine(docPath, FOLDERNAME);
            string appDocFolder = Path.Combine(appMainFolder, DOCSFOLDERNAME);
            // Create main folder ...Documents\StegimageUI
            bool exists = Directory.Exists(appMainFolder);
            if (!exists)
                System.IO.Directory.CreateDirectory(appMainFolder);
            // Create docs folder ...Documents\StegimageUI\Docs - where extracted docs are saved
            exists = Directory.Exists(appDocFolder);
            if (!exists)
                System.IO.Directory.CreateDirectory(appDocFolder);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // Bring window to foreground
            if (this.MainWindow.WindowState == WindowState.Minimized)
            {
                this.MainWindow.WindowState = WindowState.Normal;
            }

            this.MainWindow.Activate();

            return true;
        }

        [STAThread]
        public static void Main(string[] args)
        {

            if (SingleInstance<App>.InitializeAsFirstInstance(_UNIQUE))
            {
                // Createapp working folders if needed
                createWorkingFolders();

                var application = new App();
                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

    }
}
