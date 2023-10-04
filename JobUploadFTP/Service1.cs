using System.Net;
using System.Timers;
using System.IO;
using System.ServiceProcess;
using System.Configuration;
using System;
using System.Linq;
using System.Diagnostics;

namespace JobUploadFTP
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        public EventLog eventLog;

        public Service1()
        {
            InitializeComponent();

            eventLog = new System.Diagnostics.EventLog();
            if (!EventLog.SourceExists("Sincronizador"))
                EventLog.CreateEventSource("Sincronizador", "Sincronizador Subway");

            eventLog.Source = "Sincronizador";
            eventLog.Log = "Sincronizador Subway";
        }

        protected override void OnStart(string[] args)
        {
            eventLog.WriteEntry("Start");
            _timer = new Timer(Convert.ToInt32(ConfigurationManager.AppSettings["Timer"]));
            _timer.Elapsed += TimerElapsed;
            _timer.Start();
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            UploadFilesToFtp();
        }

        private void UploadFilesToFtp()
        {
            string ftpServer = ConfigurationManager.AppSettings["FtpServer"];
            string ftpUser = ConfigurationManager.AppSettings["FtpUser"];
            string ftpPassword = ConfigurationManager.AppSettings["FtpPassword"];
            string sourceFolder = ConfigurationManager.AppSettings["SourceFolder"];

            var allowedExtensions = ConfigurationManager.AppSettings["AllowedExtensions"]
                                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(e => e.Trim().ToLower())
                                        .ToList();

            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                string fileExtension = Path.GetExtension(file).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    continue;
                }

                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(ftpUser, ftpPassword);
                    try
                    {
                        client.UploadFile(ftpServer + "/" + Path.GetFileName(file), WebRequestMethods.Ftp.UploadFile, file);
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        eventLog.WriteEntry($"Error al procesar el archivo {file}. Detalle: {ex.Message}");
                    }
                }
            }
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("Stop");
            _timer.Stop();
            _timer.Dispose();
        }
    }
}
