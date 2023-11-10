using CrewLink.WindowsServices.Files;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewlink.WindowsServices.Files.Shared
{
    public class SharedEquipmentWeeklyTotalRepository
    {

        public static List<ActivityWeeklyTotals> GetByActivityAsync(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                var activityDate = GetActivityDate(activityId);

                var isSpecialProject = GetSpecialProjectStatus(activityId);

                var workWeek = Helper.GetWorkWeekByDate(activityDate);

                if (isSpecialProject)
                {
                    workWeek = Helper.GetWorkWeekByDateSpecialProjects(activityDate);
                }

                var idParameter = new SqlParameter("@activityId", SqlDbType.BigInt) { Value = activityId };
                var startDateParameter = new SqlParameter("@activityStartDate", SqlDbType.DateTime) { Value = workWeek.WeekStart };
                var endDateParameter = new SqlParameter("@activityEndDate", SqlDbType.DateTime) { Value = workWeek.WeekEnd };

                var query = $"exec WeeklyEquipmentHoursForActivity {idParameter.ParameterName},{startDateParameter.ParameterName},{endDateParameter.ParameterName}";

                ((ApplicationContext)_context).Database.CommandTimeout = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SqlCommandTimeOut"]);

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<ActivityWeeklyTotals>(query, idParameter, startDateParameter, endDateParameter)
                    .ToList();
            }
        }

        public static List<EquipmentWeeklyTotals> GetByEquipmentCodeAsync(long activityId, string equipmentCode)
        {
            using (var _context = new ApplicationContext())
            {
                var activityDate = GetActivityDate(activityId);

                var isSpecialProject = GetSpecialProjectStatus(activityId);

                var workWeek = Helper.GetWorkWeekByDate(activityDate);

                if (isSpecialProject)
                {
                    workWeek = Helper.GetWorkWeekByDateSpecialProjects(activityDate);
                }

                var equpimentCodeParameter = new SqlParameter("@equipmentCode", SqlDbType.VarChar) { Value = equipmentCode };
                var startDateParameter = new SqlParameter("@activityStartDate", SqlDbType.Date) { Value = workWeek.WeekStart };
                var endDateParameter = new SqlParameter("@activityEndDate", SqlDbType.Date) { Value = workWeek.WeekEnd };

                var query = $"exec WeeklyEquipmentHoursForEquipmentCode {startDateParameter.ParameterName},{endDateParameter.ParameterName},{equpimentCodeParameter.ParameterName}";

                ((ApplicationContext)_context).Database.CommandTimeout = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["SqlCommandTimeOut"]);

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<EquipmentWeeklyTotals>(query, startDateParameter, endDateParameter, equpimentCodeParameter)
                    .ToList();
            }
        }

        private static DateTime GetActivityDate(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                .Get<Activity>()
                .Where(x => x.Id == activityId)
                .Select(x => x.ActivityDate)
                .First();
            }
        }

        private static bool GetSpecialProjectStatus(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                var isSpecialProject = false;

                var contract = (from c in _context.Get<Contract>()
                                join j in _context.Get<Job>()
                                on c.ContractNumber equals j.ContractNumber
                                join a in _context.Get<Activity>()
                                on j.Id equals a.JobId
                                where a.Id == activityId
                                select c).FirstOrDefault();

                isSpecialProject = contract != null && contract.IsSpecialProject;

                return isSpecialProject;
            }
        }
    }

    public class ActivityWeeklyTotals
    {
        public string EquipmentCode { get; set; }

        public string EquipmentDescription { get; set; }

        public decimal? Hours { get; set; }
    }

    public class EquipmentWeeklyTotals
    {
        public string EquipmentCode { get; set; }

        public string EquipmentDescription { get; set; }

        public decimal? Hours { get; set; }

        public string JobNumber { get; set; }

        public string JobDescription { get; set; }

        public DateTime ActivityDate { get; set; }

        public string ActivityStatus { get; set; }

        public string Foreman { get; set; }
    }
}
