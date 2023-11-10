using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewlink.WindowsServices.Features
{
   public class GetMGENewConstructionDFRAdditionalInfo
    {

        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRNewConstruction.GetAdditionalInfo.Response
        {
            public string DfrName { get; set; }

        }

        public static ResponseData GetData(long ActivityId, int DfrId, string workOrder)
        {

            var response = new ResponseData();

            response.SectionBPayItemsCount = 0;

            response.SectionEPayItemsCount = 0;

             GetSectionA(ActivityId, response, DfrId);

             GetSectionB(ActivityId, response, DfrId, workOrder);

             GetSectionE(ActivityId, response, DfrId, workOrder);

             GetCommonSection(ActivityId, DfrId, workOrder, response);

            GetBusinessType(response);

            response.DfrId = DfrId;

            response.DfrName =  SharedDFRDataRepository.GetDfrName(DfrId);

            return response;
        }

        private static void GetSectionA(long ActivityId, ResponseData response, int DfrId)
        {
            var result = SharedDFRDataRepository.GetDFRAdditionalInfoAsync(ActivityId, "A", DfrId);

            if (result.Count != 0)
            {
                var revenueItems = result
                        .GroupBy(x => new { x.PayitemId, x.PipeType, x.Tier2Description, x.Qty })
                        .Select(x => new ResponseData.SectionA
                        {
                            PayitemId = x.Key.PayitemId,
                            Tier2Description = x.Key.Tier2Description,
                            PipeType = x.Key.PipeType,
                            Qty = x.Key.Qty,
                            Records = x.Select(r => new ResponseData.SectionA.Record
                            {
                                Id = r.Id,
                                Name = r.Name,
                                Value = r.Value,
                                SlNo = r.Name.Contains("LengthOfData") ? 1 : r.Name.Contains("LengthOfProject") ? 2 : 3
                            }).OrderBy(y => y.SlNo).ToList()
                        }).ToList();

                response.SectionAVal = revenueItems;
            }
        }

        private static void GetSectionB(long ActivityId, ResponseData response, int DfrId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {

                response.SectionBVal = null;

                if (CheckSectionBAvailability(ActivityId, workOrder))
                {
                    response.SectionBPayItemsCount = GetSectionBPayItemsCount(ActivityId, workOrder);

                    // && Int32.Parse(infomaster.Name.Replace("Length", "").Replace("Width", "").Replace("Depth", "")) <= response.SectionBPayItemsCount
                    var result =  (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
                                        .Where(DfrAdditionalInfoMaster.IsActiveQueryFilter)
                                        join infotrans in _context.Get<DfrAdditionalInfoTrans>()
                                                .Where(x => x.JobId == ActivityId)
                                                on infomaster.Id equals infotrans.AdditionalInfoID into lj
                                        from infotrans in lj.DefaultIfEmpty()
                                        where infomaster.DFRId == DfrId && infomaster.Section == "B"
                                        select (new
                                        {
                                            Id = infomaster.Id,
                                            Name = infomaster.Name,
                                            Value = infotrans == null ? null : infotrans.InfoValue == "" ? null : infotrans.InfoValue,
                                            Section = infomaster.Section
                                        })).ToList();

                    response.SectionBVal = result.Where(x => x.Section == "B" && Convert.ToInt16(x.Name.Replace("Length", "").Replace("Width", "").Replace("Depth", "").Replace("SectionBSubtype", "").Replace("PipeSize", "")) <= response.SectionBPayItemsCount).GroupBy(x => new { x.Name })
                                .Select(x => new ResponseData.MasterData
                                {
                                    Name = x.Key.Name,
                                    Data = x.Select(r => new ResponseData.MasterData.DetailRecords
                                    { Id = r.Id, Value = r.Value })
                                }).ToList();
                }
            }
        }

        private static void GetSectionE(long ActivityId, ResponseData response, int DfrId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {

                response.SectionEVal = null;

                if (CheckSectionEAvailability(ActivityId, workOrder))
                {
                    response.SectionEPayItemsCount = GetSectionEPayItemsCount(ActivityId, workOrder);

                    //var payitems = await (from payitemmaster in _context.Get<PayItemMapping>()
                    //                      select new
                    //                      {
                    //                          Id = payitemmaster.CompanyCode
                    //                      }).ToListAsync();

                    var result = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
                                       .Where(DfrAdditionalInfoMaster.IsActiveQueryFilter)
                                  join infotrans in _context.Get<DfrAdditionalInfoTrans>()
                                          .Where(x => x.JobId == ActivityId)
                                          on infomaster.Id equals infotrans.AdditionalInfoID into lj
                                  from infotrans in lj.DefaultIfEmpty()
                                  where infomaster.DFRId == DfrId && infomaster.Section == "E"
                                  select (new
                                  {
                                      Id = infomaster.Id,
                                      Name = infomaster.Name,
                                      Value = infotrans == null ? null : infotrans.InfoValue == "" ? null : infotrans.InfoValue,
                                      Section = infomaster.Section,
                                      ItemOrder = infotrans.ItemOrder
                                  })).ToList();

                    response.SectionEVal = result.Where(x => x.Section == "E").GroupBy(x => new { x.Name })
                                .Select(x => new ResponseData.MasterDataSectionE
                                {
                                    Name = x.Key.Name,
                                    Data = x.Select(r => new ResponseData.MasterDataSectionE.DetailRecords
                                    { Id = r.Id, Value = r.Value, ItemOrder = r.ItemOrder }).OrderBy(y => y.ItemOrder)
                                }).ToList();
                }
            }
        }

        private static int GetSectionBPayItemsCount(long activityId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {

                return (from payitem in _context.Get<RevenueActivity>()
                            .Where(RevenueActivity.IsActiveFilter)
                        join payitemmaster in _context.Get<PayItemMapping>()
                                on payitem.PayItemId equals payitemmaster.Id
                        join dfrmapping in _context.Get<DfrPayItemMapping>()
                                on payitemmaster.Id equals dfrmapping.PayItemId
                        where payitem.ActivityId == activityId &&
                                dfrmapping.DFRSection.Trim() == "B"
                        select (new
                        {
                            DfrSection = dfrmapping.DFRSection.Trim()
                        })).Count();
            }
        }

        private static int GetSectionEPayItemsCount(long activityId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {

                return (from payitem in _context.Get<RevenueActivity>()
                            .Where(RevenueActivity.IsActiveFilter)
                        join payitemmaster in _context.Get<PayItemMapping>()
                                on payitem.PayItemId equals payitemmaster.Id
                        join dfrmapping in _context.Get<DfrPayItemMapping>()
                                on payitemmaster.Id equals dfrmapping.PayItemId
                        where payitem.ActivityId == activityId &&
                                dfrmapping.DFRSection.Trim() == "E"
                        select (new
                        {
                            DfrSection = dfrmapping.DFRSection.Trim()
                        })).Count();
            }
        }

        private static bool CheckSectionBAvailability(long activityId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {

                if (workOrder.ToLower().Equals("all"))
                {
                    return (from payitem in _context.Get<RevenueActivity>()
                                    .Where(RevenueActivity.IsActiveFilter)
                            join payitemmaster in _context.Get<PayItemMapping>()
                                    on payitem.PayItemId equals payitemmaster.Id
                            join dfrmapping in _context.Get<DfrPayItemMapping>()
                                    on payitemmaster.Id equals dfrmapping.PayItemId
                            where payitem.ActivityId == activityId &&
                                    dfrmapping.DFRSection.Trim() == "B"
                            select (new
                            {
                                DfrSection = dfrmapping.DFRSection.Trim()
                            })).Any();
                }
                else
                {
                    return (from payitem in _context.Get<RevenueActivity>()
                                    .Where(RevenueActivity.IsActiveFilter)
                            join payitemmaster in _context.Get<PayItemMapping>()
                                    on payitem.PayItemId equals payitemmaster.Id
                            join dfrmapping in _context.Get<DfrPayItemMapping>()
                                    on payitemmaster.Id equals dfrmapping.PayItemId
                            where payitem.ActivityId == activityId &&
                                    payitem.WorkOrderNumber == workOrder &&
                                    dfrmapping.DFRSection.Trim() == "B"
                            select (new
                            {
                                DfrSection = dfrmapping.DFRSection.Trim()
                            })).Any();
                }
            }
        }

        private static bool CheckSectionEAvailability(long activityId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {

                if (workOrder.ToLower().Equals("all"))
                {
                    return (from payitem in _context.Get<RevenueActivity>()
                                    .Where(RevenueActivity.IsActiveFilter)
                            join payitemmaster in _context.Get<PayItemMapping>()
                                    on payitem.PayItemId equals payitemmaster.Id
                            join dfrmapping in _context.Get<DfrPayItemMapping>()
                                    on payitemmaster.Id equals dfrmapping.PayItemId
                            where payitem.ActivityId == activityId &&
                                    dfrmapping.DFRSection.Trim() == "E"
                            select (new
                            {
                                DfrSection = dfrmapping.DFRSection.Trim()
                            })).Any();
                }
                else
                {
                    return (from payitem in _context.Get<RevenueActivity>()
                                    .Where(RevenueActivity.IsActiveFilter)
                            join payitemmaster in _context.Get<PayItemMapping>()
                                    on payitem.PayItemId equals payitemmaster.Id
                            join dfrmapping in _context.Get<DfrPayItemMapping>()
                                    on payitemmaster.Id equals dfrmapping.PayItemId
                            where payitem.ActivityId == activityId &&
                                    payitem.WorkOrderNumber == workOrder &&
                                    dfrmapping.DFRSection.Trim() == "E"
                            select (new
                            {
                                DfrSection = dfrmapping.DFRSection.Trim()
                            })).Any();
                }
            }
        }

        private static void GetCommonSection(long ActivityId, int DfrId, string workOrder, ResponseData response)
        {

            using (var _context = new ApplicationContext())
            {
                var result =  (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
                                    .Where(DfrAdditionalInfoMaster.IsActiveQueryFilter)
                                    join infotrans in _context.Get<DfrAdditionalInfoTrans>()
                                         .Where(x => x.JobId == ActivityId)
                                         on infomaster.Id equals infotrans.AdditionalInfoID into lj
                                    from infotrans in lj.DefaultIfEmpty()
                                    where infomaster.DFRId == DfrId && infomaster.Section == null
                                    select (new
                                    {
                                        Id = infomaster.Id,
                                        Name = infomaster.Name,
                                        Value = infotrans == null ? null : infotrans.InfoValue == "" ? null : infotrans.InfoValue,
                                        Section = infomaster.Section
                                    })).ToList();

                response.CommonVal = result.Where(x => x.Section == null).GroupBy(x => new { x.Name })
                           .Select(x => new ResponseData.MasterData
                           {
                               Name = x.Key.Name,
                               Data = x.Select(r => new ResponseData.MasterData.DetailRecords
                               { Id = r.Id, Value = r.Value })
                           }).ToList();
            }
        }

        public static void GetBusinessType(ResponseData response)
        {
            BusinessTypeConfiguration businessType = new BusinessTypeConfiguration();

            response.ActivityType =
                        businessType
                        .Where(x => x.Type == "SPIRE_DPR_NEW_CONSTRUCTION")
                        .Select(x => new ResponseData.BusinessTypes
                        {
                            Id = x.Id,
                            Name = x.Name
                        }).ToList();
        }

        
    }
}
