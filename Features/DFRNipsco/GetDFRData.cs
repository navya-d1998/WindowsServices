using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.Caching;
using CrewlinkServices.Core.Crypto;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Features.DailyActivity;
using CrewlinkServices.Features.DailyActivity.Shared;
using RazorEngine;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Crewlink.WindowsServices.Features
{
    public class GetNipscoDFRData
    {
        public class ResponseData : BaseActivityQueryResponse
        {
            public ResponseData()
            {
                ResponseType = CrewlinkServices.Core.Request.Response.ResponseType.File;
            }

            public string SuperintendentName { get; set; }

            public string ProcessDateAndTime { get; set; }

            public string ContractorLogoPath { get; set; }

            public string IFSLogoPath { get; set; }

            public string ForemanSignature { get; set; }

            public string InspectorSignature { get; set; }

            public int UserId { get; set; }

            public string ForemanName { get; set; }

            public string ActivityNumber { get; set; }

            public DateTime ActivityDate { get; set; }

            public string ContractNumber { get; set; }

            public string WorkOrder { get; set; }

            public string PurchaseOrder { get; set; }

            public string Address { get; set; }

            public string PayitemComments { get; set; }

            public int SectionAEmptyRows { get; set; }

            public int SectionBEmptyRows { get; set; }

            public IEnumerable<long> MiscPayitems = new List<long>();

            public IEnumerable<ForemanPayitem> ForemanPayitems { get; set; } = new List<ForemanPayitem>();

            public class ForemanPayitem
            {
                public long PayitemId { get; set; }

                public string PayItemCode { get; set; }

                public string PayItemDescription { get; set; }

                public string WbsCode { get; set; }

                public string WbsDescription { get; set; }

                public decimal Qty { get; set; }

                public string UnitOfMeasure { get; set; }

                public int CityId { get; set; }

                public string Address { get; set; }

                public string CityCode { get; set; }

                public string StateCode { get; set; }

                public string WO { get; set; }

                public string PO { get; set; }

                public string PayitemType { get; set; }
            }

            public IEnumerable<ActivityPayitem> ActivityPayitems { get; set; } = new List<ActivityPayitem>();

            public class ActivityPayitem
            {
                public string PayitemCode { get; set; }

                public string UOM { get; set; }

                public string EmployeeName { get; set; }

                public string Craft { get; set; }

                public decimal StandardHours { get; set; } = 0;

                public decimal OverTimeHours { get; set; } = 0;

                public string WbsCode { get; set; }

                public string WbsDescription { get; set; }

                public int CityId { get; set; }
            }

            public IEnumerable<ActivityLabor> ActivityLabors { get; set; } = new List<ActivityLabor>();

            public class ActivityLabor
            {
                public string EmployeeName { get; set; }

                public string Craft { get; set; }

                public decimal StandardHours { get; set; } = 0;

                public decimal OverTimeHours { get; set; } = 0;
            }

            public IEnumerable<UnitItem> UnitItems { get; set; } = new List<UnitItem>();

            public IEnumerable<UnitItem> Misctems { get; set; } = new List<UnitItem>();

            public IEnumerable<UnitItem> ActivityEquipments { get; set; } = new List<UnitItem>();

            public class UnitItem
            {
                public string PayitemCode { get; set; }

                public string Craft { get; set; }

                public string UOM { get; set; }

                public decimal? Qty { get; set; } = 0;
            }
        }

        public ResponseData ExecuteRequest(string token)
        {
            try
            {
                var _crypto = new Crypto();
                var _cache = new HttpCacheStore();

                //Change back to Base64 and remove bearer section.
                string converted = token.Replace('-', '+');
                converted = converted.Replace('_', '/');
                converted = converted.Replace("bearer ", "");

                string cacheKeyValue = _crypto.Decryption(converted);

                var request = _cache.GetItem<GetDFRToken.Request>(cacheKeyValue);
                string processDate = string.Empty;

                _cache.Clear(cacheKeyValue);

                //If not in cache (should never happen unless testing manually), try to get info from tempkey.
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
                        _cache.Insert(cacheKeyValue, request, 10);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();

                string[] miscPayitems = { "98-922", "98-923", "98-924", "35-006", "35-040", "35-101", "35-200", "36-010", "36-020", "36-021", "36-022", "36-060", };

                 PopulateJobInfo(request.ActivityId, response);

                 PopulatePayitemInfo(request.ActivityId, miscPayitems, response);

                 PopulateActivityLabor(request.ActivityId, response);

                PopulateUnitItems(miscPayitems, response);

                PopulateMiscInfo(response);

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string baseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");

                string templateURL = baseURL + "DFR_Nipsco.cshtml";

                response.ContractorLogoPath = Path.Combine(baseURL, "Images\\nipsco-logo.png");

                response.IFSLogoPath = Path.Combine(baseURL, "Images\\logo2.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(baseURL + "Images\\logo2.png");


                var dfrCurrentData = Razor.Parse(File.ReadAllText(templateURL), response);

                int currentDFRId = SharedDFRDataRepository.GetDfrId("NIPSCO");

                var CurrentHashData = FileProcess.CalculateMD5Hash(dfrCurrentData);

                var ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, currentDFRId);

                if (string.IsNullOrEmpty(ArchivedHashData))
                {
                    SharedDFRDataRepository.SaveDFRData(request.ActivityId, currentDFRId, CurrentHashData, response.UserId);
                }
                else if (!CurrentHashData.Equals(ArchivedHashData))
                {
                     SharedDFRDataRepository.InvalidateSignature(request.ActivityId, currentDFRId);

                    SharedDFRDataRepository.UpdateDFRData(request.ActivityId, currentDFRId, CurrentHashData, response.UserId);

                    request.ShowSignature = false;
                }

                if (request.ShowSignature)
                {
                    BindSignature(request.ActivityId, currentDFRId, baseURL, response);
                }

                response.ProcessDateAndTime = request.ProcessDateAndTime;

                var dfrTemplate = Razor.Parse(File.ReadAllText(templateURL), response);

                response.FileContent = dfrTemplate;

                response.TemplateSize = "A4";

                return response;
            }
            catch (Exception e)
            {
                string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                throw new Exception(fullexceptiondetails);
            }
        }

        public static void PopulateJobInfo(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobinfo = 
                            (from a in _context.Get<Activity>()
                             join j in _context.Get<Job>() on a.JobId equals j.Id
                             join s in _context.Get<Employee>() on j.SuperitendentEmployeeNumber equals s.EmployeeNumber
                             join u in _context.Get<User>() on a.ForemanUserId equals u.Id
                             join f in _context.Get<Employee>() on u.EmployeeId equals f.Id
                             where a.Id == activityId
                             select new
                             {
                                 activityDate = a.ActivityDate,
                                 suprintendentName = s.EmployeeName,
                                 foremanName = f.EmployeeName,
                                 contractNumber = a.Job.ContractNumber,
                                 UserId = a.ForemanUserId,
                                 JobNumber = j.JobNumber
                             }).First();

                response.ActivityNumber = jobinfo.JobNumber;
                response.ActivityDate = jobinfo.activityDate;
                response.ContractNumber = jobinfo.contractNumber;
                response.ForemanName = jobinfo.foremanName;
                response.SuperintendentName = jobinfo.suprintendentName;
                response.UserId = jobinfo.UserId;
                response.PayitemComments = _context.Get<ActivityComment>().Where(x => x.ActivityId == activityId && x.Type == "P")
                                                        .Select(x => x.Comment).FirstOrDefault();
            }
        }

        public static void PopulatePayitemInfo(long activityId, string[] miscPayitems, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var payitemMaster = _context
                                       .Get<RevenueActivity>()
                                       .Where(RevenueActivity.IsActiveFilter)
                                       .AsNoTracking()
                                       .Where(x => x.ActivityId == activityId)
                                       .Select(x => new ResponseData.ForemanPayitem
                                       {
                                           PayitemId = x.PayItem.Id,
                                           PayItemCode = x.PayItem.PayItemCode,
                                           PayItemDescription = x.PayItem.PayItemDescription,
                                           WbsCode = x.PayItem.WbsCode,
                                           WbsDescription = x.PayItem.WbsDescription,
                                           Qty = x.Quantity,
                                           UnitOfMeasure = x.PayItem.UnitOfMeasure,
                                           CityId = x.CityId,
                                           Address = x.Address,
                                           CityCode = x.City.CityCode,
                                           StateCode = x.City.StateCode,
                                           WO = x.WorkOrderNumber,
                                           PO = x.PurchaseOrderNumber
                                       })
                                       .ToList();

                response.ForemanPayitems = payitemMaster;

                response.Address = payitemMaster.Select(x => x.Address + "-" + x.CityCode + "-" + x.StateCode).FirstOrDefault();

                response.WorkOrder = payitemMaster.Select(x => x.WO).FirstOrDefault();

                response.PurchaseOrder = payitemMaster.Select(x => x.PO).FirstOrDefault();

                var result =  _context
                                           .Get<PayItemMapping>()
                                           .Where(PayItemMapping.IsActiveFilter)
                                           .AsNoTracking()
                                           .Where(x => miscPayitems.Contains(x.PayItemCode))
                                           .Where(x => x.ContractNumber == "NIPSCO CASINGS 18")
                                           .Select(x => new
                                           {
                                               x.Id
                                           }).ToList();

                response.MiscPayitems = result.Select(x => x.Id).ToArray();
            }
        }

        public static void PopulateActivityLabor(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {

                var activityPayitem =  _context
                                       .Get<LaborActivity>()
                                       .Where(LaborActivity.IsActiveQueryFilter)
                                       .AsNoTracking()
                                       .Where(x => x.ActivityId == activityId)
                                       .Select(x => new ResponseData.ActivityPayitem
                                       {
                                           EmployeeName = x.CrewMember.Employee.EmployeeName,
                                           StandardHours = x.StandardHours,
                                           OverTimeHours = x.OvertimeHours,
                                           WbsCode = x.WbsCode,
                                           WbsDescription = x.WbsDescription,
                                           CityId = x.CityId
                                       })
                                       .ToList();

                foreach (var item in activityPayitem)
                {
                    var result = (from p in response.ForemanPayitems
                                  where p.WbsCode == item.WbsCode && p.WbsDescription == item.WbsDescription && p.CityId == item.CityId
                                  select new
                                  {
                                      p.PayItemCode,
                                      p.PayItemDescription,
                                      p.UnitOfMeasure
                                  }).FirstOrDefault();
                    item.PayitemCode = result.PayItemCode;
                    item.Craft = result.PayItemDescription;
                    item.UOM = result.UnitOfMeasure;
                }

                response.ActivityPayitems = activityPayitem;

                 PopulateLabor(activityId, response);

            }
        }

        public static void PopulateLabor(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {

                var result = response.ActivityPayitems
                    .Where(x => x.StandardHours > 0)
                    .Where(x => x.OverTimeHours > 0)
                    .GroupBy(x => new { x.EmployeeName, x.Craft })
                    .Select(x => new ResponseData.ActivityLabor
                    {
                        EmployeeName = x.First().EmployeeName,
                        Craft = x.First().Craft,
                        StandardHours = x.Sum(a => a.StandardHours),
                        OverTimeHours = x.Sum(b => b.OverTimeHours)
                    }).ToList();

                int nipscoDFRId = SharedDFRDataRepository.GetDfrId("NIPSCO");

                var additionalInput = (from infomaster in _context.Get<DfrAdditionalInfoMaster>()
                                            .Where(DfrAdditionalInfoMaster.IsActiveQueryFilter)
                                       join infotrans in _context.Get<DfrAdditionalInfoTrans>()
                                       .Where(x => x.JobId == activityId)
                                       on infomaster.Id equals infotrans.AdditionalInfoID into lj
                                       from infotrans in lj.DefaultIfEmpty()
                                       where infomaster.DFRId == nipscoDFRId
                                       select (new
                                       {
                                           Id = infomaster.Id,
                                           Name = infomaster.Name,
                                           Value = infotrans == null ? null : infotrans.InfoValue == "" ? null : infotrans.InfoValue,
                                           Section = infomaster.Section
                                       })).ToList();

                var engineerST = response.ForemanPayitems.Where(x => x.PayItemCode == "98-107").Select(x => x.Qty).Sum();

                List<ResponseData.ActivityLabor> AdditionalInput = new List<ResponseData.ActivityLabor>()
                {
                    new ResponseData.ActivityLabor(){ EmployeeName="", Craft = "Project Engineer", StandardHours = 0, OverTimeHours = 0 },
                    new ResponseData.ActivityLabor(){ EmployeeName="", Craft = "Superior Sub", StandardHours = 0, OverTimeHours = 0 }
                };

                foreach (var item in AdditionalInput)
                {
                    var searchResult = additionalInput.Where(x => x.Section == item.Craft);

                    if (searchResult != null)
                    {
                        item.EmployeeName = searchResult.Where(x => x.Name == item.Craft.Replace(" ", "_")).Select(x => x.Value).FirstOrDefault();
                        item.StandardHours = Convert.ToDecimal(searchResult.Where(x => x.Name == item.Craft.Replace(" ", "_") + "_ST")
                                                        .Select(x => x.Value).FirstOrDefault());
                        item.OverTimeHours = Convert.ToDecimal(searchResult.Where(x => x.Name == item.Craft.Replace(" ", "_") + "_OT")
                                                        .Select(x => x.Value).FirstOrDefault());

                        if (item.Craft.ToUpper().Equals("PROJECT ENGINEER"))
                        {
                            item.StandardHours = engineerST;
                        }
                    }
                }

                var finalResult = result.Union(AdditionalInput).OrderBy(x => x.Craft);

                response.ActivityLabors = finalResult;

                //PopulateUnitItems(response);
            }
        }

        public void PopulateUnitItems(string[] miscPayitems, ResponseData response)
        {
            foreach (var item in response.ForemanPayitems)
            {
                if (item.PayItemCode.Length >= 6)
                {
                    item.PayitemType = GetPayitemType(item.PayItemCode);
                }
                else item.PayitemType = "ignore";
            }

            var resultUnitItemLabor = response.ForemanPayitems
                                                .Where(x => x.PayitemType == "labor" || x.PayItemCode == "98-107")
                                                .Where(x => !miscPayitems.Contains(x.PayItemCode))
                                                .Where(x => x.Qty > 0)
                                               .GroupBy(x => new { x.PayItemCode, x.PayItemDescription, x.UnitOfMeasure })
                                               .Select(x => new ResponseData.UnitItem
                                               {
                                                   PayitemCode = x.First().PayItemCode,
                                                   Craft = x.First().PayItemDescription,
                                                   UOM = x.First().UnitOfMeasure,
                                                   Qty = x.Sum(b => b.Qty)
                                               }).ToList();

            response.UnitItems = resultUnitItemLabor.OrderBy(x => x.Craft);

            var resultUnitItemEquipment = response.ForemanPayitems
                                                .Where(x => x.PayitemType == "equipment")
                                                .Where(x => !miscPayitems.Contains(x.PayItemCode))
                                                .Where(x => x.Qty > 0)
                                               .GroupBy(x => new { x.PayItemCode, x.PayItemDescription, x.UnitOfMeasure })
                                               .Select(x => new ResponseData.UnitItem
                                               {
                                                   PayitemCode = x.First().PayItemCode,
                                                   Craft = x.First().PayItemDescription,
                                                   UOM = x.First().UnitOfMeasure,
                                                   Qty = x.Sum(b => b.Qty)
                                               }).ToList();

            response.ActivityEquipments = resultUnitItemEquipment.OrderBy(x => x.Craft);
        }

        public static void PopulateMiscInfo(ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var activityMiscPayitems = response.ForemanPayitems
                                            .Where(x => response.MiscPayitems.Contains(x.PayitemId))
                                            .GroupBy(x => new { x.PayItemCode, x.PayItemDescription, x.UnitOfMeasure })
                                            .Select(x => new ResponseData.UnitItem
                                            {
                                                PayitemCode = x.Key.PayItemCode,
                                                Craft = x.Key.PayItemDescription,
                                                UOM = x.Key.UnitOfMeasure,
                                                Qty = x.Sum(y => y.Qty)
                                            })
                                            .ToList();

                var miscPayitemsForDisplay = _context
                                               .Get<PayItemMapping>()
                                               .Where(PayItemMapping.IsActiveFilter)
                                               .AsNoTracking()
                                               .Where(x => response.MiscPayitems.Contains(x.Id))
                                               .GroupBy(x => new { x.PayItemCode, x.PayItemDescription, x.UnitOfMeasure })
                                               .Select(x => new ResponseData.UnitItem
                                               {
                                                   PayitemCode = x.Key.PayItemCode,
                                                   Craft = x.Key.PayItemDescription,
                                                   UOM = x.Key.UnitOfMeasure,
                                                   Qty = 0
                                               }).ToList();

                foreach (var item in miscPayitemsForDisplay)
                {
                    var result = (from p in activityMiscPayitems
                                  where p.PayitemCode == item.PayitemCode && p.Craft == item.Craft && p.UOM == item.UOM
                                  select new
                                  {
                                      p.Qty
                                  }).FirstOrDefault();
                    if (result != null)
                        item.Qty = result.Qty;
                }

                response.Misctems = miscPayitemsForDisplay;

                int sectionA = 2 + response.ActivityLabors.Count();

                int sectionB = 7 + response.UnitItems.Count() + response.ActivityEquipments.Count() + miscPayitemsForDisplay.Count();

                response.SectionAEmptyRows = sectionB - sectionA;

                response.SectionBEmptyRows = sectionA - sectionB;
            }
        }

        public static string GetPayitemType(string payitemCode)
        {
            try
            {
                int prefix, suffix = 0;
                int.TryParse(payitemCode.Substring(0, 2), out prefix);
                int.TryParse(payitemCode.Substring(3, 3), out suffix);

                if (prefix == 0 || suffix == 0)
                { return "ignore"; }
                else if (prefix >= 98 && suffix >= 600)
                { return "equipment"; }
                else if (prefix <= 98 && suffix < 600)
                { return "labor"; }

                return "ignore";
            }
            catch (Exception)
            {
                return "ignore";
            }
        }

        public static void BindSignature(long activityId, int dfrId, string baseURL, ResponseData response)
        {
            var signatures = SharedDFRDataRepository.GetSignature(activityId, dfrId);

            if (!signatures.Count.Equals(0))
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

    }
}
