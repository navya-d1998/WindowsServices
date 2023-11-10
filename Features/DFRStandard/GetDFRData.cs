
namespace Crewlink.WindowsServices.Features
{
    using System;
    using System.Threading.Tasks;
    using CrewlinkServices.Core.DataAccess;
    using CrewlinkServices.Core.Caching;
    using CrewlinkServices.Core.Crypto;
    using System.IO;
    using RazorEngine;
    using CrewlinkServices.Features.Shared;
    using CrewlinkServices.Core.Models;
    using System.Linq;
    using System.Data.Entity;
    using FluentValidation;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Drawing;
    using System.ComponentModel.DataAnnotations;
    using static CrewlinkServices.Features.DailyActivity.Labor.Get;
    using CrewlinkServices.Features.DailyActivity.Shared;
    using CrewlinkServices.Features.DailyActivity;
    using CrewLink.WindowsServices.Files.Shared;
    //using Crewlink.WindowsServices.Files.StandardDFR;
    using CrewlinkServices.Features.DailyActivity.DFRStandard;
    using Crewlink.WindowsServices.Features;
    using CrewLink.WindowsServices.Files;

    public  class GetStandardDFRData
    {
        public class ResponseData : BaseActivityQueryResponse
        {
            public string SuperintendentName { get; set; }
            public string ProcessDateAndTime { get; set; }
            public string LogoImagePath { get; set; }

            public string PayitemComments { get; set; }
            public string LaborComments { get; set; }
            public string EquipmentComments { get; set; }
            public string ReviewerComments { get; set; }

            public int PayitemCount { get; set; }
            public int LaborCount { get; set; }
            public int EquipmentCount { get; set; }

            public string ForemanSignature { get; set; }
            public string InspectorSignature { get; set; }

            public int UserId { get; set; }

            public IEnumerable<Revenue> TAndMRevenueItems { get; set; } = new List<Revenue>();

            public bool isSpireContract { get; set; }

            public int ProdRevenueCount { get; set; }

            public int TandMRevenueCount { get; set; }

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            public CrewlinkServices.Features.DailyActivity.DFRStandard.GetAdditionalInfo.Response DFRAdditionalInfo { get; set; }

            public List<DrawingImages> drawingImages { get; set; }

            public List<PayItemsRentalEquipments> PayItemRentalEquipment { get; set; } = new List<PayItemsRentalEquipments>();

            public List<StationData> Stations = new List<StationData>();

            public class StationData
            {
                public string payItemDescription { get; set; }

                public List<station> station = new List<station>();

            }
            public class station
            {
                public string startStation { get; set; }
                public string endStation { get; set; }
                public string totalFootage { get; set; }
            }
            public class PayItemData
            {
                public string payItemDescription { get; set; }

                public List<rentalEquipmentData> EquipmentData = new List<rentalEquipmentData>();

            }
            public class rentalEquipmentData
            {
                public string EquipmentDescription { get; set; }
                public decimal StandardHours { get; set; }
                public decimal OverTimeHours { get; set; }
                public decimal DoubleTimeHours { get; set; }

            }

            public class LaborActivityTotal
            {
                public string EmployeeNumber { get; set; }

                public string EmployeeName { get; set; }

                public long ForemanCrewId { get; set; }

                public decimal StandardHours { get; set; }

                public decimal OvertimeHours { get; set; }

                public decimal DoubleTimeHours { get; set; }
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

