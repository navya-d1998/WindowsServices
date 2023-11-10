using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;

namespace Crewlink.WindowsServices.Features
{
     public  class GetPecoFLRDFRAdditionalInfo
    {
        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRPecoFLR.GetAdditionalInfo.Response
        {
            public int DfrId { get; set; }

            public string DfrName { get; set; }

            public FLRMasterData AdditionalInfo { get; set; } = new FLRMasterData();

            //public IEnumerable<BusinessTypes> ActivityType { get; set; } = new List<BusinessTypes>();
            public class FLRMasterData
            {
                public IEnumerable<WorkOrderData> WorkOrderRecords { get; set; } = new List<WorkOrderData>();
                public IEnumerable<MasterData> FLRAddiotionalInfo { get; set; } = new List<MasterData>();
            }
            public class WorkOrderData
            {
                public string WorkOrderNumber { get; set; }
                public long? JobPayItemID { get; set; }
                public WorkOrderData WorkOrderInfo { get; internal set; }
            }

            public class MasterData
            {
                public string WorkOrderNumber { get; set; }

                public string Name { get; set; }

                public int? DisplayOrder { get; set; }

                public string Section { get; set; }

                public List<DetailRecords> Data { get; set; }

                public int? ItemOrder { get; set; }

                public long? JobPayItemID { get; set; }

                public class DetailRecords
                {
                    public Guid Id { get; set; }

                    public string Value { get; set; }

                    public string WorkOrderNumber { get; set; }

                    public int? ItemOrder { get; set; }

                    public long? JobPayItemID { get; set; }
                }
            }


        }

        public static ResponseData GetData(long ActivityId, int DfrId)
        {
            var response = new ResponseData();


            var revenue = GetRevenue(ActivityId, response, DfrId);

            var worecords = revenue.GroupBy(x => new { x.WorkOrderNumber })
            .Select(x => new ResponseData.WorkOrderData
            {
                WorkOrderNumber = x.Key.WorkOrderNumber,
                WorkOrderInfo = x.Select(w => new ResponseData.WorkOrderData
                {
                    WorkOrderNumber = w.WorkOrderNumber,
                    JobPayItemID = w.Id
                }).FirstOrDefault()
            }).ToList();

            response.AdditionalInfo.WorkOrderRecords = worecords.Select(x => x.WorkOrderInfo).ToList();

            GetAdditionalInfo(DfrId, ActivityId, response);

            response.DfrId = DfrId;

            response.DfrName = SharedDFRDataRepository.GetDfrName(DfrId);

            return response;
        }

        public static List<RevenueActivity> GetRevenue(long ActivityId, ResponseData response, int DfrId)
        {
            using (var _context = new ApplicationContext())
            {
                var activity =  _context
                       .Get<Activity>()
                       .AsNoTracking()
                       .Include(x => x.RevenueActivity)
                       .Include(x => x.RevenueActivity.Select(r => r.City))
                       .Include(x => x.RevenueActivity.Select(r => r.Customer))
                       .Include(x => x.RevenueActivity.Select(r => r.PayItem))
                       .Where(x => x.Id == ActivityId)
                       .First();

                return activity.RevenueActivity.ToList();
            }
        }
        private static void GetAdditionalInfo(int DfrId, long ActivityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var additionalInputStandard = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
                               .Where(DfrAdditionalInfoMaster.IsActiveQueryFilter)
                                               join infotrans in _context.Get<DfrAdditionalInfoTrans>()
                                                   .Where(x => x.JobId == ActivityId)
                                                   on infomaster.Id equals infotrans.AdditionalInfoID into lj
                                               from infotrans in lj.DefaultIfEmpty()
                                               where infomaster.DFRId == DfrId
                                               select (new
                                               {
                                                   Id = infomaster.Id,
                                                   Name = infomaster.Name,
                                                   Value = infotrans == null ? null : infotrans.InfoValue,
                                                   Section = infomaster.Section.Trim(),
                                                   DisplayOrder = infomaster.DisplayOrder,
                                                   ItemOrder = infotrans.ItemOrder,
                                                   WorkOrderNumber = infotrans.WorkOrderNumber,
                                                   JobPayItemID = infotrans.PayitemId == null ? null : infotrans.PayitemId
                                               })).OrderBy(x => x.DisplayOrder).ToList();


                response.AdditionalInfo.FLRAddiotionalInfo = additionalInputStandard.GroupBy(x => new { x.Name, x.WorkOrderNumber, x.ItemOrder })
                            .Select(x => new ResponseData.MasterData
                            {
                                Name = x.Key.Name,
                                WorkOrderNumber = x.Key.WorkOrderNumber,
                                ItemOrder = x.Key.ItemOrder,
                                Data = x.Select(r => new ResponseData.MasterData.DetailRecords
                                {
                                    Id = r.Id,
                                    Value = r.Value,
                                    WorkOrderNumber = r.WorkOrderNumber,
                                    ItemOrder = r.ItemOrder,
                                    JobPayItemID = r.JobPayItemID == null ? null : r.JobPayItemID
                                }).ToList()
                            }).ToList();

                response.DfrId = DfrId;

                response.DfrName = SharedDFRDataRepository.GetDfrName(DfrId);
            }
        }
    }

}

