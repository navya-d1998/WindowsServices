﻿using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewlink.WindowsServices.Features
{
    public class GetWglBundleDFRAdditionalInfo
    {
        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRWglBundle.GetAdditionalInfo.Response
        {
            public string DfrName { get; set; }

        }

        public static ResponseData GetData(long ActivityId, int DfrId)
        {
            var response = new ResponseData();

            GetAdditionalInfo(ActivityId, response, DfrId);

            return response;
        }

        public static void GetAdditionalInfo(long ActivityId, ResponseData response, int DfrId)
        {
            using (var _context = new ApplicationContext())
            {
                var additionalInputWgl = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
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
                                                   ItemOrder = infotrans.ItemOrder
                                               })).OrderBy(x => x.DisplayOrder).ToList();


                response.AdditionalInfo = additionalInputWgl.GroupBy(x => new { x.Name })
                            .Select(x => new ResponseData.MasterData
                            {
                                Name = x.Key.Name,
                                Data = x.Select(r => new ResponseData.MasterData.DetailRecords
                                {
                                    Id = r.Id,
                                    Value = r.Value,
                                    ItemOrder = r.ItemOrder
                                }).ToList()
                            }).ToList();

                response.DfrId = DfrId;

                response.DfrName = SharedDFRDataRepository.GetDfrName(DfrId);
            }
        }


            }
}
