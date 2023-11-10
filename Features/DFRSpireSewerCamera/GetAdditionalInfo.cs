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
    public class GetSpireSewerCameraDFRAdditionalInfo
    {
        public class ResponseData : CrewlinkServices.Features.DailyActivity.DFRSpireSewerCamera.GetAdditionalInfo.Response
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
                var additionalInput = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
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
                                           TransId = infotrans == null ? Guid.Empty : infotrans.Id,
                                           Section = infomaster.Section.Trim(),
                                           DisplayOrder = infomaster.DisplayOrder,
                                           ItemOrder = infotrans == null ? null : infotrans.ItemOrder
                                       })).OrderBy(x => x.DisplayOrder).ToList();

                var countyValue = additionalInput
                    .Where(x => x.Name.ToLower() == "county")
                    .Select(t => new ResponseData.MasterData.Details
                    {
                        Id = t.Id,
                        Value = t.Value,
                        Name = t.Name
                    }).FirstOrDefault();

                var comments = additionalInput
                    .Where(x => x.Name.ToLower() == "comments")
                    .Select(t => new ResponseData.MasterData.Details
                    {
                        Id = t.Id,
                        Value = t.Value,
                        Name = t.Name
                    }).FirstOrDefault();

                var phaseNumber = additionalInput
                    .Where(x => x.Name.ToLower() == "phase")
                    .Select(t => new ResponseData.MasterData.Details
                    {
                        Id = t.Id,
                        Value = t.Value,
                        Name = t.Name
                    }).FirstOrDefault();

                var inspector = additionalInput
                    .Where(x => x.Name.ToLower() == "inspector")
                    .Select(t => new ResponseData.MasterData.Details
                    {
                        Id = t.Id,
                        Value = t.Value,
                        Name = t.Name
                    }).FirstOrDefault();

                var truckid = additionalInput
                    .Where(x => x.Name.ToLower() == "truckid")
                    .Select(t => new ResponseData.MasterData.Details
                    {
                        Id = t.Id,
                        Value = t.Value,
                        Name = t.Name
                    }).FirstOrDefault();

                var activityType = additionalInput
                    .Where(x => x.Name.ToLower() == "activitytype")
                    .Select(t => new ResponseData.MasterData.Details
                    {
                        Id = t.Id,
                        Value = t.Value,
                        Name = t.Name
                    }).FirstOrDefault();

                response.AdditionalInfo.Comments = comments;
                response.AdditionalInfo.County = countyValue;
                response.AdditionalInfo.TruckId = truckid;
                response.AdditionalInfo.Inspector = inspector;
                response.AdditionalInfo.PhaseNumber = phaseNumber;
                response.AdditionalInfo.ActivityType = activityType;

                var mainLocations = additionalInput.Where(x => x.Name.ToLower() == "location" && x.Section.ToLower() == "main");
                var mainLocationFeet = additionalInput.Where(x => x.Name.ToLower() == "feet" && x.Section.ToLower() == "main");
                var lateralLocations = additionalInput.Where(x => x.Name.ToLower() == "location" && x.Section.ToLower() == "lateral");
                var lateralLocationFeet = additionalInput.Where(x => x.Name.ToLower() == "feet" && x.Section.ToLower() == "lateral");

                response.AdditionalInfo.MainLocations = (from location in mainLocations
                                                         join feet in mainLocationFeet
                                                         on location.ItemOrder equals feet.ItemOrder
                                                         select new ResponseData.MasterData.LocationDetails
                                                         {
                                                             Id = location.Id + "_" + feet.Id + "_" + location.ItemOrder,
                                                             Location = new ResponseData.MasterData.Details
                                                             {
                                                                 Id = location.TransId,
                                                                 Value = location.Value
                                                             },
                                                             Feet = new ResponseData.MasterData.Details
                                                             {
                                                                 Id = feet.TransId,
                                                                 Value = feet.Value
                                                             },
                                                             ItemOrder = location.ItemOrder
                                                         })
                                                        .OrderBy(x => x.ItemOrder)
                                                        .ToList();

                response.AdditionalInfo.LateralLocations = (from location in lateralLocations
                                                            join feet in lateralLocationFeet
                                                            on location.ItemOrder equals feet.ItemOrder
                                                            select new ResponseData.MasterData.LocationDetails
                                                            {
                                                                Id = location.Id + "_" + feet.Id + "_" + location.ItemOrder,
                                                                Location = new ResponseData.MasterData.Details
                                                                {
                                                                    Id = location.TransId,
                                                                    Value = location.Value
                                                                },
                                                                Feet = new ResponseData.MasterData.Details
                                                                {
                                                                    Id = feet.TransId,
                                                                    Value = feet.Value
                                                                },
                                                                ItemOrder = location.ItemOrder
                                                            })
                                                        .OrderBy(x => x.ItemOrder)
                                                        .ToList();

                response.DfrId = DfrId;

                response.DfrName = SharedDFRDataRepository.GetDfrName(DfrId);
            }
        }

        public static void GetBusinessType(ResponseData response, BusinessTypeConfiguration businessType)
        {
            response.ActivityType =
                        businessType
                        .Where(x => x.Type == "SPIRE_SEWER_CAMERA")
                        .Select(x => new ResponseData.BusinessTypes
                        {
                            Id = x.Id,
                            Name = x.Name
                        }).ToList();
        }
    }

    }
