using MailKit.Net.Pop3;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace parser4mails
{
    /// <summary>
    /// Interaction logic for mailwindow.xaml
    /// </summary>
    /// 

    public partial class Emailclass
    {
        [JsonProperty("hits")]
        public Hits Hits { get; set; }
    }

    public partial class Hits
    {
        [JsonProperty("total")]
        public int Total { get; set; }
        [JsonProperty("hits")]
        public Hit[] HitsHits { get; set; }
    }

    public partial class Hit
    {
        [JsonProperty("_id")]
        public string Id { get; set; }
        [JsonProperty("_source")]
        public Source Source { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("optin")]
        public string Optin { get; set; }
    }
    public partial class Source
    {
        [JsonProperty("optout")]
        public string Optout { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("affiliatelink")]
        public string Affiliatelink { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("publish")]
        public string Publish { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("duration")]
        public string Duration { get; set; }
    }

    

    public partial class mailwindow : Window
    {

        public string MyTextData { get; set; }
        public string subject_p = "";
        public string excerpt_p = "";
        public string messageId_p = "";
        public string addrfrom_p = "";
        public string uid_p = "";
        public string enddate_p = "";
        const string hostelastic = "http://172.17.1.88:9200";

        public mailwindow(string subject, string excerpt, string messageId, string addrfrom2, string white_email, string white_optin, string white_optout, string white_affiliate, string uID, string enddate, string white_duration)
        {
            InitializeComponent();
            
            zadeva_label.Content = subject;
            string potdomaila = "C:/inetpub/wwwroot/App_Data/pages/";
            string emaillink = potdomaila + messageId + ".html";
            subject_p = subject;
            excerpt_p = excerpt;
            messageId_p = messageId;
            addrfrom_p = addrfrom2;
            uid_p = uID;
            enddate_p = enddate;
            preview_browser.Source = new Uri(emaillink);

            if (white_email == addrfrom2)
            {
                email_tbox.Background = Brushes.LightGreen;
                email_tbox.IsReadOnly = true;
                email_btn.Visibility = Visibility.Hidden;
                optin_label.Visibility = Visibility.Visible;
                optout_label.Visibility = Visibility.Visible;
                affiliate_label.Visibility = Visibility.Visible;
                optin_tbox.Visibility = Visibility.Visible;
                optout_tbox.Visibility = Visibility.Visible;
                affiliate_tbox.Visibility = Visibility.Visible;
                optin_btn.Visibility = Visibility.Visible;
                optout_btn.Visibility = Visibility.Visible;
                affiliate_btn.Visibility = Visibility.Visible;
                objavi_btn.Visibility = Visibility.Visible;
                duration_label.Visibility = Visibility.Visible;
                duration_tbox.Visibility = Visibility.Visible;
                duration_btn.Visibility = Visibility.Visible;
            }

            email_tbox.Text = addrfrom2;
            optin_tbox.Text = white_optin;
            optout_tbox.Text = white_optout;
            affiliate_tbox.Text = white_affiliate;
            duration_tbox.Text = white_duration;
            if (Convert.ToDouble(white_duration) > 0)
                enddate_p = DateTime.Today.AddDays(Convert.ToDouble(white_duration)).ToString("yyyy-MM-dd");
            richtb.AppendText(excerpt);
        }

        public void Email_btn_Click(object sender, RoutedEventArgs e)
        {
            //update email on elasticsearch whitelist
            email_tbox.Background = Brushes.LightGreen;
            email_tbox.IsReadOnly = true;
            email_btn.Visibility = Visibility.Hidden;
            optin_label.Visibility = Visibility.Visible;
            optout_label.Visibility = Visibility.Visible;
            affiliate_label.Visibility = Visibility.Visible;
            optin_tbox.Visibility = Visibility.Visible;
            optout_tbox.Visibility = Visibility.Visible;
            affiliate_tbox.Visibility = Visibility.Visible;
            optin_btn.Visibility = Visibility.Visible;
            optout_btn.Visibility = Visibility.Visible;
            affiliate_btn.Visibility = Visibility.Visible;
            objavi_btn.Visibility = Visibility.Visible;
            duration_label.Visibility = Visibility.Visible;
            duration_tbox.Visibility = Visibility.Visible;
            duration_btn.Visibility = Visibility.Visible;

            string mailelastic = "";
            string json = "{\"email\": \"" + email_tbox.Text + "\", \"publish\": \"" + "false" + "\"}";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                wc.UploadString(hostelastic + "/whitelist/_doc", json);
            }
            catch
            {
                mailelastic = "Error";
            }
        }

        private void Optin_btn_Click(object sender, RoutedEventArgs e)
        {
            //update opt in on elasticsearch whitelist
            string index = "";
            string mailelastic = "";
            string json = "{\"query\": {\"term\": {\"email\": \"" + email_tbox.Text + "\"}}}";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                mailelastic = wc.UploadString(hostelastic + "/whitelist/_search?", json);
                var emailclass = new Emailclass();
                emailclass = JsonConvert.DeserializeObject<Emailclass>(mailelastic);
                index = emailclass.Hits.HitsHits[0].Id;
            }
            catch
            {
                mailelastic = "Error";
            }

            string mailelastic2 = "";
            string json2 = "{ \"doc\" : { \"optin\" : \"" + optin_tbox.Text + "\" } }";
            WebClient wc2 = new WebClient();
            wc2.Encoding = Encoding.UTF8;
            wc2.Headers.Add("Content-Type", "application/json");
            string urlupdate = hostelastic + "/whitelist/_doc/" + index + "/_update";

            try
            {
                mailelastic2 = wc2.UploadString(urlupdate, json2); // whitelist/_doc/GnRyDWsBFWWaSa5YP7VY/_update
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Optout_btn_Click(object sender, RoutedEventArgs e)
        {
            //update opt out on elasticsearch whitelist
            string index = "";
            string mailelastic = "";
            string json = "{\"query\": {\"term\": {\"email\": \"" + email_tbox.Text + "\"}}}";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                mailelastic = wc.UploadString(hostelastic + "/whitelist/_search?", json);
                var emailclass = new Emailclass();
                emailclass = JsonConvert.DeserializeObject<Emailclass>(mailelastic);
                index = emailclass.Hits.HitsHits[0].Id;
            }
            catch
            {
                mailelastic = "Error";
            }

            string mailelastic2 = "";
            string json2 = "{ \"doc\" : { \"optout\" : \"" + optout_tbox.Text + "\" } }";
            WebClient wc2 = new WebClient();
            wc2.Encoding = Encoding.UTF8;
            wc2.Headers.Add("Content-Type", "application/json");
            string urlupdate = hostelastic + "/whitelist/_doc/" + index + "/_update";
            try
            {
                mailelastic2 = wc2.UploadString(urlupdate, json2); // whitelist/_doc/GnRyDWsBFWWaSa5YP7VY/_update
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Affiliate_btn_Click(object sender, RoutedEventArgs e)
        {
            //update affiliate on elasticsearch whitelist
            string index = "";
            string mailelastic = "";
            string json = "{\"query\": {\"term\": {\"email\": \"" + email_tbox.Text + "\"}}}";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                mailelastic = wc.UploadString(hostelastic + "/whitelist/_search?", json);
                var emailclass = new Emailclass();
                emailclass = JsonConvert.DeserializeObject<Emailclass>(mailelastic);
                index = emailclass.Hits.HitsHits[0].Id;
            }
            catch
            {
                mailelastic = "Error";
            }

            string mailelastic2 = "";
            string json2 = "{ \"doc\" : { \"affiliatelink\" : \"" + affiliate_tbox.Text + "\" } }";
            WebClient wc2 = new WebClient();
            wc2.Encoding = Encoding.UTF8;
            wc2.Headers.Add("Content-Type", "application/json");
            string urlupdate = hostelastic + "/whitelist/_doc/" + index + "/_update";
            try
            {
                mailelastic2 = wc2.UploadString(urlupdate, json2); // whitelist/_doc/GnRyDWsBFWWaSa5YP7VY/_update
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void Objavi_btn_Click(object sender, RoutedEventArgs e)
        {
            Optin_btn_Click(sender, e);
            Optout_btn_Click(sender, e);
            Affiliate_btn_Click(sender, e);
            Duration_btn_Click(sender, e);


            //update publish on elasticsearch whitelist
            string index = "";
            string mailelastic = "";
            string json = "{\"query\": {\"term\": {\"email\": \"" + email_tbox.Text + "\"}}}";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                mailelastic = wc.UploadString(hostelastic + "/whitelist/_search?", json);
                var emailclass = new Emailclass();
                emailclass = JsonConvert.DeserializeObject<Emailclass>(mailelastic);
                index = emailclass.Hits.HitsHits[0].Id;
            }
            catch
            {
                mailelastic = "Error";
            }

            string mailelastic2 = "";
            string json2 = "{ \"doc\" : { \"publish\" : \"" + "true" + "\" } }";
            WebClient wc2 = new WebClient();
            wc2.Encoding = Encoding.UTF8;
            wc2.Headers.Add("Content-Type", "application/json");
            string urlupdate = hostelastic + "/whitelist/_doc/" + index + "/_update";
            try
            {
                mailelastic2 = wc2.UploadString(urlupdate, json2); // whitelist/_doc/GnRyDWsBFWWaSa5YP7VY/_update
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            string todaysdate = DateTime.Today.ToString("yyyy-MM-dd");
            if (duration_tbox.Text != "")
                enddate_p = DateTime.Today.AddDays(Convert.ToDouble(duration_tbox.Text)).ToString("yyyy-MM-dd");

            //add mail to elasticsearch
            string jsonbody = "{ \"subject\" : \"" + subject_p + "\", \"addrfrom\" : \"" + addrfrom_p + "\", \"excerpt\" : \"" + excerpt_p + "\", \"score\" : \"0.0\", \"messageid\" : \"" + messageId_p + "\", \"preview\" : \"!!!preview!!!\", \"campaignname\" : \"Campaign name\", \"descriptionofcampaign\" : \"Description of campaign\", \"publisher\" : \"publisher1\", \"fieldofinterest\" : \"News\", \"region\" : \"Europe\", \"contenttype\" : \"Newsletter\", \"optin\" : \"" + optin_tbox.Text + "\", \"optout\" : \"" + optout_tbox.Text + "\", \"affiliatelink\" : \"" + affiliate_tbox.Text + "\", \"enddate\" : \"" + enddate_p + "\", \"date\" : \"" + todaysdate + "\" } ";

            //string jsonbody = "{ \"subject\" : \"" + subject_p + "\", \"addrfrom\" : \"" + addrfrom_p + "\", \"excerpt\" : \"" + excerpt_p + "\", \"score\" : \"0.0\", \"messageid\" : \"" + messageId_p + "\", \"preview\" : \"!!!preview!!!\", \"campaignname\" : \"Campaign name\", \"descriptionodcampaign\" : \"Description of campaign\", \"publisher\" : \"publisher1\", \"fieldofinterest\" : \"News\", \"region\" : \"Europe\", \"contenttype\" : \"Newsletter\", \"optin\" : \"" + optin_tbox.Text + "\", \"optout\" : \"" + optout_tbox.Text + "\", \"affiliatelink\" : \"" + affiliate_tbox.Text + "\"}";
            WebClient wc4 = new WebClient();
            wc4.Encoding = Encoding.UTF8;
            wc4.Headers.Add("Content-Type", "application/json");
            try
            {
                wc4.UploadString(hostelastic + "/emmares_search_test/_doc", jsonbody);
                //delete from pop
                DeleteMessageByUID(uid_p);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error on uploading to es (mail)" + ex);
            }

            this.Close();
        }

        private void DeleteMessageByUID(string messageId)
        {
             string hostName = "172.17.1.41";
             int port = 110;
             bool useSsl = false;
             string userName = "publish@emmares";
             string password = "publish123!";
            using (var client = new Pop3Client())
            {
                client.Connect(hostName, port, useSsl);
                client.Authenticate(userName, password);
                for (int i = 0; i < client.Count; i++)
                {
                    // If the Message ID of the current message is the same as the parameter given, delete that message
                    if (client.GetMessageUid(i) == messageId)
                    {
                        // Delete
                        client.DeleteMessage(i);
                    }
                }
                client.Disconnect(true);
            }

        }


        public void Izbrisi_btn_Click(object sender, RoutedEventArgs e)
        {
            //Delete email
            DeleteMessageByUID(uid_p);
            this.Close();
        }

        private void Blacklist_btn_Click_1(object sender, RoutedEventArgs e)
        {
            //update blacklist
            string mailelastic = "";
            string json = "{\"email\": \"" + email_tbox.Text + "\"}";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                mailelastic = wc.UploadString(hostelastic + "/blacklist/_doc", json);
                this.Close();
            }
            catch
            {
                mailelastic = "Error";
            }

        }

        private void Duration_btn_Click(object sender, RoutedEventArgs e)
        {
            //update duration in days on elasticsearch whitelist
            string index = "";
            string mailelastic = "";
            string json = "{\"query\": {\"term\": {\"email\": \"" + email_tbox.Text + "\"}}}";
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            wc.Headers.Add("Content-Type", "application/json");
            try
            {
                mailelastic = wc.UploadString(hostelastic + "/whitelist/_search?", json);
                var emailclass = new Emailclass();
                emailclass = JsonConvert.DeserializeObject<Emailclass>(mailelastic);
                index = emailclass.Hits.HitsHits[0].Id;
            }
            catch
            {
                mailelastic = "Error";
            }

            string json2 = "{ \"doc\" : { \"duration\" : \"" + duration_tbox.Text + "\" } }";
            WebClient wc2 = new WebClient();
            wc2.Encoding = Encoding.UTF8;
            wc2.Headers.Add("Content-Type", "application/json");
            string urlupdate = hostelastic + "/whitelist/_doc/" + index + "/_update";
            try
            {
                wc2.UploadString(urlupdate, json2); // whitelist/_doc/GnRyDWsBFWWaSa5YP7VY/_update
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
