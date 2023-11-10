
using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Core.Request;
using CrewlinkServices.Core.Request.Response;
using CrewlinkServices.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewlink.WindowsServices.Features
{
    public class GetSpire24DFRAdditionalInfo
    {
        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRSpire24.GetAdditionalInfo.Response
        {
            public string DfrName { get; set; }

        }
        public static ResponseData GetData(long ActivityId, int DfrId)
        {
            var response = new ResponseData();

            GetAdditionalInfo(ActivityId, response, DfrId );

            return response;
        }

        public static void GetAdditionalInfo(long activityId, ResponseData response, int dfrId)
        {
            using (var _context = new ApplicationContext())
            {

                var additionalInputStandard = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
                          .Where(DfrAdditionalInfoMaster.IsActiveQueryFilter)
                                               join infotrans in _context.Get<DfrAdditionalInfoTrans>()
                                                   .Where(x => x.JobId == activityId)
                                                   on infomaster.Id equals infotrans.AdditionalInfoID into lj
                                               from infotrans in lj.DefaultIfEmpty()
                                               where infomaster.DFRId == dfrId
                                               select (new
                                               {
                                                   Id = infomaster.Id,
                                                   Name = infomaster.Name,
                                                   Value = infotrans == null ? null : infotrans.InfoValue,
                                                   Section = infomaster.Section.Trim(),
                                                   DisplayOrder = infomaster.DisplayOrder
                                               })).OrderBy(x => x.DisplayOrder).ToList();


                response.AdditionalInfo = additionalInputStandard.GroupBy(x => new { x.Name })
                            .Select(x => new ResponseData.MasterData
                            {
                                Name = x.Key.Name,
                                Data = x.Select(r => new ResponseData.MasterData.DetailRecords
                                {
                                    Id = r.Id,
                                    Value = r.Value
                                }).ToList()
                            }).ToList();

                response.DfrId = dfrId;

                response.DfrName = SharedDFRDataRepository.GetDfrName(dfrId);
            }
        }

    }
}
