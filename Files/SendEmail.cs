using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Identity;
using CrewlinkServices.Models.CustomModel;
using CrewlinkServices.Models.DB;
using CrewlinkServices.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using CrewlinkServices.Core.Models;

namespace CrewLink.WindowsServices.Files
{
    public static class SendEmail
    {

        public static CommonStatusResponse SendReportAsEmail(MemoryStream streamObj, string fileName, EmailInformation emailInfo)
        {
            using (var contxt = new ApplicationContext())
            {
                CLDBEntities DB = new CLDBEntities();
                var statusResponse = new CommonStatusResponse();
                EmailViewModel EmailInfo = new EmailViewModel();
                Common CommonObj = new Common();
                MailMessage mailMessage = new MailMessage();
                //TODO: Get current User email id to send email.
                // To Get the mail id from AD uncomment the below line.
                var emailId = emailInfo.EmailAddress;

              //  var emailId = adService.GetEmailAddress("reviewer.one");
                List<ConfigDataResponse> RevenueReportEmailConfig = DB.GET_CONFIG_DATA_SP("BULK_DFR").ToList();
                Dictionary<String, String> TempDictionary = new Dictionary<String, String>();
                String TempVariable_;
                // Assign the config value to Dictionary Object
                TempDictionary = RevenueReportEmailConfig.ToDictionary(key => key.Configuration_Key, value => value.Configuration_Value);
                if (RevenueReportEmailConfig.Count().Equals(0))
                {
                    statusResponse.Status = "0";
                    statusResponse.StatusDesc = "Failed";
                    statusResponse.Description = "Record not found in configuration so please contact administrator.";
                    return statusResponse;
                }
                else
                {
                    var dfrName = '"' + emailInfo.DfrType.TrimEnd('_') + '"';
                    TempDictionary.TryGetValue("BulkDFR_Email_TO", out TempVariable_);
                    mailMessage.To.Add(emailInfo.EmailAddress);

                    //TempDictionary.TryGetValue("BulkDFR_Email_CC", out TempVariable_);
                    //if (!String.IsNullOrEmpty(TempVariable_))
                    //    mailMessage.CC.Add(TempVariable_);
                    //TempDictionary.TryGetValue("BulkDFR_Email_BCC", out TempVariable_);
                    //if (!String.IsNullOrEmpty(TempVariable_))
                    //    mailMessage.Bcc.Add(TempVariable_);
                    TempDictionary.TryGetValue("BulkDFR_Email_Subject", out TempVariable_);
                    mailMessage.Subject = TempVariable_.Replace("@ED", DateTime.Now.ToString("MM/dd/yyyy"));
                    TempDictionary.TryGetValue("BulkDFR_Email_Body", out TempVariable_);
                    TempVariable_ = TempVariable_.Replace("@TemplateName", dfrName);
                    TempVariable_ = TempVariable_.Replace("@StartDate", emailInfo.StartDate.AddDays(1).ToString("MM/dd/yyyy"));
                    TempVariable_ = TempVariable_.Replace("@EndDate", emailInfo.EndDate.AddDays(1).ToString("MM/dd/yyyy"));
                    mailMessage.Body = TempVariable_;
                    mailMessage.IsBodyHtml = true;
                    Attachment attachment;
                    attachment = new System.Net.Mail.Attachment(streamObj, fileName, "application/zip");
                    mailMessage.Attachments.Add(attachment);
                    statusResponse = ExportDFRReport(mailMessage, emailInfo);
                    return statusResponse;
                }
            }
        }
        public static CommonStatusResponse ExportDFRReport(MailMessage mailMessage, EmailInformation emailInfo)
        {
            CommonStatusResponse StatusResponse = new CommonStatusResponse();
            String TempVariable_;


            String TempVariable1_;

            Dictionary<String, String> dictionary = new Dictionary<String, String>();

            SmtpClient smtpClient = new SmtpClient();
            try
            {
                var DB = new CLDBEntities();
                using (DB)
                {
                    List<ConfigDataResponse> EmailConfigData = DB.GET_CONFIG_DATA_SP("EMAIL_CONFIG").ToList();

                    dictionary = EmailConfigData.ToDictionary(key => key.Configuration_Key, value => value.Configuration_Value);

                    dictionary.TryGetValue("SmtpClient_From_Address", out TempVariable_);
                    dictionary.TryGetValue("SmtpClient_From_Name", out TempVariable1_);
                    mailMessage.From = new MailAddress(TempVariable_, TempVariable1_);

                    dictionary.TryGetValue("SmtpClient_Host", out TempVariable_);
                    smtpClient.Host = TempVariable_;

                    dictionary.TryGetValue("SmtpClient_Port", out TempVariable_);
                    smtpClient.Port = Convert.ToInt16(TempVariable_);
                    // smtpClient.UseDefaultCredentials = false;

                    dictionary.TryGetValue("SmtpClient_Username", out TempVariable_);
                    dictionary.TryGetValue("SmtpClient_Password", out TempVariable1_);
                    smtpClient.Credentials = new NetworkCredential(TempVariable_, TempVariable1_);

                    // smtpClient.Host = "smtp.gmail.com";

                    dictionary.TryGetValue("SmtpClient_EnableSsl", out TempVariable_);
                    smtpClient.EnableSsl = Convert.ToBoolean(TempVariable_);

                    smtpClient.Send(mailMessage);

                  //  Helper.Logger("Email sent", "ExportDFRReport", 0);

        
                    using (var _context = new ApplicationContext())
                    {
                        var existinfData = _context.Get<EmailInformation>().Where(x => x.Id == emailInfo.Id).FirstOrDefault();
                        if (existinfData != null)
                        {
                            existinfData.EmailStatus = 2;
                            existinfData.CompletedOn = DateTime.Now;
                        }
                        _context.Update(existinfData);
                        _context.SaveChanges();
                    }
                    StatusResponse.Status = "1";
                    StatusResponse.StatusDesc = "Success";
                    StatusResponse.Description = "Email have been sent successfully";
                }
                DB.Dispose();
            }
            catch (Exception ex)
            {
                Helper.UpdateAllEmailDFRFileStatus(emailInfo, 3);

                Helper.UpdateEmailStatus(emailInfo, 3);

                Helper.LogError(ex, "ExportDFRReport");

                StatusResponse.Status = "0";
                StatusResponse.StatusDesc = "Failed";
                StatusResponse.Description = ex.ToString();

                Helper.LogError(ex, "ExportDFRReport");

            }
            return StatusResponse;
        }
    }
}
