
namespace CrewLink.WindowsServices
{

    using CrewlinkServices.Core.DataAccess;
    using CrewlinkServices.Core.Models;
    using CrewlinkServices;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.ServiceProcess;
    using System.Text;
    using System.Threading.Tasks;
    using System.Reflection;
    using RazorEngine;
    using CrewLink.WindowsServices.Files;
    using System.IO.Compression;
    using System.Timers;
    using iTextSharp.text;
    using CrewLink.WindowsServices;
    using Crewlink.WindowsServices.Features;


    public partial class Service1 : ServiceBase
    {
        private static System.Timers.Timer CrewlinkTimer;
        public Service1()
        {
            InitializeComponent();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        public static void ProcessEmail(List<EmailInformation> emailList)
        {
            try
            {


                var degreeofparallelism = 2;

                using (var _context = new ApplicationContext())
                {
                    var existingData = _context.Get<WindowsConfiguration>().Where(x => x.ConfigurationKey.ToLower() == "degree_of_parallelism").FirstOrDefault();

                    if (existingData != null)
                    {
                        degreeofparallelism = Convert.ToInt32(existingData.ConfigurationValue);
                    }
                }


                //var degreecount = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0));
                //var fullcount = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 1) * 1.0));


                Parallel.ForEach(emailList, new ParallelOptions { MaxDegreeOfParallelism = degreeofparallelism }, emailData =>
                {
                    singleMailFunction(emailData);

                });


            }
            catch (Exception ex)
            {
                Helper.LogError(ex, "ProcessEmail");

                //   System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "catch_56 " + ".txt");

                string fullexceptiondetails = ex.Message + "||||" + ex.StackTrace;

                throw new Exception(fullexceptiondetails);
            }
        }

        private static void CrewlinkTimer_Elapsed(object sender, ElapsedEventArgs e)
        {


            CrewlinkTimer.Stop();

            try
            {

                var finalList = new List<EmailInformation>();

                var emailList = new List<EmailInformation>();

                var failedEmails = new List<EmailInformation>();

                var retryEmailList = new List<EmailInformation>();

                var jobCount = 5;

                using (var _context = new ApplicationContext())
                {

                    emailList = _context.Get<EmailInformation>().
                                       Where(x => x.EmailStatus == 1)
                                       .ToList();

                    failedEmails = _context.Get<EmailInformation>().
                                       Where(x => x.EmailStatus == 3 && x.RetryCount < 3 )
                                       .ToList();

                }


                using (var _context = new ApplicationContext())
                {

                    var existingData = _context.Get<WindowsConfiguration>().Where(x => x.ConfigurationKey.ToLower() == "job_processing_count").FirstOrDefault();
             
                    if (existingData != null)
                    {
                        jobCount = Convert.ToInt32(existingData.ConfigurationValue);
                    }

                    var count = 0;

                    foreach (var x in emailList)
                    {
                        if (count < jobCount)
                        {
                            finalList.Add(x);
                        }
                        count++;
                    }

                    count = 0;

                    foreach (var x in failedEmails)
                    {
                        if (count < jobCount)
                        {
                            retryEmailList.Add(x);
                        }
                        count++;
                    }

                }

                ProcessEmail(retryEmailList);

                ProcessEmail(emailList);

            }
            catch (Exception ex)
            {
                Helper.LogError(ex, "CrewlinkTimer_Elapsed_catch");

            }
            finally
            {

                var currentDay = DateTime.Today.DayOfWeek.ToString();

                if (currentDay.Equals("Monday"))
                {
                    CrewlinkTimer.Enabled = false;

                    CrewlinkTimer.Interval = 1000 * 20;

                }
                else
                {

                    CrewlinkTimer.Enabled = false;

                    CrewlinkTimer.Interval = 1000 * 60;

                }

                CrewlinkTimer.Start();

            }
        }

        public static void singleMailFunction(EmailInformation emailInfo)
        {
     
            using (var _context = new ApplicationContext())
            {
                var existingData = _context.Get<EmailInformation>().Where(x => x.Id == emailInfo.Id).FirstOrDefault();

                var statusInfo = _context.Get<EmailFileStatus>()
                                      .Where(x => x.Status_Value == 4)
                                      .FirstOrDefault();

                if (existingData != null)
                {
                    existingData.EmailStatus = statusInfo.Id;
                    existingData.ModifiedOn = DateTime.Now;
                }
                _context.Update(existingData);
                _context.SaveChanges();
            }

            var DFRFileList = Helper.GetAllDFRInfo(emailInfo);

            try
            {
                string zipFilename = string.Format(@"{0}{1}.zip", "CrewLink" + emailInfo.DfrType, System.DateTime.Now.ToString("yyyyMMddHHmm"));

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        foreach (var url in DFRFileList)
                        {

                            dynamic request;
                            switch (emailInfo.DfrType)
                            {
                                case "nashvilledfr":
                                    request = new GetNashvilleDFRData();
                                    break;
                                case "tnedfr":
                                    request = new GetStandardDFRData();
                                    break;
                                case "spire24DFR_":
                                    request = new GetSpire24DFRData();
                                    break;
                                case "bhdfr":
                                    request = new GetBlackHillsDFRData();
                                    break;
                                case "dtestandard":
                                    request = new GetDteStandardDFRData();
                                    break;
                                case "dteDFR_":
                                    request = new GetDteDFRData();
                                    break;
                                case "pngDFR_":
                                    request = new GetPngDFRData();
                                    break;
                                case "xcelDFR_":
                                    request = new GetXCELDFRData();
                                    break;
                                case "pecoDFR_":
                                    request = new GetPecoDFRData();
                                    break;
                                case "wglvablanketDFR_":
                                    request = new GetWglVaBlanketDFRData();
                                    break;
                                case "wglDFR_":
                                    request = new GetWglBundleDFRData();
                                    break;
                                case "nipscoDFR_":
                                    request = new GetNipscoDFRData();
                                    break;
                                case "vectrenDFR_":
                                    request = new GetVectrenDFRData();
                                    break;
                                case "dprhourlywork_":
                                    request = new GetDPRHourlyWorkDFRData();
                                    break;
                                case "DFR_":
                                    request = new GetTemplateByIdData();
                                    break;
                                case "mgedfr_":
                                    request = new GetMGENewConstructionData();
                                    break;
                                case "pecoFlrDFR_":
                                    request = new GetPecoFLRDFRData();
                                    break;
                                case "spireSewerCamdfr_":
                                    request = new GetSpireSewerCameraDFRData();
                                    break;
                                case "wgllandoverdfr":
                                    request = new GetWglLandoverPh5DFRData();
                                    break;
                                default:
                                    request = new GetStandardDFRData();
                                    break;
                            }

                            using (var _context = new ApplicationContext())
                            {
                                var existingData = _context.Get<DFRFileInformation>().Where(x => x.Id == url.Id).FirstOrDefault();

                                if (existingData != null)
                                {
                                    existingData.FileStatus = 4;
                                    existingData.ModifiedOn = DateTime.Now;
                                }
                                _context.Update(existingData);
                                _context.SaveChanges();
                            }

                            var dfrData = request.ExecuteRequest(url.Token);

                           var folderData = GenerateZipFolder.ExecuteResult(dfrData.FileContent, dfrData.TemplateSize, dfrData.NumberOfPages, dfrData.FileName);

                            var fileRead = folderData.Content.ReadAsByteArrayAsync().Result;

                            ZipArchiveEntry entry = archive.CreateEntry(url.FileName + ".pdf", CompressionLevel.Optimal);

                            using (Stream ZipFile = entry.Open())
                            {
                                byte[] data = fileRead;
                                if (data != null)
                                    ZipFile.Write(data, 0, data.Length);

                            }

                            Helper.UpdateDFRFileStatus(url, 2);
                        }
                    }
                    if (memoryStream != null && memoryStream.Length != 0)
                    {
                        memoryStream.Position = 0;
               
                        SendEmail.SendReportAsEmail(memoryStream, zipFilename + ".zip", emailInfo);

                        Helper.UpdateEmailStatus(emailInfo, 2);

                        Helper.UpdateAllEmailDFRFileStatus(emailInfo, 2);
                    }

                }
            }
            catch (Exception ex)
            {
                Helper.UpdateAllEmailDFRFileStatus(emailInfo, 3);

                Helper.UpdateEmailStatus(emailInfo, 3);

                Helper.LogError(ex, "singleMailFunction_catch");

            }
        }




        protected override void OnStart(string[] args)
        {

            try

            {         
                Helper.Logger("Before Start", "OnStart", 0);

                CrewlinkTimer = new Timer(1000 * 20) { AutoReset = false };

                CrewlinkTimer.Elapsed += new ElapsedEventHandler(CrewlinkTimer_Elapsed);

                CrewlinkTimer.Start();

                Helper.Logger("Timer started", "OnStart", 0);
            }
            catch (Exception ex)
            {
                Helper.LogError(ex, "OnStart");
            }

        }

        protected override void OnStop()
        {
            using (var _context = new ApplicationContext())
            {
                var emailList = _context.Get<EmailInformation>().
                                       Where(x => x.EmailStatus == 4)
                                       .ToList();

                foreach (var x in emailList)
                {
                    x.EmailStatus = 1;
                    _context.Update(x);
                }

                _context.SaveChanges();
            }

            Helper.Logger("Stop", "OnStop", 0);

        }
    }
}
