/**
	* @author Martin Konečnik
	* @email martin.konecnik@gmail.com
	**/

using System;
using OpenPop.Mime;
using System.Collections.Generic;
using OpenPop.Pop3;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Topshelf;
using Topshelf.Quartz;
using Quartz;
using HtmlAgilityPack;

namespace ExtractNewsletterFromEmail
{
    public class MyService
    {
        public void Start() { }
        public void Stop() { }
    }
    public class MyJob : IJob
    {
//<<<<<<< Updated upstream
        static readonly string dbName = System.Configuration.ConfigurationManager.AppSettings["dbName"];
        static readonly string hostname = System.Configuration.ConfigurationManager.AppSettings["hostname"];
        static readonly string username = System.Configuration.ConfigurationManager.AppSettings["username"];
        static readonly string password = System.Configuration.ConfigurationManager.AppSettings["password"];
        static readonly int port = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["port"]);
        static readonly string usessl = System.Configuration.ConfigurationManager.AppSettings["usessl"];
        const int timeBetweenQueries = 30000;   // 30 seconds between queries. ... also has to be changed in main (.WithIntervalInSeconds)
       // const int port = 993;
        const bool useSsl = false;
        const string logs = "logs";
       
//=======
       /* const string dbName = "Emmares4";
        const int timeBetweenQueries = 30000;   // 30 seconds between queries.
        const string hostname = "mail.emmares.com";
        const int port = 110;
        const bool useSSL = false;
        const string username = "preview@emmares.com";
        const string password = "preview123!";*/
//>>>>>>> Stashed changes
        public void Execute(IJobExecutionContext context)
        {
            System.IO.Directory.CreateDirectory(logs);
            DebugPrint("Service started");
            QueryEmail();
        }

