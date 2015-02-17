using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
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

namespace Android_Resource_Handlers
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AdmAuthentication _auth;
        private List<Language> _langs = new List<Language>();

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _auth = new AdmAuthentication(ConfigurationManager.AppSettings["ClientID"], ConfigurationManager.AppSettings["ClientSecret"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            FillLanguageLists();
        }

        

        private string Translate(string text, string from, string to)
        {
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + System.Web.HttpUtility.UrlEncode(text) + "&from=" + from + "&to=" + to;
            string authToken = "Bearer" + " " + _auth.GetAccessToken().access_token;
            Console.WriteLine(uri);
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
            httpWebRequest.Headers.Add("Authorization", authToken);

            WebResponse response = null;
            try
            {
                response = httpWebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String"));
                    string translation = (string)dcs.ReadObject(stream);
                    Console.WriteLine("Translation for source text '{0}' from {1} to {2} is", text, from, to);
                    return translation;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return String.Empty;
            }
        }

        private void B_Translate_Click(object sender, RoutedEventArgs e)
        {
            if (CB_From.SelectedIndex != -1 && CB_To.SelectedIndex != -1)
            {
                string from = (CB_From.SelectedItem as Language).Code;
                string to = (CB_To.SelectedItem as Language).Code;
                TBk_Output.Text = Translate(TBx_Input.Text, from, to);
            }
            else
            {
                MessageBox.Show("Please select the languages!");
            }
        }

        private void FillLanguageLists()
        {
            string text = ReadEmbeddedResourceTextFile("Android_Resource_Handlers.languages.txt");
            List<string> lines = new List<string>(text.Split(new string[] { "\r\n" }, StringSplitOptions.None));
            foreach (string line in lines)
            {
                string[] data = line.Split('\t');
                _langs.Add(new Language(data[0].Trim(), data[1].Trim()));
            }

            CB_From.ItemsSource = _langs;
            CB_To.ItemsSource = _langs;
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
    }
}
