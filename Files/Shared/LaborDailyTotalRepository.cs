
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


    public interface ISharedLaborDailyTotalRepository
    {
        List<ActivityDailyTotals> GetByActivityAsync(long activityId);

        List<EmployeeDailyTotals> GetByEmployeeAsync(long activityId, string employeeNumber);
    }

    public class SharedLaborDailyTotalRepository
    {


        public static List<ActivityDailyTotals> GetByActivityAsync(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                var activityDate = GetActivityDate(activityId);

                var idParameter = new SqlParameter("@activityId", SqlDbType.BigInt) { Value = activityId };

                var activityDateParameter = new SqlParameter("@activityDate", SqlDbType.DateTime) { Value = activityDate };

                var query = $"exec DailyLaborHoursForActivity {idParameter.ParameterName},{activityDateParameter.ParameterName}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<ActivityDailyTotals>(query, idParameter, activityDateParameter)
                    .ToList();
            }
        }
        public static List<EmployeeDailyTotals> GetByEmployeeAsync(long activityId, string employeeNumber)
        {
            using (var _context = new ApplicationContext())
            {
                var activityDate = GetActivityDate(activityId);

                var activityDateParameter = new SqlParameter("@activityDate", SqlDbType.Date) { Value = activityDate };

                var employeeNumberParameter = new SqlParameter("@employeeNumber", SqlDbType.VarChar) { Value = employeeNumber };

                var query = $"exec DailyLaborHoursForEmployee {activityDateParameter.ParameterName},{employeeNumberParameter.ParameterName}";

                return ((ApplicationContext)_context)
                .Database
                .SqlQuery<EmployeeDailyTotals>(query, activityDateParameter, employeeNumberParameter)
                .ToList();
            }
        }

        private static DateTime GetActivityDate(long activityId)
        {
            var timeData = new DateTime();
            using (var _context = new ApplicationContext())
            {
                timeData = _context
                .Get<Activity>()
                .Where(x => x.Id == activityId)
                .Select(x => x.ActivityDate)
                .First();
            }
            return timeData;
        }

    }
    public class ActivityDailyTotals
    {
        private ActivityDailyTotals() { }

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
    }

    public class EmployeeDailyTotals
    {
        private EmployeeDailyTotals() { }

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
