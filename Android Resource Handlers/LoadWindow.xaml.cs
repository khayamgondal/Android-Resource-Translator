using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace Android_Resource_Handlers
{
    /// <summary>
    /// Interaction logic for LoadWindow.xaml
    /// </summary>
    public partial class LoadWindow : Window
    {
        private Translator translator;
        private List<Language> _langs = new List<Language>();
        private BackgroundWorker worker;
        private string from;
        private string stringtext;
        private bool check;
        private IList sel;
        private string stringoutfolder;

        public LoadWindow()
        {
            InitializeComponent();
            FillLanguageLists();
            translator = new Translator();
            TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            ClientID.Text = ConfigurationManager.AppSettings["ClientID"];
            ClientSecret.Text = ConfigurationManager.AppSettings["ClientSecret"];
            worker = new BackgroundWorker();
            worker.DoWork += worker_DoWork;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += worker_ProgressChanged;
            this.Closing += new System.ComponentModel.CancelEventHandler(LoadWindow_Closing);

        }

        void LoadWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Configuration oConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                oConfig.AppSettings.Settings["ClientSecret"].Value = ClientSecret.Text;
                oConfig.AppSettings.Settings["ClientID"].Value = ClientID.Text;
                oConfig.Save(ConfigurationSaveMode.Full);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception exp)
            {

                MessageBox.Show(exp.Message);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            TaskbarItemInfo.ProgressValue = (double)e.ProgressPercentage / 100;
            ProgressBar1.Value = e.ProgressPercentage;
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int count = 1;
            foreach (string to in sel)
            {
                string tex = "Translating " + count + " of " + sel.Count + "   From " + from + "  to  " + to;
                worker.ReportProgress(count * 100 / sel.Count);
                UpdateMyDelegatedelegate UpdateMyDelegate = new UpdateMyDelegatedelegate(UpdateMyDelegateLabel);
                console_output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyDelegate, tex);

                TranslateLanguage(stringtext, stringoutfolder, from, to, check);
                count++;
                console_output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyDelegate, "Done... Task is finished now!\n");
                worker.ReportProgress(0);

            }
        }

        private void FillLanguageLists()
        {
            List<string> listOfItems = new List<string>();
            string text = ReadEmbeddedResourceTextFile("Android_Resource_Handlers.languages.txt");
            List<string> lines = new List<string>(text.Split(new string[] { "\r\n" }, StringSplitOptions.None));
            foreach (string line in lines)
            {
                string[] data = line.Split('\t');
                _langs.Add(new Language(data[0].Trim(), data[1].Trim()));
                listOfItems.Add(data[0].Trim());
            }

            CB_From.ItemsSource = _langs;
            checkbox_combo.ItemsSource = listOfItems;
            //  checkbox_combo.SelectedItemsOverride = listOfItems;

        }

        private string ReadEmbeddedResourceTextFile(string filename)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = filename;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = reader.ReadToEnd();
                return result;
            }
        }
        private void button1_Click_1(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension
            dlg.DefaultExt = ".xml";
            dlg.Filter = "Android Resource Files (.xml)|*.xml";

            // Display OpenFileDialog by calling ShowDialog method
            Nullable<bool> result = dlg.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;
                FileNameTextBox.Text = filename;
                DataSet dataSet = new DataSet();
                dataSet.ReadXml(filename);
                data_grid.ItemsSource = dataSet.Tables[0].DefaultView;

            }
        }
        private delegate void UpdateMyDelegatedelegate(string i);
        private void UpdateMyDelegateLabel(string i)
        {
            console_output.Text = i;
            console_output.FontSize = 30;
        }
        private void UpdateMyDelegateLabel2(string i)
        {
            console_error.Text = i;

        }
        public void TranslateLanguage(string input, string output, string sourceLanguage, string targetLanguage, bool overrideIfExists)
        {
            Console.WriteLine("Start translating {0} -> {1}", sourceLanguage, targetLanguage);
            UpdateMyDelegatedelegate UpdateMyDelegate = new UpdateMyDelegatedelegate(UpdateMyDelegateLabel2);
            //    console_output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyDelegate, "Translating " + sourceLanguage + "  to  " + targetLanguage +" . . . .");
            try
            {
                string outputPath = System.IO.Path.Combine(output, string.Format("res/values-{0}", targetLanguage));

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                outputPath = System.IO.Path.Combine(outputPath, "strings.xml");

                Console.WriteLine("Output File: {0}.", outputPath);

                //source xml document
                XmlDocument sourceDoc = new XmlDocument();
                sourceDoc.Load(input);

                //find source resources
                XmlNode sourceResources = FindResourcesRootElement(sourceDoc);

                //destination xml document
                XmlDocument destinationDoc = null;
                XmlNode destinationResources = null;
                //optimization for quick find a node
                Dictionary<string, XmlNode> destinationNodes = new Dictionary<string, XmlNode>();
                if (File.Exists(outputPath))
                {
                    destinationDoc = new XmlDocument();
                    destinationDoc.Load(outputPath);

                    //find destination resources
                    destinationResources = FindResourcesRootElement(destinationDoc);

                    if (destinationResources == null)
                    {
                        //force null
                        destinationDoc = null;
                    }
                    else
                    {
                        for (int i = 0; i < destinationResources.ChildNodes.Count; i++)
                        {
                            XmlNode node = destinationResources.ChildNodes[i];

                            if (node.NodeType == XmlNodeType.Element)
                            {
                                string name = node.Attributes["name"].Value;
                                destinationNodes.Add(name, node);
                            }
                        }
                    }
                }

                //Display the contents of the child nodes.
                if (sourceResources != null && sourceResources.HasChildNodes)
                {
                    for (int i = 0; i < sourceResources.ChildNodes.Count; i++)
                    {
                        XmlNode node = sourceResources.ChildNodes[i];

                        if (node.NodeType == XmlNodeType.Element)
                        {
                            string name = node.Attributes["name"].Value;
                            string text = node.InnerText;

                            if (destinationDoc == null) //no destination
                            {
                                node.InnerText = translator.Translate(text, sourceLanguage, targetLanguage);
                                node.InnerText = node.InnerText.Replace("'", "\'");
                                //     BingTranslator.Translate(sourceLanguage, , text);
                            }
                            else
                            {
                                XmlNode outputNode = destinationNodes.ContainsKey(name) ? destinationNodes[name] : null;

                                if (outputNode == null || overrideIfExists)
                                {
                                    node.InnerText = translator.Translate(text, sourceLanguage, targetLanguage);
                                    node.InnerText = node.InnerText.Replace("'", "\'");

                                }
                                else
                                {
                                    node.InnerText = outputNode.InnerText;
                                    node.InnerText = node.InnerText.Replace("'", "\'");

                                }
                            }
                        }
                    }
                }

                sourceDoc.Save(outputPath);
                Console.WriteLine("Finish translating {0} -> {1}", sourceLanguage, targetLanguage);
                //   console_output.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyDelegate, "  ... Done \n");

            }
            catch (Exception ex)
            {
                console_error.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, UpdateMyDelegate, "  ERROR: " + ex.Message + "\n");

                Console.WriteLine("Error in translation from {0} -> {1}", sourceLanguage, targetLanguage);
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void translate_button_Click_1(object sender, RoutedEventArgs e)
        {
            sel = checkbox_combo.SelectedItems;

            if (CB_From.SelectedIndex != -1 && sel.Count > 0)
            {
                from = (CB_From.SelectedItem as Language).Code;
                check = check_override.IsEnabled;
                stringtext = FileNameTextBox.Text;
                stringoutfolder = folderout.Text;
                console_error.Text = "";
                console_output.Text = "";
                string fulP = System.IO.Path.GetFullPath(stringtext);
                // if (Directory.Exists(stringtext) && Directory.Exists(stringoutfolder))
                {
                    console_output.Text = "Creating your files... Dont press any button until done \n";
                    worker.RunWorkerAsync();

                }
                // else
                //    MessageBox.Show("Your input or output paths are not valid");

            }
            else
                MessageBox.Show("Select output languages");
        }

        private static XmlNode FindResourcesRootElement(XmlDocument doc)
        {
            if (doc == null)
            {
                return null;
            }

            XmlNode resources = null;

            //find resources
            for (int i = 0; i < doc.ChildNodes.Count; ++i)
            {
                XmlNode node = doc.ChildNodes[i];

                if (node.NodeType == XmlNodeType.Element && node.Name == "resources") //find resources element
                {
                    resources = node;
                    break;
                }
            }

            return resources;
        }

        private void folderout_GotFocus_1(object sender, RoutedEventArgs e)
        {
            folderout.Text = "";
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {

                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
                e.Handled = true;
            }
            catch (Exception es)
            {
                MessageBox.Show(es.Message);
            }
        }


    }
}
