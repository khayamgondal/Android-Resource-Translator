using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Android_Resource_Handlers
{
    public class Translator
    {
        private AdmAuthentication _auth;
        public Translator()
        {
            try
            {
                _auth = new AdmAuthentication(ConfigurationManager.AppSettings["ClientID"], ConfigurationManager.AppSettings["ClientSecret"]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public string Translate(string text, string from, string to)
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
                    Console.WriteLine("Translation for source text '{0}' from {1} to {2} is {3}", text, from, to, translation);
                    return translation;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Internet Problem "+ex.Message);
                return String.Empty;
            }
        }
    }
}
