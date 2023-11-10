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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using static Crewlink.WindowsServices.Features.GetDteDFRData.ResponseData;

namespace Crewlink.WindowsServices.Features
{
     public class GetDteDFRData
    {
        public class ResponseData : BaseActivityQueryResponse
        {
            public string SuperintendentName { get; set; }
            public string ProcessDateAndTime { get; set; }
            public string LogoImagePath { get; set; }
            public string WorkOrder { get; set; }
            public string PurchaseOrder { get; set; }
            public string Location { get; set; }

            public string PayitemComments { get; set; }
            public string LaborComments { get; set; }
            public string EquipmentComments { get; set; }
            public string ReviewerComments { get; set; }

            public int PayitemCount { get; set; }
            public int LaborCount { get; set; }
            public int EquipmentCount { get; set; }
            public bool ShowApproverReviewerComments { get; set; }
            public bool ShowForemanComments { get; set; }

            public string ForemanSignature { get; set; }
            public string InspectorSignature { get; set; }
            public string UniqueFilename { get; set; }

            public int UserId { get; set; }

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            // public DFRVectren.GetAdditionalInfo.Response DFRAdditionaInfo { get; set; }

            public IEnumerable<RevenueData> DteRevenue { get; set; } = new List<RevenueData>();

            public class RevenueData
            {
                public string PayItemCode { get; set; }

                public string PayItemDescription { get; set; }

                public decimal Quantity { get; set; }

                public string UnitOfMeasure { get; set; }

                public string PurchaseOrderNumber { get; set; }

                public string WorkOrderNumber { get; set; }

                public string Address { get; set; }

                public decimal Price { get; set; }
            }

            public class LaborActivityTotal
            {
                public string EmployeeNumber { get; set; }

                public string EmployeeName { get; set; }

                public string Title { get; set; }

                public decimal StandardHours { get; set; }

                public decimal OvertimeHours { get; set; }

                public decimal DoubleTimeHours { get; set; }

                public decimal TotalHours { get; set; }
            }

            public class EquipmentActivityTotal
            {
                public string EquipmentName { get; set; }

                public string EquipmentCode { get; set; }

                public decimal Hours { get; set; }
            }

            public ResponseData()
            {
                ResponseType = CrewlinkServices.Core.Request.Response.ResponseType.File;
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
                        cacheKeyValue.Split('_').Count() == 15)
                    {
                        string[] SplitKey = cacheKeyValue.Split('_');
                        request = new GetDFRToken.Request();
                        request.FileName = SplitKey[0] + "_" + SplitKey[1] + "_" + SplitKey[2] + "_" + SplitKey[3] + "_" + SplitKey[4];
                        request.ActivityId = long.Parse(SplitKey[5]);
                        request.ProcessDateAndTime = SplitKey[6] + " " + SplitKey[7];
                        request.ShowPayitem = bool.Parse(SplitKey[8]);
                        request.ShowLabor = bool.Parse(SplitKey[9]);
                        request.ShowEquipment = bool.Parse(SplitKey[10]);
                        request.ShowForemanComments = bool.Parse(SplitKey[11]);
                        request.ShowApproverReviewerComments = bool.Parse(SplitKey[12]);
                        request.ShowSignature = bool.Parse(SplitKey[13]);
                        request.WorkOrderNumber = SplitKey[14];
                        _cache.Insert(cacheKeyValue, request, 13);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();

                 PopulateJobInfo(request.ActivityId, response);

                SharedBaseActivityHandler.PopulateJobDetails(request.ActivityId, response);

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");

             //   string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "..\\..\\..\\Crewlink.Services\\Features\\DailyActivity\\Templates\\");

                response.LogoImagePath = Path.Combine(BaseURL, "Images\\ifs-logo-bw.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "Images\\ifs-logo-bw.png");

                response.ShowForemanComments = request.ShowForemanComments;

                var tempVale = "";

                int currentDFRId =  SharedDFRDataRepository.GetDfrId("DTE");

                response.UniqueFilename = request.FileName;

                 GetAdditionalInfo(request.ActivityId, response, currentDFRId);

                 GetRevenue(request.ActivityId, response, request.WorkOrderNumber.Trim());

                var dfrTemplate = ReturnFileHTML(BaseURL, "DFR_Dte.cshtml", response);
               
                if (request.ShowLabor)
                {
                     GetLabor(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Labor.cshtml", response);
                }

                if (request.ShowPayitem)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Payitem.cshtml", response);
                }

                if (request.ShowEquipment)
                {
                     GetEquipment(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Equipment.cshtml", response);
                }

                if (request.ShowApproverReviewerComments)
                {
                    GetAdditionalInfo(request.ActivityId, response, currentDFRId);
                }

                if (response.ReviewerComments != null && request.ShowApproverReviewerComments)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Additional_Info.cshtml", response);
                }

                if (response.ReviewerComments == null && request.ShowApproverReviewerComments)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_ApproverReviwer_No_Comments.cshtml", response);
                }

                dfrTemplate += tempVale;

                var CurrentHashData = FileProcess.CalculateMD5Hash(tempVale);

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
                    BindSignature(request.ActivityId, currentDFRId, BaseURL, response);
                }

                response.FileName = request.FileName.ToString();

                response.ProcessDateAndTime = request.ProcessDateAndTime;

                dfrTemplate += ReturnFileHTML(BaseURL, "DFR_Dte_Signature.cshtml", response);

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
        public void BindSignature(long activityId, int dfrId, string baseURL, ResponseData response)
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