        public  ResponseData ExecuteRequest(string token)
        {

            try
            {

                var _crypto = new Crypto();
                var _cache = new HttpCacheStore();

                string converted = token.Replace('-', '+');
                converted = converted.Replace('_', '/');
                converted = converted.Replace("bearer ", "");

                string cacheKeyValue = _crypto.Decryption(converted);

                var request = _cache.GetItem<GetDFRToken.Request>(cacheKeyValue);

                string processDate = string.Empty;

                _cache.Clear(cacheKeyValue);

                if (request == null)
                {
                    if (cacheKeyValue != null &&
                        cacheKeyValue != string.Empty &&
                        cacheKeyValue.Split('_').Count() == 16)
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
                        request.ShowImageAttachments = bool.Parse(SplitKey[12]);
                        request.ShowCutSheets = bool.Parse(SplitKey[13]);
                        request.ShowRestoration = bool.Parse(SplitKey[14]);
                        request.ResurfacingId = long.Parse(SplitKey[15]);
                        //request.ShowHoursByPayItem = bool.Parse(SplitKey[15]);
                        _cache.Insert(cacheKeyValue, request, 10);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                ResponseData response = new ResponseData();

                if (request.ActivityId.Equals(0))
                {
                    PopulateJobInfoUsingResurfacing(request.ResurfacingId, response);
                    PopulateJobDetailsUsingResurfacing(request.ResurfacingId, response);
                }
                else
                {
                    PopulateJobInfo(request.ActivityId, response);
                    PopulateJobDetails(request.ActivityId, response);
                }

                response.IsTecoTampaProject = false;
                if (response.ContractNumber.ToUpper() == "TECO TAMPA BLANKET 20-25")
                {
                    response.IsTecoTampaProject = true;
                }


                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];


                string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");

                response.LogoImagePath = Path.Combine(BaseURL, "Images\\infrasource-logo.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "Images\\infrasource-logo.png");


                var tempVale = "";

                var dfrTemplate = "";

                var onlyResurfacing = false;

                if (!request.ResurfacingId.Equals(0))
                {
                    using (var _context = new ApplicationContext())
                    {
                        onlyResurfacing = _context
                        .Get<Resurfacing>()
                        .Any(x => x.Id == request.ResurfacingId && x.JobId != null);
                    }
                }

                if (onlyResurfacing)
                {
                    dfrTemplate = ReturnFileHTML(BaseURL, "DFR_Standard_Resurfacing.cshtml", response);
                }
                else
                {
                    dfrTemplate = ReturnFileHTML(BaseURL, "DFR_Standard.cshtml", response);
                }

                if (request.ShowImageAttachments)
                {
                    if (request.ResurfacingId.Equals(0))
                    {
                        using (var _context = new ApplicationContext())
                        {
                            request.ResurfacingId = _context
                            .Get<Resurfacing>()
                            .Where(x => x.ActivityId == request.ActivityId)
                            .Select(x => x.Id)
                            .FirstOrDefault();
                        }
                    }

                    PopulateImageData(request.ActivityId, request.ResurfacingId, response);
                    response.JobImages.ImageDataInfo = response.JobImages.ImageDataInfo.OrderBy(image => image.ImageOrder).ToList();
                }

                if (request.ShowPayitem)
                {
                    GetRevenue(request.ActivityId, response);

                    response.isSpireContract = false;

                    response.ProdRevenueCount = response.PayitemCount;

                    response.TandMRevenueCount = 0;


                    if (response.ContractNumber.ToLower().Contains("spire") && response.CompanyCode == "111")
                    {
                        response.isSpireContract = true;

                        GetTandMRevenue(request.ActivityId, response);
                    }
                    if (response.IsSpecialProject == true)
                    {
                        tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Payitem_Special_Project.cshtml", response);
                    }
                    else
                    {
                        tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Payitem.cshtml", response);
                    }
                }

                if (request.ShowLabor)
                {
                    GetLabor(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Labor.cshtml", response);
                }

                if (request.ShowEquipment)
                {
                    GetEquipment(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Equipment.cshtml", response);
                }

                if (request.ShowCutSheets)
                {
                    if (request.ResurfacingId.Equals(0) && !request.ActivityId.Equals(0))
                    {
                        using (var _context = new ApplicationContext())
                        {
                            request.ResurfacingId = _context
                            .Get<Resurfacing>()
                            .Where(x => x.ActivityId == request.ActivityId)
                            .Select(x => x.Id)
                            .FirstOrDefault();
                        }
                    }

                    GetCutSheetsData(request.ResurfacingId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_CutSheets.cshtml", response);
                }

                if (request.ShowRestoration)
                {
                    if (request.ResurfacingId.Equals(0) && !request.ActivityId.Equals(0))
                    {
                        using (var _context = new ApplicationContext())
                        {
                            request.ResurfacingId = _context
                            .Get<Resurfacing>()
                            .Where(x => x.ActivityId == request.ActivityId)
                            .Select(x => x.Id)
                            .FirstOrDefault();
                        }
                    }

                    GetRestorationData(request.ResurfacingId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Restoration.cshtml", response);
                }

                if (request.ShowHoursByPayItem)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Labor_By_PayItem.cshtml", response);
                }
                if (request.ShowRentalEquipments)
                {
                    GetRentalEquipments(request.ActivityId, response);
                    tempVale += ReturnFileHTML(BaseURL, "DFR_SPECIAL_PROJECT_RENTAL_EQUIPMENTS.cshtml", response);
                }
                if (request.ShowStations)
                {
                    GetStations(request.ActivityId, response);
                    tempVale += ReturnFileHTML(BaseURL, "DFR_SPECIAL_PROJECT_STATIONS.cshtml", response);
                }

                if (request.ShowImageAttachments)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Image_Attachments.cshtml", response);
                }

                if (request.ShowDrawing)
                {
                    response.drawingImages = getDrawingImages(request.ResurfacingId);
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Starnard_Drawing_Images.cshtml", response);
                }


                int CurrentDFRId = SharedDFRDataRepository.GetDfrId("STANDARD");

                if (response.ContractNumber == "WGL OFFSET Q")
                {
                    GetAdditionalInfo(request.ActivityId, response, CurrentDFRId);

                    if (response.ReviewerComments != null && response.ReviewerComments != "")

                        tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Additional_Info.cshtml", response);
                }
                var isSpecialProject = new Contract();
                using (var _context = new ApplicationContext())
                {
                    isSpecialProject = _context.Get<Contract>().Where(x => x.ContractNumber == response.ContractNumber).FirstOrDefault();
                }

                if (isSpecialProject.IsSpecialProject == true)
                {
                    // DFR_SPECIAL_PROJECT_RENTAL_EQUIPMENTS.cshtml
                    //await GetRentalEquipments(request.ActivityId, response);
                    //tempVale += ReturnFileHTML(BaseURL, "DFR_SPECIAL_PROJECT_RENTAL_EQUIPMENTS.cshtml", response);
                    //await GetStations(request.ActivityId, response);
                    //tempVale += ReturnFileHTML(BaseURL, "DFR_SPECIAL_PROJECT_STATIONS.cshtml", response);

                }

                dfrTemplate += tempVale;


                var CurrentHashData = FileProcess.CalculateMD5Hash(tempVale);


                var ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);

                if (string.IsNullOrEmpty(ArchivedHashData) && !request.ActivityId.Equals(0))
                {
                    SharedDFRDataRepository.SaveDFRData(request.ActivityId, CurrentDFRId, CurrentHashData, response.UserId);
                }
                else if (!CurrentHashData.Equals(ArchivedHashData) && !request.ActivityId.Equals(0))
                {
                    SharedDFRDataRepository.InvalidateSignature(request.ActivityId, CurrentDFRId);

                    SharedDFRDataRepository.UpdateDFRData(request.ActivityId, CurrentDFRId, CurrentHashData, response.UserId);

                    request.ShowSignature = false;
                }


                if (request.ShowSignature && !request.ActivityId.Equals(0))
                {
                    BindSignature(request.ActivityId, CurrentDFRId, BaseURL, response);
                }

                // to 
           
                //try         //// testing block remove  after testing
                //{
                //    System.Drawing.Image imgk = System.Drawing.Image.FromFile(BaseURL + "Images\\infrasource-logo.png");
                //    using (var _context = new ApplicationContext())
                //    {
                //        var abcd = new LogMessage();
                //        abcd.LogDate = DateTime.Now;
                //        abcd.Message = imgk.Height + ":" + imgk.Width;
                //        abcd.Source = "drawing";
                //        abcd.StatusType = 111;
                //        _context.Add(abcd);
                //        _context.SaveChanges();
                //    }
                //    System.Drawing.Image img = System.Drawing.Image.FromFile(BaseURL + "Images\\infrasource-logo.png", true);
                //}
                //catch (Exception e)
                //{
                //    using (var _context = new ApplicationContext())
                //    {
                //        var abcd = new LogMessage();
                //        abcd.LogDate = DateTime.Now;
                //        abcd.Message = e.ToString();
                //        abcd.Source = "drawing";
                //        abcd.StatusType = 111;
                //        _context.Add(abcd);
                //        _context.SaveChanges();
                //    }
                //}



                response.FileName = request.FileName.ToString();

                response.ProcessDateAndTime = request.ProcessDateAndTime;

                dfrTemplate += ReturnFileHTML(BaseURL, "DFR_Standard_Signature.cshtml", response);

                response.FileContent = dfrTemplate;

                response.TemplateSize = "A3";

                response.NumberOfPages = 0;

                return response;
            }
            catch (Exception e)
            {
                Helper.LogError(e, "GetStandardDFRData");

                string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                throw new Exception(fullexceptiondetails);
            }


        }

        public static void GetAdditionalInfo(long activityId, ResponseData response, int dfrId)
        {
            var result = GetStandardAdditionalInfo.GetData(activityId, dfrId);

            if (result.AdditionalInfo != null)
            {
                response.ReviewerComments = result.AdditionalInfo.Where(x => x.Name.Contains("Reviewer")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
            }
        }
        public static void BindSignature(long activityId, int dfrId, string baseURL, ResponseData response)
        {
           // SharedBaseActivityHandler.PopulateLabor(activityId, response);

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
        private static void GetLabor(long activityId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateLabor(activityId, response);

            response.LaborComments = response.JobComments.Where(x => x.CommentType == "L").Select(x => x.Comment).FirstOrDefault();

            var laborGroup = response.LaborRecords
                    .GroupBy(x => new
                    {
                        x.EmployeeNumber,
                        x.EmployeeName,
                        x.ForemanCrewId
                    })
                    .Select(result => new ResponseData.LaborActivityTotal
                    {
                        EmployeeNumber = result.Key.EmployeeNumber,
                        EmployeeName = result.Key.EmployeeName,
                        ForemanCrewId = result.Key.ForemanCrewId,
                        StandardHours = result.Sum(x => x.Records.Sum(y => y.StandardHours)),
                        OvertimeHours = result.Sum(x => x.Records.Sum(y => y.OvertimeHours)),
                        DoubleTimeHours = result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours))
                    }).ToList();

            response.LaborActivity = laborGroup.OrderBy(x => x.EmployeeName).ToList();
            response.LaborCount = laborGroup.Count();
        }

        public static void GetRentalEquipments(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var payItemRentalEquipment = new List<PayItemsRentalEquipments>();
                payItemRentalEquipment = _context.Get<PayItemsRentalEquipments>().Where(x => x.Job_id == activityId && x.IsActive == true && (x.StationFlag == null || x.StationFlag != true)).ToList();

                response.PayItemRentalEquipment = payItemRentalEquipment;
            }
        }

        public static List<DrawingImages> getDrawingImages(long resurfacingId)
        {
            using (var _imageContext = new ImageContext())
            {

                List<DrawingImages> images = new List<DrawingImages>();
                images = _imageContext
                    .Get<DrawingImages>()
                    .AsNoTracking()
                    .Where(x => x.ResurfacingId == resurfacingId)
                    .Where(x => x.IsActive == true)
                    .ToList();
                return images;
            }
        }
        public static void GetStations(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var stations = new List<PayItemsRentalEquipments>();
                stations = _context.Get<PayItemsRentalEquipments>().Where(x => x.Job_id == activityId && x.IsActive == true && (x.StationFlag == true)).ToList();
                List<string> PayItem = new List<string>();
                foreach (var item in stations)
                {
                    if (PayItem.IndexOf(item.PayItemDescription + " - " + item.PayItemName) == -1)
                    {
                        PayItem.Add(item.PayItemDescription + " - " + item.PayItemName);
                    }

                }
                PayItem.Distinct();
                var dummyPayItemStations = new List<ResponseData.StationData>();



                foreach (var payitem in PayItem)
                {
                    var dummyPayItemStation = new ResponseData.StationData();
                    var dummyStations = new List<ResponseData.station>();
                    foreach (var item in stations)
                    {
                        if (payitem == item.PayItemDescription + " - " + item.PayItemName)
                        {
                            var dummyStation = new ResponseData.station();
                            dummyStation.startStation = item.StartStation;
                            dummyStation.endStation = item.EndStation;
                            dummyStation.totalFootage = item.TotalFootage;
                            // dummyEquipments.Clear();
                            dummyStations.Add(dummyStation);
                        }
                    }
                    dummyPayItemStation.payItemDescription = payitem;
                    dummyPayItemStation.station = dummyStations;
                    var data = dummyPayItemStation;
                    dummyPayItemStations.Add(data);
                }
                response.Stations = dummyPayItemStations;
            }
        }
        public static void GetRestorationData(long resurfacingId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateRestorationData(resurfacingId, response);
            //long[] resurfacingIds = new long[resurfacingId];

            //GetRestorationData1.ExecuteRequest(resurfacingIds);
        }
        public static void GetCutSheetsData(long resurfacingId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateCutSheetsData(resurfacingId, response);
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

        private static void GetTandMRevenue(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var tAndMPayItems = _context
                                               .Get<TandMPayItems>()
                                               .Select(x => x.PayItemCode)
                                               .ToList();

                var prodRevenueItems = response.RevenueItems.ToList();

                var tandmRevenueItems = new List<BaseActivityQueryResponse.Revenue>();

                foreach (var a in tAndMPayItems)
                {
                    var resultTandmPayitems = new List<BaseActivityQueryResponse.Revenue>();

                    if (a.ToUpper() == "SCAM")
                    {
                        resultTandmPayitems = response.RevenueItems.Where(x => x.PayItemCode.Contains(a)).ToList();
                    }
                    else
                    {
                        resultTandmPayitems = response.RevenueItems.Where(x => x.PayItemCode == a).ToList();

                    }

                    foreach (var item in resultTandmPayitems)
                    {
                        prodRevenueItems.Remove(item);
                        tandmRevenueItems.Add(item);
                    }
                }


                response.RevenueItems = prodRevenueItems;
                response.ProdRevenueCount = prodRevenueItems.Count();
                response.TAndMRevenueItems = tandmRevenueItems;
                response.TandMRevenueCount = tandmRevenueItems.Count();
            }

        }

