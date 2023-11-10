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
    public class SharedEquipmentDailyTotalsRepository
    {

        public static List<ActivityDailyTotals> GetDailyByActivityAsync(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                var activityDate = GetActivityDate(activityId);

                var idParameter = new SqlParameter("@activityId", SqlDbType.BigInt) { Value = activityId };
                var activityDateParameter = new SqlParameter("@activityDate", SqlDbType.DateTime) { Value = activityDate };

                var query = $"exec DailyEquipmentHoursForActivity {idParameter.ParameterName},{activityDateParameter.ParameterName}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<ActivityDailyTotals>(query, idParameter, activityDateParameter)
                    .ToList();
            }
        }

        public static List<EquipmentDailyTotals> GetDailyByEquipmentCodeAsync(long activityId, string equipmentCode)
        {
            using (var _context = new ApplicationContext())
            {
                var activityDate = GetActivityDate(activityId);

                var equpimentCodeParameter = new SqlParameter("@equipmentCode", SqlDbType.VarChar) { Value = equipmentCode };
                var activityDateParameter = new SqlParameter("@activityDate", SqlDbType.Date) { Value = activityDate };

                var query = $"exec DailyEquipmentHoursForEquipmentCode {activityDateParameter.ParameterName},{equpimentCodeParameter.ParameterName}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<EquipmentDailyTotals>(query, activityDateParameter, equpimentCodeParameter)
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
    }
    public class ActivityDailyTotals
    {
        public string EquipmentCode { get; set; }

        public string EquipmentDescription { get; set; }

        public decimal? Hours { get; set; }
    }

    public class EquipmentDailyTotals
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
