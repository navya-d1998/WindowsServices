
namespace CrewLink.WindowsServices.Files.Shared
{
    using CrewlinkServices.Core.DataAccess;
    using CrewlinkServices.Core.Models;
    using CrewlinkServices.Features.DailyActivity.Labor.Shared;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    public interface ISharedLaborWeeklyTotalRepository
    {
        List<ActivityWeeklyTotals> GetByActivityAsync(long activityId);

        List<EmployeeWeeklyTotals> GetByEmployeeAsync(long activityId, string employeeNumber);
    }

    public class SharedLaborWeeklyTotalRepository
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

                var query = $"exec WeeklyLaborHoursForActivity {idParameter.ParameterName},{startDateParameter.ParameterName},{endDateParameter.ParameterName}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<ActivityWeeklyTotals>(query, idParameter, startDateParameter, endDateParameter)
                    .ToList();
            }
        }

        public static List<EmployeeWeeklyTotals> GetByEmployeeAsync(long activityId, string employeeNumber)
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

                var employeeNumberParameter = new SqlParameter("@employeeNumber", SqlDbType.VarChar) { Value = employeeNumber };

                var startDateParameter = new SqlParameter("@activityStartDate", SqlDbType.Date) { Value = workWeek.WeekStart };

                var endDateParameter = new SqlParameter("@activityEndDate", SqlDbType.Date) { Value = workWeek.WeekEnd };

                var query = $"exec WeeklyLaborHoursForEmployee {startDateParameter.ParameterName},{endDateParameter.ParameterName},{employeeNumberParameter}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<EmployeeWeeklyTotals>(query, startDateParameter, endDateParameter, employeeNumberParameter)
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
        private ActivityWeeklyTotals() { }

        public string EmployeeNumber { get; set; }

        public string EmployeeName { get; set; }

        public decimal StandardHours { get; set; }

        public decimal OvertimeHours { get; set; }

        public decimal DoubleTimeHours { get; set; }

        public decimal Overrides { get; set; }

        public decimal PaidTimeOff { get; set; }

        public decimal HolidayPay { get; set; }

        public decimal AutoAllowance { get; set; }

        public decimal PerDiam { get; set; }

        public string PerDiamType { get; set; }

        public decimal RigRentals { get; set; }

        public decimal MiscCode { get; set; }

        public decimal TravelPay { get; set; }

        public decimal WaitTimeHours { get; set; }
        public string UnionCode { get; set; }
    }

    public class EmployeeWeeklyTotals
    {
        private EmployeeWeeklyTotals() { }

        public string EmployeeNumber { get; set; }

        public string EmployeeName { get; set; }

        public decimal StandardHours { get; set; }

        public decimal OvertimeHours { get; set; }

        public decimal DoubleTimeHours { get; set; }

        public decimal Overrides { get; set; }

        public decimal PaidTimeOff { get; set; }

        public decimal HolidayPay { get; set; }

        public decimal AutoAllowance { get; set; }

        public decimal PerDiam { get; set; }

        public decimal RigRentals { get; set; }

        public decimal MiscCode { get; set; }

        public decimal TravelPay { get; set; }

        public decimal WaitTimeHours { get; set; }

        public string JobNumber { get; set; }

        public string JobDescription { get; set; }

        public string JobStatus { get; set; }

        public DateTime ActivityDate { get; set; }

        public string Foreman { get; set; }
        public string UnionCode { get; set; }
    }
}
