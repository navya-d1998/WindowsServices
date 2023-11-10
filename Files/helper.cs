using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewLink.WindowsServices.Files
{
    public static class Helper
    {
        public static List<EmailFileStatus> GetStatusId()
        {
            using (var _context = new ApplicationContext())
            {

                var statusList = _context.Get<EmailFileStatus>()
                                        .ToList();

                return statusList;
            }
        }

        public static List<DFRFileInformation> GetAllDFRInfo(EmailInformation emailData)
        {
            using (var _context = new ApplicationContext())
            {

                var fileList = _context.Get<DFRFileInformation>()
                                        .Where(x => x.EmailInfoId == emailData.Id)
                                        .ToList();

                return fileList;
            }
        }
        public static void UpdateAllEmailDFRFileStatus(EmailInformation emailData, int status)
        {
            using (var _context = new ApplicationContext())
            {

                var listDFR = _context.Get<DFRFileInformation>()
                                        .Where(x => x.EmailInfoId == emailData.Id)
                                        .ToList();


                var statusInfo = _context.Get<EmailFileStatus>()
                                        .Where(x => x.Status_Value == status)
                                        .FirstOrDefault();

                // logic for retry count

                if (listDFR.Count != 0)
                {
                    foreach(var dfrData in listDFR)
                    {
                        if (status == 3 && dfrData.RetryCount < 3)
                        {
                            dfrData.RetryCount = dfrData.RetryCount + 1;
                        }

                        dfrData.FileStatus = statusInfo.Id;

                        _context.Update(dfrData);

                        _context.SaveChanges();
                    }
                  
                }

            }
        }

        public static void UpdateDFRFileStatus( DFRFileInformation fileData, int status)
        {
            using (var _context = new ApplicationContext())
            {

                var existingFileInfo = _context.Get<DFRFileInformation>()
                                        .Where(x => x.Id == fileData.Id)
                                        .FirstOrDefault();

                var statusInfo = _context.Get<EmailFileStatus>()
                               .Where(x => x.Status_Value == status)
                               .FirstOrDefault();

                if (existingFileInfo != null)
                {
                    if (status == 3 && existingFileInfo.RetryCount < 3)
                    {
                        existingFileInfo.RetryCount = existingFileInfo.RetryCount + 1;
                    }

                    existingFileInfo.FileStatus = statusInfo.Id;

                    _context.Update(existingFileInfo);

                    _context.SaveChanges();
                }

            }
        }

        public static void UpdateEmailStatus(EmailInformation emailData, int status)
        {
            using (var _context = new ApplicationContext())
            {

                var existingEmailInfo = _context.Get<EmailInformation>()
                                        .Where(x => x.Id == emailData.Id)
                                        .FirstOrDefault();

                var statusInfo = _context.Get<EmailFileStatus>()
                               .Where(x => x.Status_Value == status)
                               .FirstOrDefault();


                if (existingEmailInfo != null)
                {
                    existingEmailInfo.EmailStatus = statusInfo.Id;

                    if(status == 3 && existingEmailInfo.RetryCount <3)
                    {
                        existingEmailInfo.RetryCount = existingEmailInfo.RetryCount + 1;
                    }

                    _context.Update(existingEmailInfo);

                    _context.SaveChanges();
                }

            }
        }

        public static void LogError(Exception ex, string source)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("********************" + " Error Log - " + DateTime.Now + "*********************");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append("Exception Type : " + ex.GetType().Name);
            sb.Append(Environment.NewLine);
            sb.Append("Error Message : " + ex.Message);
            sb.Append(Environment.NewLine);
            sb.Append("Error Source : " + ex.Source);
            sb.Append(Environment.NewLine);
            if (ex.StackTrace != null)
            {
                sb.Append("Error Trace : " + ex.StackTrace);
            }
            Exception innerEx = ex.InnerException;
            while (innerEx != null)
            {
                sb.Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
                sb.Append("Exception Type : " + innerEx.GetType().Name);
                sb.Append(Environment.NewLine);
                sb.Append("Error Message : " + innerEx.Message);
                sb.Append(Environment.NewLine);
                sb.Append("Error Source : " + innerEx.Source);
                sb.Append(Environment.NewLine);
                if (ex.StackTrace != null)
                {
                    sb.Append("Error Trace : " + innerEx.StackTrace);
                }
                innerEx = innerEx.InnerException;
            }
            Logger(sb.ToString(), source, 1);
        }
        public static void Logger(string message, string source, int statusType)
        {
            using (var _context = new ApplicationContext())
            {
                var messageData = new LogMessage();
                messageData.LogDate = DateTime.Now;
                messageData.Message = message;
                messageData.Source = source;
                messageData.StatusType = statusType;
                _context.Add(messageData);
                _context.SaveChanges();
            }
        }
        public static WorkWeekDateRange GetPriorWorkWeekByDate(DateTime date)
        {
            // previous Sunday
            var priorWeekEnd = date.AddDays(-(int)date.DayOfWeek);

            // moves back to previous Saturday
            priorWeekEnd = priorWeekEnd.AddDays(-1);

            var priorWeekStart = priorWeekEnd.AddDays(-6);

            return new WorkWeekDateRange(priorWeekStart, priorWeekEnd);
        }

        public static WorkWeekDateRange GetWorkWeekByDate(DateTime date)
        {
            // previous Sunday
            var start = date.AddDays(-(int)date.DayOfWeek);
            var end = start.AddDays(6);

            return new WorkWeekDateRange(start, end);
        }

        public static WorkWeekDateRange GetWorkWeekByDateSpecialProjects(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Sunday)
            {
                var end = date.AddDays(-(int)date.DayOfWeek);
                var start = end.AddDays(-6);

                return new WorkWeekDateRange(start, end);
            }
            else
            {
                var start = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
                var end = start.AddDays(6);

                return new WorkWeekDateRange(start, end);
            }
        }
        //public static WorkWeekDateRange GetActiveWeekByDateSpecialProjects(DateTime date)
        //{

        //        var start = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
        //        var end = start.AddDays(6);

        //        return new WorkWeekDateRange(start, end);
        //}

        public static WorkWeekDateRange GetWorkMonthByDate(DateTime date)
        {
            var start = new DateTime(date.Year, date.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);

            return new WorkWeekDateRange(start, end);
        }
    }

    public class WorkWeekDateRange
    {
        public WorkWeekDateRange(DateTime startDate, DateTime endDate)
        {
            WeekStart = startDate.Date;
            WeekEnd = endDate.Date;
        }

        public DateTime WeekStart { get; }

        public DateTime WeekEnd { get; }
    }
}