        private static void GetRevenue(long activityId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateRevenue(activityId, response);
            //  PopulateRevenue(activityId, response);

            response.PayitemCount = response.RevenueItems.SelectMany(x => x.Records).Count();

            response.PayitemComments = response.JobComments.Where(x => x.CommentType == "P").Select(x => x.Comment).FirstOrDefault();
        }

        public static void PopulateImageData(long activityId, long resurfacingId, BaseActivityQueryResponse response)
        {

            var imageData = GetImageDataInfo(activityId, resurfacingId);

            SharedDFRDataRepository.GetUploaderInfo(imageData);

            response.JobImages = new GetJobImageInfo.Response();

            //  response.JobImages = (GetJobImageInfo.Response)imageData;

            response.JobImages.ImageDataInfo = imageData;

        }
        public static IList<JobImages> GetImageDataInfo(long activityId, long resurfacingId)
        {

            if (activityId.Equals(0))
            {
                var result = SharedDFRDataRepository.GetResurfacingImages(resurfacingId);

                return result;
            }
            else
            {
                var result = SharedDFRDataRepository.GetExistingJobImageInfo(activityId);

                return result;
            }
        }
        public static void PopulateJobDetailsUsingResurfacing(long resurfacingId, BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {

                var jobDetails = _context
        .Get<Resurfacing>()
        .AsNoTracking()
        .Where(x => x.Id == resurfacingId)
        .Select(x => new
        {
            x.JobId,
            x.ActivityDate,
            x.Job.JobNumber,
            x.Job.Description,
            x.JobActivityStatus.Name,
            x.ForemanUserId,
            x.Foreman.Employee.EmployeeName,
            x.Job.ContractNumber,
            x.Job.CompanyCode,
            x.SuperitendentComment,
            x.WorkOrderNumber,
            x.Address,
            x.TrafficControl,
            x.CustomerComplaint,
            x.BacklogWorkDate,
            x.CityId,
            x.IsJobCompleted,
            x.IsRestorationRequired,
            x.RestorationData,
            x.RestorationOrderNumber,
            x.PurchaseOrderNumber
        }).First();

                response.JobNumber = jobDetails.JobNumber;
                response.JobId = resurfacingId;
                response.JobDescription = jobDetails.Description;
                response.ActivityDate = jobDetails.ActivityDate;
                response.ForemanUserId = jobDetails.ForemanUserId;
                response.ContractNumber = jobDetails.ContractNumber;
                response.JobStatus = jobDetails.Name;
                response.CompanyCode = jobDetails.CompanyCode;
                response.ForemanName = jobDetails.EmployeeName;
                response.SuperintendentComment = jobDetails.SuperitendentComment;
                response.WorkOrder = jobDetails.WorkOrderNumber;
                response.ResurfacingAddress = jobDetails.Address;
                response.TrafficControl = jobDetails.TrafficControl;
                response.BacklogWorkDate = jobDetails.BacklogWorkDate?.ToString("MM/dd/yyyy");
                response.CustomerComplaint = jobDetails.CustomerComplaint;
                response.CityId = jobDetails.CityId;
                response.isJobCompleted = jobDetails.IsJobCompleted;
                response.IsRestorationRequired = jobDetails.IsRestorationRequired;
                response.RestorationData = jobDetails.RestorationData;
                response.RestorationOrder = jobDetails.RestorationOrderNumber;
                response.PurchaseOrder = jobDetails.PurchaseOrderNumber;

                GetCityCodeAndStateCode(response);

            }
        }
        public static void GetCityCodeAndStateCode(BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {
                if (response.CityId != null)
                {
                    var city = _context
                        .Get<City>()
                        .Where(x => x.Id == response.CityId)
                        .FirstOrDefault();

                    response.CityCode = city?.CityCode;
                    response.StateCode = city?.StateCode;
                }
            }
        }
        public static void PopulateJobInfoUsingResurfacing(long resurfacingId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobinfo =
            (from r in _context.Get<Resurfacing>()
             join j in _context.Get<CrewlinkServices.Core.Models.Job>() on r.JobId equals j.Id
             join s in _context.Get<Employee>() on j.SuperitendentEmployeeNumber equals s.EmployeeNumber
             join u in _context.Get<CrewlinkServices.Core.Models.User>() on r.ForemanUserId equals u.Id
             join f in _context.Get<Employee>() on u.EmployeeId equals f.Id
             where r.Id == resurfacingId
             select new
             {
                 suprintendentName = s.EmployeeName,
                 foremanName = f.EmployeeName,
                 contractNumber = r.Job.ContractNumber,
                 UserId = r.ForemanUserId
             }).First();

                response.ContractNumber = jobinfo.contractNumber;
                response.ForemanName = jobinfo.foremanName;
                response.SuperintendentName = jobinfo.suprintendentName;
                response.UserId = jobinfo.UserId;
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

        public static void PopulateJobDetails(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobDetails = _context
                .Get<Activity>()
                .AsNoTracking()
                .Where(x => x.Id == activityId)
                .Select(x => new
                {
                    x.JobId,
                    x.ActivityDate,
                    x.Job.JobNumber,
                    x.Job.Description,
                    x.JobActivityStatus.Name,
                    x.Comments,
                    x.ForemanUserId,
                    x.Foreman.Employee.EmployeeName,
                    x.Job.ContractNumber,
                    x.SuperitendentComment,
                    x.RevenueExported,
                    x.ModifiedBy,
                    x.Job.CompanyCode,
                    x.Submitted_On
                }).First();

                var isSpecialProject = _context
                    .Get<Contract>()
                    .AsNoTracking()
                    .Where(c => c.ContractNumber == jobDetails.ContractNumber)
                    .Select(x => x.IsSpecialProject).First();
                var isPecoRelatedOverride = _context
                       .Get<Contract>()
                       .AsNoTracking()
                       .Where(c => c.ContractNumber == jobDetails.ContractNumber)
                       .Select(x => x.IsPecoRelatedOverride).First();

                if (isSpecialProject)
                {
                    //var unionCodes = new List<SpecialProjectsUnionCode>();
                    try
                    {
                        var unionCodes = (from u in _context
                                .Get<SpecialProjectsUnionCode>()
                                          where u.ContractNumber == jobDetails.ContractNumber && u.JobNumber == jobDetails.JobNumber && u.IsActive == true
                                          select new JobUnionCode
                                          {
                                              Id = u.Id,
                                              ContractNumber = u.ContractNumber,
                                              JobNumber = u.JobNumber,
                                              UnionCode = u.UnionCode,
                                              IsActive = u.IsActive
                                          }).ToList();

                        response.JobUnionCodes = unionCodes;
                    }
                    catch (Exception e)
                    {
                        Helper.LogError(e, "GetStandardDFRData");
                        string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                        throw new Exception(fullexceptiondetails);
                    }
                }

                if (isPecoRelatedOverride)
                {
                    var companyCodeParam = new SqlParameter("@CompanyCode", SqlDbType.VarChar) { Value = jobDetails.CompanyCode };

                    List<PecoUnionCode> pecoUnionCodes = new List<PecoUnionCode>();
                    //List<SpecialProjectsUnionCode> unionCodes = new List<SpecialProjectsUnionCode>();

                    var query = $"exec GET_PECO_UNION_CODES {companyCodeParam}";

                    try
                    {
                        pecoUnionCodes = ((ApplicationContext)_context)
                                       .Database
                                       .SqlQuery<PecoUnionCode>(query, companyCodeParam)
                                       .ToList();

                        response.PecoUnionCodes = pecoUnionCodes;

                    }
                    catch (Exception e)
                    {
                        Helper.LogError(e, "GetStandardDFRData");

                        string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                        throw new Exception(fullexceptiondetails);
                    }

                }
                var ApproverRole = (from activity in _context.Get<Activity>()
                                    join jobMaster in _context.Get<Job>() on activity.JobId equals jobMaster.Id
                                    join employee in _context.Get<Employee>() on jobMaster.SuperitendentEmployeeNumber equals employee.EmployeeNumber
                                    join user in _context.Get<CrewlinkServices.Core.Models.User>() on employee.Id equals user.EmployeeId
                                    where (activity.Id == activityId)
                                    select (new BaseActivityQueryResponse.User
                                    {
                                        UserType = user.UserType,
                                        UserRole = user.RoleId,
                                        EmployeeNumber = employee.EmployeeNumber
                                    })).FirstOrDefault();
                var submittedOn = "-";

                if (jobDetails.Submitted_On != null)
                {
                    var submittedDate = jobDetails.Submitted_On.Value;
                    submittedOn = submittedDate.ToString("MM/dd/yyyy HH:mm");
                }
                response.Submitted_On = submittedOn;
                response.ApproverRole = ApproverRole != null ? ApproverRole.UserType : null;
                response.ApproverEmployeeNumber = ApproverRole != null ? ApproverRole.EmployeeNumber : null;
                response.JobId = activityId;
                response.JobDescription = jobDetails.Description;
                response.JobNumber = jobDetails.JobNumber;
                response.ActivityDate = jobDetails.ActivityDate;
                response.ForemanUserId = jobDetails.ForemanUserId;
                response.JobComments = jobDetails.Comments.Select(x => new BaseActivityQueryResponse.Comments { Comment = x.Comment, CommentType = x.Type });
                response.ForemanName = jobDetails.EmployeeName;
                response.ContractNumber = jobDetails.ContractNumber;
                response.SuperintendentComment = jobDetails.SuperitendentComment;
                response.JobStatus = jobDetails.Name;
                response.RevenueExported = jobDetails.RevenueExported;
                response.LockdownStatus = Lockdown(activityId, jobDetails.ActivityDate, jobDetails.ModifiedBy, _context);
                response.IsSpecialProject = isSpecialProject;
                response.IsPecoRelatedOverride = isPecoRelatedOverride;
                response.CompanyCode = jobDetails.CompanyCode;
                GetReviewerInfo(activityId, response);
                GetContractInfo(response);
                GetEmployeeCompanyCodeInfo(response);
            }
        }