        /// <summary>
        /// Example showing:
        ///  - how to fetch all messages from a POP3 server
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <returns>All Messages on the POP3 server</returns>
        static Queue<Message> FetchAllMessages(string hostname, int port, bool useSsl, string username, string password)
        {
            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server
                client.Authenticate(username, password);

                // Get the number of messages in the inbox
                int messageCount = client.GetMessageCount();

                // We want to download all messages
                Queue<Message> allMessages = new Queue<Message>();

                // Messages are numbered in the interval: [1, messageCount]
                // Ergo: message numbers are 1-based.
                // Most servers give the latest message the highest number
                for (int i = messageCount; i > 0; i--)
                {
                    allMessages.Enqueue(client.GetMessage(i));
                }

                // Now return the fetched messages
                return allMessages;
            }
        }

        /// <summary>
        /// Example showing:
        ///  - how to delete a specific message from a server
        /// </summary>
        /// <param name="hostname">Hostname of the server. For example: pop3.live.com</param>
        /// <param name="port">Host port to connect to. Normally: 110 for plain POP3, 995 for SSL POP3</param>
        /// <param name="useSsl">Whether or not to use SSL to connect to server</param>
        /// <param name="username">Username of the user on the server</param>
        /// <param name="password">Password of the user on the server</param>
        /// <param name="messageNumber">
        /// The number of the message to delete.
        /// Must be in range [1, messageCount] where messageCount is the number of messages on the server.
        /// </param>
        static bool DeleteMessageByMessageId(Pop3Client client, string messageId)
        {
            // Get the number of messages on the POP3 server
            int messageCount = client.GetMessageCount();

            // Run trough each of these messages and download the headers
            for (int messageItem = messageCount; messageItem > 0; messageItem--)
            {
                // If the Message ID of the current message is the same as the parameter given, delete that message
                if (client.GetMessageHeaders(messageItem).MessageId == messageId)
                {
                    // Delete
                    client.DeleteMessage(messageItem);
                    return true;
                }
            }

            // We did not find any message with the given messageId, report this back
            return false;
        }

        static string ProcessNewsletter(ref string campaignID, string newsletter)
        {
            DebugPrint("Processing ...");
            //newsletter = System.IO.File.ReadAllText("NL_31_8.txt");
            Match match;
            if (Regex.IsMatch(newsletter, "<!--EmmaresCID=.+-->"))           // If match for IDCamp is found store it, otherwise continue as it's not a newsletter.
                match = Regex.Match(newsletter, "<!--EmmaresCID=.+-->");
            else
                return "Not newsletter";
            campaignID = match.Value.Substring(15, 36);                   // Get the actual ID
//<<<<<<< Updated upstream
            //campaignID = "9F79506F-4152-4886-CE16-08D5EC810E4F";
            Log(String.Format("Newsletter for Campaign {0} recognised.", campaignID));
//=======
//>>>>>>> Stashed changes
            DebugPrint("Removing footer ...");
            RemoveFooter(ref newsletter);                               // Removes the footer using HtmlAgilePack
            newsletter = newsletter.Replace("'", "''");                 // To prevent SQL from misinterpreting '.
            DebugPrint("Processing complete.");
            return newsletter;
        }

        static void RemoveFooter(ref string newsletter)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(newsletter);
            try
            {
                HtmlNode node = doc.DocumentNode.SelectSingleNode("//table[@class='footerWrapper']");   // Finds the table tag with class of footerWrapper.
                node.ParentNode.RemoveChild(node, false);
                newsletter = doc.DocumentNode.InnerHtml;
            }
            catch
            {
                DebugPrint("Footer node not found. Newsletter unchanged");
            }
        }

        static void QueryEmail()
        {
            DebugPrint("Querying ...");
            // As we only need to access messages sequentially, Queue is suitable.
            Queue<Message> allMessages = FetchAllMessages(hostname, port, useSsl, username, password);
            List<string> messagesToDelete = new List<string>();

            string newsletter;
            string campaignID = "empty";

            using (var conn = new SqlConnection())
            {
                conn.ConnectionString = String.Format("Data Source=172.17.1.42,1433; Database={0}; User Id=ema; Password=e3m7a1!", dbName);
                conn.Open();
                
                DebugPrint("Number of emails: " + allMessages.Count);
                /*
                newsletter = System.IO.File.ReadAllText("20180731105244.CB6B9DE0253@snd28.sndwar.com.html");
                campaignID = "d0149074-9822-4b68-3b18-08d5fb71ac35";
                SqlCommand command = new SqlCommand("UPDATE Campaigns SET Newsletter = '" + newsletter + "', HasNewsletter = 1 WHERE ID = '" + campaignID + "';", conn);
                command.ExecuteNonQuery();
                */
                foreach (Message m in allMessages)
                {
                    DebugPrint("Test");
                    messagesToDelete.Add(m.Headers.MessageId);
                    newsletter = m.FindFirstHtmlVersion()?.GetBodyAsText();      // Saves the whole email into string.
                    System.IO.File.WriteAllText(String.Format("{0}/{1}.html", logs, m.Headers.MessageId), newsletter);

                    DebugSaveNewsletterToFile(m.Headers.MessageId, newsletter);      // Only runs in debug mode.
                    //newsletter = m.FindFirstPlainTextVersion().GetBodyAsText(); // Used for testing when mailing html as plaintext.
                    Log(String.Format("Received an e-mail from {0}", m.Headers.From));
                    if (String.Equals((newsletter = ProcessNewsletter(ref campaignID, newsletter)), "Not newsletter"))  // If e-mail does not have newsletter characteristics, it's discarded.
                    {
                        Log("Discarded e-mail.");
                        DebugPrint("E-mail not recognised as a newsletter and discarded");
                        continue;
                    }
                    DebugSaveNewsletterToFile(m.Headers.MessageId + "_after", newsletter);

                    /*
                     * No idea what this code was meant to do.
                    SqlCommand command = new SqlCommand("SELECT * FROM Campaigns WHERE Newsletter IS NULL OR Newsletter = ''");
                    SqlTransaction transaction = conn.BeginTransaction();
                    */

//<<<<<<< Updated upstream
                    Log(String.Format("Executed update query for campaign with ID {0} in database {1}.", campaignID, dbName));
                    DebugPrint(String.Format("Updating campaign with ID {0} in database {1}. If campaign with said ID does not exist in the database, it will be discarded.", campaignID, dbName));
//=======
//>>>>>>> Stashed changes
                    SqlCommand command = new SqlCommand("UPDATE Campaigns SET Newsletter = '" + newsletter + "', HasNewsletter = 1 WHERE ID = '" + campaignID + "';", conn);
                    //SqlCommand command = new SqlCommand("UPDATE Campaigns SET Newsletter = '" + newsletter + "', HasNewsletter = 1 WHERE ID = '05719fb0-6d12-4537-4441-08d5ee94d972';", conn);
                    command.ExecuteNonQuery();

                    DebugPrint("Fetching just added newsletter from DB ...");
                    DebugGetNewsletterFromDatabase(conn, campaignID); // Read the newsletter from DB and save it to file.
                }
            }

            using (var client = new Pop3Client())
            {
                client.Connect(hostname, port, useSsl);
                client.Authenticate(username, password);

                foreach (string s in messagesToDelete)
                {
                    DeleteMessageByMessageId(client, s);    // Deletes all previously processed messages.
                }
            }
        }

        public static void Log(string info)
        {
            var now = DateTime.Now;
            var file = now.ToString("yyyy-MM-dd") + ".log";
            using (var sw = new System.IO.StreamWriter(String.Format("{0}/{1}", logs, file), true, System.Text.Encoding.UTF8))
            {
                sw.WriteLine(now.ToString("s") + ": " + info);
            }
        }


        // Deprecated methods
        static void DeleteMessageOnServer(string hostname, int port, bool useSsl, string username, string password, int messageNumber)
        {
            // The client disconnects from the server when being disposed
            using (Pop3Client client = new Pop3Client())
            {
                // Connect to the server
                client.Connect(hostname, port, useSsl);

                // Authenticate ourselves towards the server
                client.Authenticate(username, password);

                // Mark the message as deleted
                // Notice that it is only MARKED as deleted
                // POP3 requires you to "commit" the changes
                // which is done by sending a QUIT command to the server
                // You can also reset all marked messages, by sending a RSET command.
                client.DeleteMessage(messageNumber);

                // When a QUIT command is sent to the server, the connection between them are closed.
                // When the client is disposed, the QUIT command will be sent to the server
                // just as if you had called the Disconnect method yourself.
            }
        }
        static string RemoveFooterRegex(string newsletter) // Method removes footer and returns newsletter without it.
        {
            // Deprecated due to inconsistency. Better to do with html, mainly HtmlAgilityPack.
            try
            {
                System.IO.File.WriteAllText("C:/Users/martink/Desktop/Things/Emmares/EMMARES.MVP/readNL.html", newsletter);
                //newsletter = System.IO.File.ReadAllText("C:/Users/martink/Desktop/Things/Emmares/EMMARES.MVP/sampleNL.html");
                int startFooterIndex = newsletter.IndexOf(@"Help\s+us\s+revolutionise\s+email\s+marketing.");    // Approximate location of the area we're trying to remove.
                int endFooterIndex = newsletter.LastIndexOf("Copyright c");                             // Not the best way to identify the end, but safe of html parsing the easiest. Might be better to reimplement as html, if this doesn't work well.
                string partialFirst = newsletter.Remove(startFooterIndex);                              // Remove everything after footer's starting string.
                partialFirst = partialFirst.Remove(partialFirst.LastIndexOf(@"<table\s+style"));           // Remove the start of the table representing footer.
                string partialLast = newsletter.Remove(0, endFooterIndex);                              // Remove everything up to specified end of footer.
                partialLast = partialLast.Remove(0, partialLast.IndexOf("IDCamp") + 6);
                partialLast = partialLast.Remove(0, partialLast.IndexOf("IDCamp") + 6);                 // Index of second IDCamp in leftover, which is the last one in footer.
                partialLast = partialLast.Remove(0, partialLast.IndexOf("</table>") + 8);               // Finally finds the end of footer.
                return partialFirst + partialLast;                                                      // Glue together everything that isn't footer. Note, with proper knowledge of exactly where footer ends/starts it could all be done with one remove.
            }
            catch   // If it fails for whatever reason we return the original newsletter.
            {
                Console.WriteLine("Footer not recognised. Returned original newsletter.");
                //Console.WriteLine(newsletter);
                return newsletter;
            }
        }

        // Testing methods
        static void DisplayNode(HtmlNode node)
        {
            Console.WriteLine("Node Name: " + node.Name);

            Console.Write("\n" + node.Name + " children:\n");

            DisplayChildNodes(node);
        }
        static void DisplayChildNodes(HtmlNode nodeElement)
        {
            HtmlNodeCollection childNodes = nodeElement.ChildNodes;

            if (childNodes.Count == 0)
            {
                Console.WriteLine(nodeElement.Name + " has no children");
            }
            else
            {

                foreach (var node in childNodes)
                {
                    if (node.NodeType == HtmlNodeType.Element)
                    {
                        Console.WriteLine(node.OuterHtml);
                    }
                }
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        static void DebugPrint(string text)
        {
            Console.WriteLine(text);
        }   // Just for simple enabling/disabling of output.

        [System.Diagnostics.Conditional("DEBUG")]
        static void DebugGetNewsletterFromDatabase(SqlConnection conn, string CampaignID)
        {
            SqlCommand cmd = new SqlCommand   // Unrelated. Code to get data from Database, since application has character limit.
            {
                CommandText = "SELECT Campaigns.Newsletter FROM Campaigns WHERE Campaigns.ID = '" + CampaignID + "';",
                CommandType = System.Data.CommandType.Text,
                Connection = conn
            };
            SqlDataReader reader = cmd.ExecuteReader();
            if(reader.Read() == false)
                DebugPrint(String.Format("Could not read the newsletter with CampaignID {0} from database {1}.", CampaignID, dbName));
            else
                System.IO.File.WriteAllText(CampaignID + ".html", reader.GetString(0));
            reader.Close();
        }

        [System.Diagnostics.Conditional("DEBUG")]
        static void DebugSaveNewsletterToFile(string id, string newsletter)
        {
            System.IO.File.WriteAllText(id + ".html", newsletter);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var hf = HostFactory.Run(x =>
            {
                x.Service<MyService>(s =>
                {
                s.ConstructUsing(name => new MyService());
                s.WhenStarted(tc => tc.Start());
                s.WhenStopped(tc => tc.Stop());

                s.ScheduleQuartzJob(q =>
                    q.WithJob(() =>
                        JobBuilder.Create<MyJob>().Build())
                        .AddTrigger(() => TriggerBuilder.Create()
                            .WithSimpleSchedule(b => b
                                .WithIntervalInSeconds(30)
                                .RepeatForever())
                            .Build()));
                });
                x.RunAsLocalSystem()
                    .DependsOnEventLog()
                    .StartAutomatically()
                    .EnableServiceRecovery(rc => rc.RestartService(1));

                x.SetDescription("Queries newsletters.");
                x.SetDisplayName("EmmaresNL");
                x.SetServiceName("Emmares Newsletter Service");
            });
        }
    }
}
