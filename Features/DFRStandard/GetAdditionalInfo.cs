
using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Features.DailyActivity;
using CrewlinkServices.Core.Request.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewlink.WindowsServices.Features
{
    public class GetStandardAdditionalInfo
    {

        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRStandard.GetAdditionalInfo.Response
        {
            public string DfrName { get; set; }

        }
        public static ResponseData GetData(long ActivityId, int DfrId)
        {
            var response = new ResponseData();
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

                response.DfrId = DfrId;

                response.DfrName = SharedDFRDataRepository.GetDfrName(DfrId);

                return response;
            }
        }



    }
}