        public static void PopulateJobInfo(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobinfo =
            (from a in _context.Get<Activity>()
             join j in _context.Get<CrewlinkServices.Core.Models.Job>() on a.JobId equals j.Id
             join s in _context.Get<Employee>() on j.SuperitendentEmployeeNumber equals s.EmployeeNumber
             join u in _context.Get<CrewlinkServices.Core.Models.User>() on a.ForemanUserId equals u.Id
             join f in _context.Get<Employee>() on u.EmployeeId equals f.Id
             where a.Id == activityId
             select new
             {
                 suprintendentName = s.EmployeeName,
                 foremanName = f.EmployeeName,
                 contractNumber = a.Job.ContractNumber,
                 UserId = a.ForemanUserId
             }).First();

                response.ContractNumber = jobinfo.contractNumber;
                response.ForemanName = jobinfo.foremanName;
                response.SuperintendentName = jobinfo.suprintendentName;
                response.UserId = jobinfo.UserId;
            }
        }

        private string ReturnFileHTML(string path, string partial, ResponseData response = null)
        {
            if (response == null)
            { return Razor.Parse(File.ReadAllText(path + partial)); }
            else
            { return Razor.Parse(File.ReadAllText(path + partial), response); }

        }

        private static void GetRevenue(long activityId, ResponseData response, string workOrderNumber)
        {
            using (var _context = new ApplicationContext())
            {
                SharedBaseActivityHandler.PopulateRevenue(activityId, response);
                var revenueItems = _context
                                        .Get<RevenueActivity>()
                                        .AsNoTracking()
                                        .Where(RevenueActivity.IsActiveFilter)
                                        .Where(x => x.ActivityId == activityId)
                                        .Select(x => new RevenueData
                                        {
                                            PayItemCode = x.PayItem.PayItemCode,
                                            PayItemDescription = x.PayItem.PayItemDescription,
                                            UnitOfMeasure = x.PayItem.UnitOfMeasure,
                                            Address = x.Address + " - " + x.City.CityCode + " - " + x.City.StateCode,
                                            WorkOrderNumber = x.WorkOrderNumber,
                                            PurchaseOrderNumber = x.PurchaseOrderNumber,
                                            Quantity = x.Quantity
                                        }).OrderBy(x => x.Address).ThenBy(x => x.PayItemCode).ToList();

                if (!workOrderNumber.ToLower().Equals("all"))
                { revenueItems = revenueItems.Where(x => x.WorkOrderNumber == workOrderNumber).ToList(); }

             
                response.DteRevenue = revenueItems;

                response.PurchaseOrder = response.DteRevenue.Select(x => x.PurchaseOrderNumber).FirstOrDefault();

                response.WorkOrder = response.DteRevenue.Select(x => x.WorkOrderNumber).FirstOrDefault();

                response.Location = response.DteRevenue.Select(x => x.Address).FirstOrDefault();

                response.PayitemCount = response.DteRevenue.Count();

                response.PayitemComments = response.JobComments.Where(x => x.CommentType == "P").Select(x => x.Comment).FirstOrDefault();
            }
         }

     

        private static void GetLabor(long activityId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateLabor(activityId, response);

            response.LaborComments = response.JobComments.Where(x => x.CommentType == "L").Select(x => x.Comment).FirstOrDefault();

            var laborGroup = response.LaborRecords
                    .GroupBy(x => new
                    {
                        x.EmployeeNumber,
                        x.EmployeeName,
                        x.Title
                    })
                    .Select(result => new ResponseData.LaborActivityTotal
                    {
                        EmployeeNumber = result.Key.EmployeeNumber,
                        EmployeeName = result.Key.EmployeeName,
                        Title = result.Key.Title,
                        StandardHours = result.Sum(x => x.Records.Sum(y => y.StandardHours)),
                        OvertimeHours = result.Sum(x => x.Records.Sum(y => y.OvertimeHours)),
                        DoubleTimeHours = result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours)),
                        TotalHours = result.Sum(x => x.Records.Sum(y => y.StandardHours)) +
                                            result.Sum(x => x.Records.Sum(y => y.OvertimeHours)) +
                                            result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours))
                    }).ToList();

            response.LaborActivity = laborGroup;

            response.LaborCount = laborGroup.Count();
        }

        private static void GetEquipment(long activityId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateEquipment(activityId, response);

            response.EquipmentComments = response.JobComments.Where(x => x.CommentType == "E").Select(x => x.Comment).FirstOrDefault();

            var equipmentGroup = response.EquipmentRecords
                    .GroupBy(x => new
                    {
                        x.EquipmentCode,
                        x.EquipmentName
                    })
                    .Select(result => new ResponseData.EquipmentActivityTotal
                    {
                        EquipmentCode = result.Key.EquipmentCode,
                        EquipmentName = result.Key.EquipmentName,
                        Hours = result.Sum(x => x.Entries.Sum(y => y.Hours))
                    }).ToList();

            response.EquipmentActivity = equipmentGroup;

            response.EquipmentCount = equipmentGroup.Count();
        }

        public static void GetAdditionalInfo(long activityId, ResponseData response, int dfrId)
        {
            var result = GetDteDFRAdditionalInfo.GetData(activityId, dfrId);

            if (result.AdditionalInfo != null)
            {
                response.ReviewerComments = result.AdditionalInfo.Where(x => x.Name.Contains("Reviewer")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
            }
        }

    }
}
