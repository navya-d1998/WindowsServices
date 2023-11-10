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
    public class GetHourlyWorkDFRAdditionalInfo
    {
        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRHourlyWork.GetAdditionalInfo.Response
        {
            public string DfrName { get; set; }

        }
        public static ResponseData GetData(long ActivityId, int DfrId)
        {
            var response = new ResponseData();

            var businessType = new BusinessTypeConfiguration();

            GetAdditionalInfo(ActivityId, response, DfrId);

            GetBusinessType(response, businessType);

            return response;
        }
        private static void GetAdditionalInfo(long ActivityId, ResponseData response, int DfrId)
        {
            using (var _context = new ApplicationContext())
            {
                var payitemMappingTandM = new TandMPayitemMappingConfiguration();

                var jobPayitem = _context
                                 .Get<RevenueActivity>()
                                 .Where(RevenueActivity.IsActiveFilter)
                                 .Where(x => x.ActivityId == ActivityId)
                                 .Select(x => new
                                 {
                                     JobPayitem = x.PayItem.PayItemCode
                                 }).ToList();

                var jobPayitemMapping = (from pm in payitemMappingTandM
                                         join lp in jobPayitem on pm.Payitem equals lp.JobPayitem
                                         select (new { pm.DisplayName })).ToList().Distinct();

                var additionalInputTandM = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
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
                                                PayCodeId = infotrans.PayitemId,
                                                ItemOrder = infotrans == null ? null : infotrans.ItemOrder
                                            })).OrderBy(x => x.DisplayOrder).ToList();

                var commonInput = additionalInputTandM.Where(x => x.Section == null || x.Section == "");

                response.AdditionalInfo = commonInput.GroupBy(x => new { x.Name })
                            .Select(x => new ResponseData.MasterData
                            {
                                Name = x.Key.Name,
                                Data = x.Select(r => new ResponseData.MasterData.DetailRecords
                                {
                                    Id = r.Id,
                                    Value = r.Value
                                }).ToList()
                            }).ToList();

                var result = (from ainp in additionalInputTandM
                              join jpmap in jobPayitemMapping on ainp.Name equals jpmap.DisplayName
                              select (new
                              {
                                  Id = ainp.Id,
                                  Name = ainp.Name,
                                  Value = ainp.Value,
                                  Section = ainp.Section,
                                  DisplayOrder = ainp.DisplayOrder == null ? 7 : ainp.DisplayOrder
                              })).ToList();

                var groupingresult = result.GroupBy(x => new { x.Name, x.DisplayOrder, x.Section })
                                .Select(x => new ResponseData.MasterData
                                {
                                    Name = x.Key.Name,
                                    DisplayOrder = x.Key.DisplayOrder,
                                    Section = x.Key.Section,
                                    Data = x.Select(r => new ResponseData.MasterData.DetailRecords
                                    {
                                        Id = r.Id,
                                        Value = r.Value
                                    }).ToList()
                                }).ToList();

                var TandMnote = additionalInputTandM.Where(x => x.Name.ToLower() == "time_material_note");
                var TandMaddress = additionalInputTandM.Where(x => x.Name.ToLower() == "time_material_address");
                var TandMworkOrder = additionalInputTandM.Where(x => x.Name.ToLower() == "time_material_work_order");

                response.PayCodeNotes = (from note in TandMnote
                                         join address in TandMaddress
                                                         on note.ItemOrder equals address.ItemOrder
                                         join workOrder in TandMworkOrder
                                                         on note.ItemOrder equals workOrder.ItemOrder
                                         select new ResponseData.PayCodeDetails
                                         {
                                             Id = note.Id + "_" + address.Id + "_" + note.ItemOrder,

                                             JobPayItemId = note.PayCodeId,

                                             StreetAddress = new ResponseData.Details
                                             {
                                                 Id = address.Id,
                                                 Value = address.Value
                                             },
                                             Note = new ResponseData.Details
                                             {
                                                 Id = note.Id,
                                                 Value = note.Value
                                             },
                                             WorkOrderNumber = new ResponseData.Details
                                             {
                                                 Id = workOrder.Id,
                                                 Value = workOrder.Value
                                             },
                                             ItemOrder = note.ItemOrder,
                                         })
                                                    .OrderBy(x => x.ItemOrder)
                                                    .ToList();
                response.LaborInfo = groupingresult.Where(x => x.Section == "L-P" || x.Section == "L-S").OrderBy(y => y.DisplayOrder).ThenBy(y => y.Name);

                response.EquipmentInfo = groupingresult.Where(x => x.Section == "E-P" || x.Section == "E-S").OrderBy(y => y.DisplayOrder).ThenBy(y => y.Name);

                response.DfrId = DfrId;

                response.DfrName = SharedDFRDataRepository.GetDfrName(DfrId);
            }
        }

            public static void GetBusinessType(ResponseData response, BusinessTypeConfiguration businessType)
            {
                response.ActivityType =
                            businessType
                            .Where(x => x.Type == "SPIRE_DPR_HOURLY_WORK")
                            .Select(x => new ResponseData.BusinessTypes
                            {
                                Id = x.Id,
                                Name = x.Name
                            }).ToList();
            }
        }
}
