using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.Caching;
using CrewlinkServices.Core.Crypto;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Enums;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Features.DailyActivity;
using CrewlinkServices.Features.DailyActivity.Shared;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Crewlink.WindowsServices.Features
{
   public class GetMGENewConstructionData
    {

        public class ResponseData : BaseActivityQueryResponse
        {
            public ResponseData()
            {
                ResponseType = CrewlinkServices.Core.Request.Response.ResponseType.File;
            }

            public DateTime ActivityDate { get; set; }

            public string JobNumber { get; set; }

            public string ContractNumber { get; set; }

            public string Town { get; set; }

            public string County { get; set; }

            public string Foreman { get; set; }

            public string LogoImagePath { get; set; }

            public int SectionFCount { get; set; }

            public int CommentsCount { get; set; }

            public string ForemanSignature { get; set; }

            public string InspectorSignature { get; set; }

            public string ProcessDateAndTime { get; set; }

            public int UserId { get; set; }

            public string PayitemComment { get; set; }

            public string WorkOrder { get; set; }

            public String Separator { get; set; } = ",";

            public IEnumerable<Record> Records { get; set; }

            public string TickImagePath { get; set; }

            public string Length1 { get; set; }

            public string Depth1 { get; set; }

            public string PipeSize1 { get; set; }

            public string Width1 { get; set; }

            public string SectionBSubtype1 { get; set; }

            public string Length2 { get; set; }

            public string Depth2 { get; set; }

            public string PipeSize2 { get; set; }

            public string Width2 { get; set; }

            public string SectionBSubtype2 { get; set; }

            public string Length3 { get; set; }

            public string Depth3 { get; set; }

            public string PipeSize3 { get; set; }

            public string Width3 { get; set; }

            public string SectionBSubtype3 { get; set; }

            public string Length4 { get; set; }

            public string Depth4 { get; set; }

            public string PipeSize4 { get; set; }

            public string Width4 { get; set; }

            public string SectionBSubtype4 { get; set; }

            public string Length5 { get; set; }

            public string Depth5 { get; set; }

            public string PipeSize5 { get; set; }

            public string Width5 { get; set; }

            public string SectionBSubtype5 { get; set; }

            public decimal ProjectLength { get; set; } = 0;

            public string BusinessType { get; set; }

            public CrewlinkServices.Features.DailyActivity.DFRNewConstruction.GetAdditionalInfo.Response DFRAdditionaInfo { get; set; }

            public class Record
            {
                public string DfrId { get; set; }

                public string DfrMainSection { get; set; }

                public string DfrSection { get; set; }

                public string DfrName { get; set; }

                public string PatItem { get; set; }

                public string Tier1Description { get; set; }

                public string Tier2Description { get; set; }

                public string Tier2ActualDescription { get; set; }

                public string Tier3Description { get; set; }

                public string UOM { get; set; }

                public string PipeType { get; set; }

                public string PipeSize { get; set; }

                public string PayItemDescription { get; set; }

                public decimal Qty { get; set; }

                public string SubTotal { get; set; }

                public string WorkOrder { get; set; }

                public string Tier1Code { get; set; }

                public string Tier2Code { get; set; }

                public string Tier3Code { get; set; }

                public string StreetAddress { get; set; }

                public DateTime CreatedOn { get; set; }

                public string PipeDia { get; set; }
            }

            public IEnumerable<FRecord> FRecords { get; set; }

            public class FRecord
            {
                public string PatItem { get; set; }

                public string StreetAddress { get; set; }

                public string WorkOrder { get; set; }

                public string PipeType { get; set; }

                public string Tier2Description { get; set; }

                public decimal Total { get; set; }

                public Dictionary<SectionType, decimal?> SectionValues { get; set; }
            }

            public IEnumerable<Record> GetDataForSection(string section, IEnumerable<Record> records)
            {
                var filteredRecords = records.Where(x => x.DfrSection == section);
                return filteredRecords;
            }

            public IEnumerable<Record> GetDataForSections(string[] sections, IEnumerable<Record> records)
            {
                var filteredRecords = records.Where(x => sections.Contains(x.DfrSection)).OrderBy(x => x.DfrSection);
                return filteredRecords;
            }

            public decimal GetDataGrouping(string section, IEnumerable<Record> records)
            {
                var filteredGroups = records.Where(x => x.DfrSection == section).Sum(x => x.Qty);
                return filteredGroups;
            }
        }

        public ResponseData ExecuteRequest(string token)
        {
            try
            {

                var _crypto = new Crypto();
                var _cache = new HttpCacheStore();
                ////Change back to Base64 and remove bearer section.
                string converted = token.Replace('-', '+');
                converted = converted.Replace('_', '/');
                converted = converted.Replace("bearer ", "");

                string cacheKeyValue = _crypto.Decryption(token);

                var request = _cache.GetItem<GetDFRToken.Request>(cacheKeyValue);

                _cache.Clear(cacheKeyValue);

                ////If not in cache (should never happen unless testing manually), try to get info from tempkey.
                if (request == null)
                {
                    if (cacheKeyValue != null &&
                        cacheKeyValue != string.Empty &&
                        cacheKeyValue.Split('_').Count() == 13)
                    {
                        string[] SplitKey = cacheKeyValue.Split('_');
                        request = new GetDFRToken.Request();
                        request.FileName = SplitKey[0] + "_" + SplitKey[1] + "_" + SplitKey[2] + "_" + SplitKey[3] + "_" + SplitKey[4];
                        request.ActivityId = long.Parse(SplitKey[5]);
                        request.ProcessDateAndTime = SplitKey[6] + " " + SplitKey[7];
                        request.ShowPayitem = bool.Parse(SplitKey[8]);
                        request.ShowLabor = bool.Parse(SplitKey[9]);
                        request.ShowEquipment = bool.Parse(SplitKey[10]);
                        request.ShowSignature = bool.Parse(SplitKey[11]);
                        request.WorkOrderNumber = SplitKey[12].Trim();
                        _cache.Insert(cacheKeyValue, request, 11);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();

                 GetJobRecord(request.ActivityId, response);

                 GetMappingRecord(request.ActivityId, response, request.WorkOrderNumber.Trim());

                DataBinding(response, request.WorkOrderNumber.Trim());

                 GetAdditionalInfo(request.ActivityId, response, request.WorkOrderNumber.Trim());

                int CurrentDFRId =  SharedDFRDataRepository.GetDfrId("SPIRE_DPR_NEW_CONSTRUCTION");

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string baseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");

               // string baseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "..\\..\\..\\Crewlink.Services\\Features\\DailyActivity\\Templates\\");

                string templateURL = baseURL + "DFR_MGE_NewConstruction.cshtml";

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(baseURL + "Images\\ifs-logo-bw.png");


                var dfrCurrentData = Razor.Parse(File.ReadAllText(templateURL), response);

                if (request.WorkOrderNumber.Trim().ToLower().Equals("all"))
                {
                    var CurrentHashData = FileProcess.CalculateMD5Hash(dfrCurrentData);

                    var ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);

                    if (string.IsNullOrEmpty(ArchivedHashData))
                    {
                         SharedDFRDataRepository.SaveDFRData(request.ActivityId, CurrentDFRId, CurrentHashData, response.UserId);
                    }
                    else
                    {
                        if (!CurrentHashData.Equals(ArchivedHashData))
                        {
                             SharedDFRDataRepository.InvalidateSignature(request.ActivityId, CurrentDFRId);

                             SharedDFRDataRepository.UpdateDFRData(request.ActivityId, CurrentDFRId, CurrentHashData, response.UserId);

                            request.ShowSignature = false;
                        }
                    }
                }

                if (request.ShowSignature)
                {
                    BindSignature(request.ActivityId, baseURL, response);
                }

                response.FileName = request.FileName.ToString();

                response.ProcessDateAndTime = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();

                var dfrTemplate = Razor.Parse(File.ReadAllText(templateURL), response);

                response.FileContent = dfrTemplate;

                return response;
            }
            catch (Exception e)
            {
                string fullexceptiondetails = e.Message + "||||" + e.StackTrace + "||||" + e.InnerException;
                throw new Exception(fullexceptiondetails);
            }
        }
        public static void GetAdditionalInfo(long activityId, ResponseData response, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {
                int CurrentDFRId = SharedDFRDataRepository.GetDfrId("SPIRE_DPR_NEW_CONSTRUCTION");

                response.DFRAdditionaInfo = GetMGENewConstructionDFRAdditionalInfo.GetData(activityId, CurrentDFRId, workOrder);

                if (response.DFRAdditionaInfo.SectionAVal != null)
                {
                    try
                    {
                        if (!workOrder.ToLower().Equals("all"))
                        {
                            var result =  _context.Get<RevenueActivity>()
                                .Where(RevenueActivity.IsActiveFilter)
                                .Where(x => x.ActivityId == activityId)
                                .Where(x => x.WorkOrderNumber == workOrder)
                                .Select(x => x.Id).ToList();

                            response.DFRAdditionaInfo.SectionAVal = response.DFRAdditionaInfo.SectionAVal.Where(x => result.Contains(x.PayitemId)).ToList();
                        }

                        foreach (var item in response.DFRAdditionaInfo.SectionAVal)
                        {
                            response.ProjectLength += item.Records.Where(x => x.Name == "LengthOfProject").Select(y => decimal.Parse(string.IsNullOrWhiteSpace(y.Value) ? "0" : y.Value)).Sum();
                        }
                    }
                    catch { response.ProjectLength = 0; }
                }

                if (response.DFRAdditionaInfo.SectionBVal != null)
                {
                    response.Length1 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Length1")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Depth1 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Depth1")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.PipeSize1 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("PipeSize1")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Width1 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Width1")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.SectionBSubtype1 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("SectionBSubtype1")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();

                    response.Length2 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Length2")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Depth2 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Depth2")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.PipeSize2 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("PipeSize2")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Width2 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Width2")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.SectionBSubtype2 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("SectionBSubtype2")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();

                    response.Length3 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Length3")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Depth3 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Depth3")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.PipeSize3 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("PipeSize3")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Width3 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Width3")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.SectionBSubtype3 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("SectionBSubtype3")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();

                    response.Length4 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Length4")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Depth4 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Depth4")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.PipeSize4 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("PipeSize4")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Width4 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Width4")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.SectionBSubtype4 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("SectionBSubtype4")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();

                    response.Length5 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Length5")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Depth5 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Depth5")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.PipeSize5 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("PipeSize5")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.Width5 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("Width5")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                    response.SectionBSubtype5 = response.DFRAdditionaInfo.SectionBVal.Where(x => x.Name.Contains("SectionBSubtype5")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
                }
                response.BusinessType = response.DFRAdditionaInfo.CommonVal.Where(x => x.Name.Contains("ActivityType")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
            }
        }

        public static void BindSignature(long activityId, string baseURL, ResponseData response)
        {
            var signatures = SharedDFRDataRepository.GetSignature(activityId, 2);

            if (signatures != null)
            {
                string PartialPath = baseURL + "Images\\Temp\\" + response.FileName;

                foreach (var signature in signatures)
                {
                    if (signature.UserType.Equals(0))
                    {
                        response.ForemanSignature = PartialPath + "_0.png";
                        FileProcess.SaveBLOBAsImage(response.ForemanSignature, signature.ESignature);
                    }
                    else if (signature.UserType.Equals(1))
                    {
                        response.InspectorSignature = PartialPath + "_1.png";
                        FileProcess.SaveBLOBAsImage(response.InspectorSignature, signature.ESignature);
                    }
                }
            }
        }

        public static void GetJobRecord(long ActivityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobdetails =  (from activity in _context.Get<Activity>()
                                        join jobmaster in _context.Get<CrewlinkServices.Core.Models.Job>()
                                        on activity.JobId equals jobmaster.Id
                                        //join revenue in _context.Get<Core.Models.RevenueActivity>()
                                        //on activity.JobId equals revenue.ActivityId
                                        where activity.Id == ActivityId
                                        select (new ResponseData
                                        {
                                            ActivityDate = activity.ActivityDate,
                                            Foreman = activity.Foreman.Employee.EmployeeName,
                                            JobNumber = activity.Job.JobNumber,
                                            ContractNumber = activity.Job.ContractNumber,
                                            Town = activity.RevenueActivity.Select(c => c.City.CityCode).FirstOrDefault(),
                                            County = activity.RevenueActivity.Select(s => s.City.StateCode).FirstOrDefault(),
                                            UserId = activity.ForemanUserId,
                                            PayitemComment = activity.Comments.Where(x => x.Type == "P").Select(x => x.Comment).FirstOrDefault()
                                        })).FirstOrDefault();

                response.ActivityDate = jobdetails.ActivityDate;
                response.Foreman = jobdetails.Foreman;
                response.JobNumber = jobdetails.JobNumber;
                response.ContractNumber = jobdetails.ContractNumber;
                response.Town = jobdetails.Town;
                response.County = jobdetails.County;
                response.UserId = jobdetails.UserId;
                response.PayitemComment = jobdetails.PayitemComment;
            }
        }
        public class SectionEVal
        {
            public string PayItemId { get; set; }
            public string PayItemName { get; set; }
            public string PipeSize { get; set; }
        }
        public static void GetMappingRecord(long activityId, ResponseData response, string workOrderNumber)
        {
            using (var _context = new ApplicationContext())
            {
                string[] Tier2Code = { "07", "08", "09", "10", "11", "12", "16" };
                int counter = 0;
                var SecEVal = new List<SectionEVal>();

                var addInfo = (from additionalInfoTrans in _context.Get<DfrAdditionalInfoTrans>()
                               join additionalInfoMast in _context.Get<DfrAdditionalInfoMaster>() on additionalInfoTrans.AdditionalInfoID equals additionalInfoMast.Id
                               where additionalInfoTrans.JobId == activityId
                               where additionalInfoMast.Section == "E"
                               orderby additionalInfoMast.Name
                               select new
                               {
                                   PayitemId = additionalInfoTrans.PayitemId,
                                   InfoValue = additionalInfoTrans.InfoValue,
                                   JobId = additionalInfoTrans.JobId,
                                   ItemOrder = additionalInfoTrans.ItemOrder
                               }).ToArray();


                var jobinfo =  (from payitem in _context.Get<RevenueActivity>().Where(RevenueActivity.IsActiveFilter)
                                     join payitemmaster in _context.Get<PayItemMapping>() on payitem.PayItemId equals payitemmaster.Id
                                     join dfrmapping in _context.Get<DfrPayItemMapping>() on payitemmaster.Id equals dfrmapping.PayItemId
                                     where payitem.ActivityId == activityId
                                     select (new ResponseData.Record
                                     {
                                         DfrId = dfrmapping.DFRId,
                                         DfrName = dfrmapping.DFRName,
                                         DfrSection = dfrmapping.DFRSection.Trim(),
                                         PatItem = payitemmaster.PayItemCode,
                                         PayItemDescription = payitemmaster.PayItemDescription,
                                         Tier1Code = payitemmaster.TierOneCode,
                                         Tier1Description = payitemmaster.TierOneDescription,
                                         Tier2Code = payitemmaster.TierTwoCode,
                                         Tier2ActualDescription = payitemmaster.TierTwoDescription,
                                         Tier2Description = payitemmaster.TierTwoDescription.ToLower().Contains("steel services")
                                                            ? payitemmaster.PayItemDescription : payitemmaster.TierTwoDescription,
                                         Tier3Code = payitemmaster.TierThreeCode,
                                         Tier3Description = payitemmaster.TierThreeDescription,
                                         UOM = payitemmaster.UnitOfMeasure,
                                         PipeType = payitemmaster.TierTwoDescription.Contains("Steel") ? "Steel" : "Plastic",
                                         Qty = payitem.Quantity,
                                         WorkOrder = payitem.WorkOrderNumber,
                                         StreetAddress = payitem.Address + ", " + payitem.City.CityCode + ", " + payitem.City.StateCode,
                                         CreatedOn = payitem.Created
                                     })).ToList();

                var payItemsList = jobinfo
                    .Where(x => x.DfrSection == "E" || x.DfrSection == "E1" || x.DfrSection == "E2" || x.DfrSection == "E3" || x.DfrSection == "E4")
                    .GroupBy(x => x.PatItem).Select(group => new { PayItem = group.Key })
                    .OrderBy(x => x.PayItem);

                var payItemIdList = (from payitem in _context.Get<RevenueActivity>()
                                     where payitem.ActivityId == activityId
                                     select new { PayItemId = payitem.Id.ToString(), WBSCode = payitem.PayItem.WbsCode, PayItem = payitem.PayItem.PayItemCode }).ToList();

                for (int count = 0; count < addInfo.Count(); count++)
                {
                    var val = addInfo.Where(x => x.ItemOrder == count)
                            .Select(x => new SectionEVal
                            {
                                PayItemId = x.InfoValue,
                                PipeSize = addInfo.Where(y => y.ItemOrder == count).Where(y => y.InfoValue.Contains("\"")).FirstOrDefault().InfoValue,
                                PayItemName = payItemIdList.Where(name => name.PayItemId == x.InfoValue).FirstOrDefault().PayItem
                            }).FirstOrDefault();

                    if (val != null)
                        SecEVal.Add(val);
                }

                foreach (var item in payItemsList)
                {
                    foreach (var ji in jobinfo)
                    {
                        ji.PipeSize = SecEVal.Where(x => x.PayItemName == ji.PatItem)?.FirstOrDefault()?.PipeSize;
                        ji.PipeDia = ji.PipeSize;
                    }

                    counter += 1;
                    if (counter >= SecEVal.Count())
                        break;
                }

                if (!workOrderNumber.ToLower().Equals("all"))
                { jobinfo = jobinfo.Where(x => x.WorkOrder == workOrderNumber).ToList(); }

                foreach (var item in jobinfo)
                {
                    var pipeType = item.Tier2Description;
                    item.Tier2Description = item.Tier2Description.ToLower().Contains("main tiein")
                                            ? item.PayItemDescription : item.Tier2Description;
                    item.PipeType = pipeType.ToLower().Contains("main tiein")
                                    ? item.Tier3Description : item.PipeType;
                    //item.PipeSize = string.Format("{0:0.##}", InchConversion(item.Tier2Description));
                    item.PipeSize = item.DfrSection == "E" || item.DfrSection == "E1" || item.DfrSection == "E2" || item.DfrSection == "E3" || item.DfrSection == "E4" ? item.PipeSize : string.Format("{0:0.##}", InchConversion(item.Tier2Description));
                    // item.SubTotal = string.Format("{0:0.##}", (Convert.ToDecimal(item.PipeSize) * item.Qty));
                    item.SubTotal = string.Format("{0:0.##}", item.Qty);

                    item.Qty = item.PatItem == "FTO" ? 25 : item.Qty;
                    //item.PipeDia = item.PipeSize + " Inch";
                    item.PipeDia = item.DfrSection == "E" || item.DfrSection == "E1" || item.DfrSection == "E2" || item.DfrSection == "E3" || item.DfrSection == "E4" ? item.PipeSize : item.PipeSize + " Inch";
                }
                response.Records = jobinfo;

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                response.TickImagePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\Images\\tick.png");
            }
        }

        public static decimal InchConversion(string Tier2Description)
        {
            try
            {
                string[] spl = Tier2Description.Split(' ');

                if (spl.Count() > 0 && spl[0].Contains("\""))
                {
                    return Convert.ToDecimal(spl[0].Replace("\"", ""));
                }// Single Space
                else if (spl.Count() >= 2 && spl[1].ToUpper().Contains("INCH"))
                {
                    return Convert.ToDecimal(spl[0]);
                } // Double Space
                else if (spl.Count() >= 3 && spl[2].ToUpper().Contains("INCH"))
                {
                    return Convert.ToDecimal(spl[0]);
                }
                return 1;
            }
            catch { return 1; }
        }

        public static void DataBinding(ResponseData response, string workOrder)
        {
          //  response.Foreman = User;

            string[] FSection = { "F2", "F3", "F4", "F5", "F6", "F8", "F9", "F11", "F14" };

            var FSectionRecords = response.Records.Where(x => FSection.Contains(x.DfrSection)).OrderBy(x => x.CreatedOn);

            if (workOrder.ToLower().Equals("all"))
            {
                response.WorkOrder = response.Records
                    .Where(x => !FSection.Contains(x.DfrSection))
                    .OrderBy(x => x.CreatedOn)
                    .Select(x => x.WorkOrder)
                    .FirstOrDefault();
            }
            else
            { response.WorkOrder = workOrder; }

            var groupedRecords = FSectionRecords
                    .GroupBy(x => new
                    {
                        x.PatItem,
                        x.StreetAddress,
                        x.WorkOrder,
                        x.PipeType,
                        x.Tier2Description
                    }, (key, values) => new ResponseData.FRecord
                    {
                        PatItem = key.PatItem,
                        StreetAddress = key.StreetAddress,
                        WorkOrder = key.WorkOrder,
                        PipeType = key.PipeType,
                        Tier2Description = key.Tier2Description,
                        SectionValues = GetSectionsDictionary(values),
                        Total = GetUOMTotal(values)
                    }).ToList();

            if (groupedRecords.Count == 1
                && groupedRecords.Exists(x => x.PatItem.Contains("FTO"))
                && groupedRecords.Exists(x => x.SectionValues.ContainsKey(SectionType.F4)))
            {
                var ftoRecord = groupedRecords.FirstOrDefault(x => x.PatItem.Contains("FTO") && x.SectionValues.ContainsKey(SectionType.F4));
                var result = new Dictionary<SectionType, decimal?>();
                foreach (var item in ftoRecord.SectionValues.Keys)
                {
                    result.Add(SectionType.F3, ftoRecord.SectionValues[item].Value);
                }
                ftoRecord.SectionValues = result;
            }

            var fRecordsToRemove = new List<ResponseData.FRecord>();

            // Display the details on the same line for FTO, F8, F9 
            if (groupedRecords.Count >= 2
                && groupedRecords.Exists(x => x.PatItem.Contains("FTO")))
            {
                foreach (var ftoRecord in groupedRecords.Where(x => x.PatItem.Contains("FTO")))
                {
                    foreach (var record in groupedRecords.Where(x => x.StreetAddress == ftoRecord.StreetAddress && x.PatItem != "FTO"))
                    {
                        foreach (var item in record.SectionValues.Keys)
                            ftoRecord.SectionValues.Add(item, record.SectionValues[item].Value);

                        fRecordsToRemove.Add(record);
                    }
                }

                foreach (var fRecordToRemove in fRecordsToRemove)
                    groupedRecords.Remove(fRecordToRemove);
            }


            // Display the details on the same line for F2DBBC, F8, F9 
            if (groupedRecords.Count >= 2
                && groupedRecords.Exists(x => x.PatItem.Contains("F2DBBC")))
            {
                foreach (var f2dbbcRecord in groupedRecords.Where(x => x.PatItem.Contains("F2DBBC")))
                {
                    foreach (var record in groupedRecords.Where(x => x.StreetAddress == f2dbbcRecord.StreetAddress && x.PatItem != "F2DBBC"))
                    {
                        foreach (var item in record.SectionValues.Keys)
                        {
                            if (!(f2dbbcRecord.SectionValues.ContainsKey(item)))
                            {
                                f2dbbcRecord.SectionValues.Add(item, record.SectionValues[item].Value);
                            }
                            else
                            {
                                f2dbbcRecord.SectionValues[item] += record.SectionValues[item].Value;
                            }
                            f2dbbcRecord.Total += record.SectionValues[item].Value;
                        }
                        fRecordsToRemove.Add(record);
                    }
                }

                foreach (var fRecordToRemove in fRecordsToRemove)
                    groupedRecords.Remove(fRecordToRemove);
            }


            response.FRecords = groupedRecords;

            response.SectionFCount = groupedRecords.Count();

            string[] CommentSections = { "H8", "H9", "H10", "H11", "H-ADA" };
            response.CommentsCount = response.Records.Where(x => CommentSections.Contains(x.DfrSection)).Count();

            var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

            response.LogoImagePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\Images\\spire-logo.png");

            response.TemplateSize = "A3";

            response.NumberOfPages = 1;
        }

        private static decimal GetUOMTotal(IEnumerable<ResponseData.Record> values)
        {
            if (!values.Count().Equals(0))
                return values
                    .Where(x => (x.UOM.ToUpper() == "FT" && !x.DfrSection.ToLower().Contains("f5") && !x.Tier2ActualDescription.ToLower().Contains("steel services")))
                    .Select(t => t.Qty).Sum();
            else return 0;
        }

        private static Dictionary<SectionType, decimal?> GetSectionsDictionary(IEnumerable<ResponseData.Record> values)
        {
            var result = new Dictionary<SectionType, decimal?>();

            foreach (var item in values)
            {
                var enumVal = ReturnSectionTypeEnum(item.DfrSection);

                decimal? itemDict = null;

                result.TryGetValue(enumVal, out itemDict);

                if (itemDict != null)
                {
                    result[enumVal] = itemDict + item.Qty;
                }
                else
                {
                    result.Add(enumVal, item.Qty);
                }
            }
            return result;
        }

        private static SectionType ReturnSectionTypeEnum(string dfrSection)
        {
            var sectType = SectionType.None;
            return sectType.GetValueFromDescription<SectionType>(dfrSection);
        }


    }
}