        public static void GetEmployeeCompanyCodeInfo(ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                //_context
                //     .Get<Employee>()
                //     .AsNoTracking()
                //     .Where(x => x.Id == response.ForemanUserId)
                //     .Select(x => new
                //     {
                //         x.CompanyCode
                //     }).FirstOrDefault();

                var employeeCompanyCodeInfo = (
                from employee in _context.Get<Employee>()
                join user in _context.Get<CrewlinkServices.Core.Models.User>() on employee.Id equals user.EmployeeId
                where user.Id == response.ForemanUserId
                select new { employee.CompanyCode }).FirstOrDefault();

                response.EmployeeCompanyCode = employeeCompanyCodeInfo.CompanyCode;
            }
        }

        public static void GetContractInfo(ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobContract = _context
                 .Get<Contract>()
                 .AsNoTracking()
                 .Where(x => x.ContractNumber == response.ContractNumber)
                 .Select(x => new
                 {
                     x.Id,
                     x.CompanyId,
                     x.Company.CompanyCode
                 }).FirstOrDefault();

                response.ContractId = jobContract.Id;
                response.CompanyId = jobContract.CompanyId;
                response.CompanyCode = jobContract.CompanyCode;
            }
        }
        private static string ReturnFileHTML(string path, string partial, ResponseData response = null)
        {
         
            if (response == null)
            { return Razor.Parse(File.ReadAllText(path + partial)); }
            else
            { return Razor.Parse(File.ReadAllText(path + partial), response); }

        }
        public static void GetReviewerInfo(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobReviewer = _context
                 .Get<ActivityReviewer>()
                 .AsNoTracking()
                 .Where(x => x.JobID == activityId)
                 .Select(x => new
                 {
                     x.User.Employee.EmployeeName
                 }).FirstOrDefault();

                if (jobReviewer != null)
                    response.ReviewerName = jobReviewer.EmployeeName;
            }
        }

