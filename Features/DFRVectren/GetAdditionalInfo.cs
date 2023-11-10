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
    public class GetVectrenDFRAdditionalInfo
    {
        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRVectren.GetAdditionalInfo.Response
        {
            public string DfrName { get; set; }

        }

        public static ResponseData GetData(long ActivityId, int DfrId)
        {
            var response = new ResponseData();

            GetCommonSection(ActivityId, response, DfrId);

            response.DfrId = DfrId;

            response.DfrName = SharedDFRDataRepository.GetDfrName(DfrId);


            return response;
        }
        public static  void GetCommonSection(long ActivityId, ResponseData response, int DfrId)
        {
            using (var _context = new ApplicationContext())
            {

                var result = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
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

                response.AdditionalInfo = result.Where(x => x.Section == null).GroupBy(x => new { x.Name })
                           .Select(x => new ResponseData.CommonData
                           {
                               Name = x.Key.Name,
                               Data = x.Select(r => new ResponseData.CommonData.DetailRecords
                               { Id = r.Id, Value = r.Value })
                           }).ToList();

                var inputType = new AdditionalInputTypeConfiguration();

                foreach (var item in response.AdditionalInfo)
                {
                    item.Options = inputType
                        .Where(x => x.Type == item.Name)
                        .Select(x => new ResponseData.CommonData.DropdownOptions
                        {
                            Id = x.Id,
                            Value = x.Value,
                            Type = x.Type
                        }).ToList();
                }
            }

        }
            }
}