        private static bool Lockdown(long activityId, DateTime activityDate, int? approverId, ApplicationContext _context)
        {
            return _context
                    .Get<LockDown>().Where(LockDown.IsActiveQueryFilter)
                    .Where(x => x.UserId == approverId)
                    .Where(x => (activityDate >= x.WeekStart && activityDate <= x.WeekEnd))
                    .Where(x => x.IsLockedDown == true)
                    .Any();
        }

    }
    
    //public class ResponseData1 : BaseActivityQueryResponse
    //{
    //    public DateTime ActivityDate { get; set; }
    //    public string LogoImagePath { get; set; }

    //    public int UserId { get; set; }

    //    public string SuperintendentName { get; set; }
    //    public long JobId { get; set; }

    //    public string UniqueJobId
    //    {
    //        get
    //        {
    //            return string.Concat(new String('0', 8 - JobId.ToString().Length), JobId.ToString());
    //        }
    //    }

    //    public string JobNumber { get; set; }

    //    public string JobDescription { get; set; }

    //    public int ForemanUserId { get; set; }

    //    public string ApproverRole { get; set; }

    //    [StringLength(20)]
    //    public string ApproverEmployeeNumber { get; set; }

    //    public string ReviewerName { get; set; }

    //    public string ForemanName { get; set; }
    //    public string ForemanID { get; set; }

    //    public int CompanyId { get; set; }

    //    public string CompanyCode { get; set; }

    //    public string EmployeeCompanyCode { get; set; }

    //    public string ContractNumber { get; set; }

    //    public int ContractId { get; set; }

    //    public string City { get; set; }

    //    public string WorkOrder { get; set; }

    //    public string RestorationOrder { get; set; }

    //    public string PurchaseOrder { get; set; }

    //    public string JobStatus { get; set; }

    //    public string SuperintendentComment { get; set; }

    //    public string Submitted_On { get; set; }

    //    public bool RevenueExported { get; set; }

    //    public bool LockdownStatus { get; set; }

    //    public bool IsSpecialProject { get; set; }

    //    public bool IsPecoRelatedOverride { get; set; }

    //    public bool IsSplitUpJob { get; set; }

    //    public string ResurfacingAddress { get; set; }

    //    public string CityCode { get; set; }

    //    public string StateCode { get; set; }

    //    public short? CityId { get; set; }

    //    public bool? TrafficControl { get; set; }

    //    public bool? CustomerComplaint { get; set; }

    //    public bool IsTecoTampaProject { get; set; }

    //    public bool? isJobCompleted { get; set; }
    //    public bool? IsRestorationRequired { get; set; }
    //    public string RestorationData { get; set; }

    //    public string BacklogWorkDate { get; set; }

    //    public IEnumerable<JobUnionCode> JobUnionCodes { get; set; } = new List<JobUnionCode>();

    //    public IEnumerable<PecoUnionCode> PecoUnionCodes { get; set; } = new List<PecoUnionCode>();

    //    public IEnumerable<Comments> JobComments { get; set; } = new List<Comments>();

    //    public string FileContent { get; set; }

    //    public MemoryStream StreamContent { get; set; }

    //    public string FileName { get; set; }

    //    public string TemplateSize { get; set; } = "A4";

    //    public int NumberOfPages { get; set; } = 0;

    //    public int CurrentPage { get; set; }
    //}
}