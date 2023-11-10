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

namespace Crewlink.WindowsServices.Features
{
    public class GetDteStandardDFRData
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

            public bool showPayItem { get; set; }
            public bool showLabor { get; set; }
            public bool showEquipment { get; set; }
            public bool showImageUploads { get; set; }

            public bool dteStandardDfr { get; set; }

            public bool printAlong { get; set; }

            public int count { get; set; }

            public int UserId { get; set; }

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            public CrewlinkServices.Features.DailyActivity.DFRStandard.GetAdditionalInfo.Response DFRAdditionalInfo { get; set; }

            public class RevAddress
            {
                public string address { get; set; }
                public string City { get; set; }
                public string State { get; set; }
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
                        cacheKeyValue.Split('_').Count() == 18)
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
                        request.Address = SplitKey[16];
                        request.workOrders = SplitKey[17];
                        //request.ShowHoursByPayItem = bool.Parse(SplitKey[15]);
                        _cache.Insert(cacheKeyValue, request, 10);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();
                if (request.ActivityId.Equals(0))
                {
                     PopulateJobInfoUsingResurfacing(request.ResurfacingId, response);
                    SharedBaseActivityHandler.PopulateJobDetailsUsingResurfacing(request.ResurfacingId, response);
                }
                else
                {
                     PopulateJobInfo(request.ActivityId, response);
                     SharedBaseActivityHandler.PopulateJobDetails(request.ActivityId, response);
                }

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");

              //  string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "..\\..\\..\\Crewlink.Services\\Features\\DailyActivity\\Templates\\");

                response.LogoImagePath = Path.Combine(BaseURL, "Images\\logo2.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "Images\\ifs-logo-bw.png");

                var tempVale = "";

                //int CurrentDFRId = await _dfrDataRepository.GetDfrId("STANDARD");

                //await GetAdditionalInfo(request.ActivityId, response, CurrentDFRId);

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

                /*if (request.ShowImageAttachments)
                {
                    if (request.ResurfacingId.Equals(0))
                    {
                        request.ResurfacingId = await _context
                            .Get<Resurfacing>()
                            .Where(x => x.ActivityId == request.ActivityId)
                            .Select(x => x.Id)
                            .FirstOrDefaultAsync();
                    }

                    await PopulateImageData(request.ActivityId, request.ResurfacingId, response);
                    response.JobImages.ImageDataInfo = response.JobImages.ImageDataInfo.OrderBy(image => image.ImageOrder).ToList();
                }*/
                response.showPayItem = request.ShowPayitem;
                response.showLabor = request.ShowLabor;
                response.showEquipment = request.ShowEquipment;
                response.showImageUploads = request.ShowImageAttachments;
                response.dteStandardDfr = false;
                response.printAlong = false;
                List<ResponseData.RevAddress> revAct = new List<ResponseData.RevAddress>();
                if (request.Address == null || request.Address.ToUpper() == "ALL")
                {
                    revAct =  GetActivityAddress(request.ActivityId);
                }
                else if ((request.workOrders != "" || request.workOrders != null) && (request.Address == "" || request.Address == null))
                {
                    revAct =  GetFirstWorkOrderAddress(request.ActivityId, request.workOrders);


                }
                else
                {
                    string[] splitaddress = request.Address.Split(',');
                    ResponseData.RevAddress rev = new ResponseData.RevAddress();
                    rev.address = splitaddress[0];
                    rev.City = splitaddress[1];
                    rev.State = splitaddress[2];
                    if (splitaddress.Length == 4)
                    {
                        request.ActivityId = long.Parse(splitaddress[3]);
                    }

                    revAct.Add(rev);
                }

                response.count = 0;
                foreach (var address in revAct)
                {
                    if (response.count > 0)
                    {
                        tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Standard_Header.cshtml", response);
                    }
                    response.count++;
                    if (request.ShowPayitem)
                    {
                        if (response.count == revAct.Count)
                        {
                            response.dteStandardDfr = true;
                        }
                        else
                        {
                            response.dteStandardDfr = false;
                        }
                         GetRevenue(request.ActivityId, response, address);

                        if (request.workOrders != "" && request.workOrders != null)
                        {

                         GetRevenueWorkOrder(request, response);
                        }

                        tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Standard_Payitems.cshtml", response);
                    }

                    if (request.ShowLabor)
                    {
                        if (response.count == revAct.Count)
                        {
                            response.dteStandardDfr = true;
                        }
                        else
                        {
                            response.dteStandardDfr = false;
                        }
                        GetLabor(request.ActivityId, response);

                        tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Standard_Labor.cshtml", response);
                    }
                    /*
                     * C:\Users\vijaypradeesh.raja\Documents\crewlink\src\Crewlink.Services\Features\DailyActivity\Templates\DFR_Dte_Payitem.cshtml
                     * C:\Users\vijaypradeesh.raja\Documents\crewlink\src\Crewlink.Services\Features\DailyActivity\Templates\DFR_Dte_Standard_Labor.cshtmlC: \Users\vijaypradeesh.raja\Documents\crewlink\src\Crewlink.Services\Features\DailyActivity\Templates\DFR_Standard_Equipment.cshtml*
                    C:\Users\vijaypradeesh.raja\Documents\crewlink\src\Crewlink.Services\Features\DailyActivity\Templates\DFR_Dte_Standard_Equipments.cshtml/*/
                    if (request.ShowEquipment)
                    {
                        if (response.count == revAct.Count)
                        {
                            response.dteStandardDfr = true;
                        }
                        else
                        {
                            response.dteStandardDfr = false;
                        }
                         GetEquipment(request.ActivityId, response);

                        tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Standard_Equipments.cshtml", response);
                    }


                    if (request.ShowHoursByPayItem)
                    {
                        tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Labor_By_PayItem.cshtml", response);
                    }

                    if (request.ShowImageAttachments)
                    {
                        if (response.count == revAct.Count)
                        {
                            response.dteStandardDfr = true;
                        }
                        else
                        {
                            response.dteStandardDfr = false;
                        }
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
                        string addRess = address.address + "," + address.City + "," + address.State;
                        SharedBaseActivityHandler.PopulateImageData(request.ActivityId, request.ResurfacingId, response);
                        response.JobImages.ImageDataInfo = response.JobImages.ImageDataInfo.Where(x => x.address == addRess).OrderBy(image => image.ImageOrder).ToList();
                        tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Standard_Image_Uploads.cshtml", response);
                    }

                }
                if (response.showImageUploads && (request.Address == null || request.Address.ToUpper() == "ALL"))
                {
                    response.dteStandardDfr = true;
                    SharedBaseActivityHandler.PopulateImageData(request.ActivityId, request.ResurfacingId, response);
                    response.JobImages.ImageDataInfo = response.JobImages.ImageDataInfo.Where(x => x.address == null).OrderBy(image => image.ImageOrder).ToList();
                    if (response.JobImages.ImageDataInfo.Count > 0)
                    {
                        tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Image_Uploads_not_mapped.cshtml", response);
                    }

                }

                if (response.showImageUploads && ((request.workOrders != "" || request.workOrders != null) && (request.Address == "" || request.Address == null)))
                {
                    response.dteStandardDfr = true;
                    SharedBaseActivityHandler.PopulateImageData(request.ActivityId, request.ResurfacingId, response);
                    var addresses =  GetWorkOrderAddress(request.ActivityId, request.workOrders);
                    foreach (var address in addresses)
                    {
                        string addRess = address.address + "," + address.City + "," + address.State;
                        response.JobImages.ImageDataInfo = response.JobImages.ImageDataInfo.Where(x => x.address == addRess).OrderBy(image => image.ImageOrder).ToList();
                        if (response.JobImages.ImageDataInfo.Count > 0)
                        {
                            tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_Image_Uploads_not_mapped.cshtml", response);
                        }
                    }

                }

                int CurrentDFRId =  SharedDFRDataRepository.GetDfrId("DTE STD");

                if (response.ContractNumber == "WGL OFFSET Q")
                {
                     GetAdditionalInfo(request.ActivityId, response, CurrentDFRId);

                    if (response.ReviewerComments != null && response.ReviewerComments != "")

                        tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Additional_Info.cshtml", response);
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

                response.FileName = request.FileName.ToString();



                response.ProcessDateAndTime = request.ProcessDateAndTime;

                dfrTemplate += ReturnFileHTML(BaseURL, "DFR_Standard_Signature.cshtml", response);

                response.FileContent = dfrTemplate;

                response.TemplateSize = "A3";

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

        public static List<ResponseData.RevAddress> GetActivityAddress(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                List<ResponseData.RevAddress> revadd = new List<ResponseData.RevAddress>();


                var revenueItems = _context
                                .Get<RevenueActivity>()
                                .AsNoTracking()
                                .Where(RevenueActivity.IsActiveFilter)
                                .Where(x => x.ActivityId == activityId)
                                .Select(x => new { address = x.Address, City = x.City.CityCode, State = x.City.StateCode }
                                )
                                .Distinct()
                                .ToList();
                foreach (var item in revenueItems)
                {
                    ResponseData.RevAddress rev = new ResponseData.RevAddress();
                    rev.address = item.address;
                    rev.City = item.City;
                    rev.State = item.State;
                    revadd.Add(rev);
                }
                return revadd;
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
             join u in _context.Get<User>() on a.ForemanUserId equals u.Id
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

        public static void PopulateJobInfoUsingResurfacing(long resurfacingId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobinfo =
            (from r in _context.Get<Resurfacing>()
             join j in _context.Get<CrewlinkServices.Core.Models.Job>() on r.JobId equals j.Id
             join s in _context.Get<Employee>() on j.SuperitendentEmployeeNumber equals s.EmployeeNumber
             join u in _context.Get<User>() on r.ForemanUserId equals u.Id
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

        private static string ReturnFileHTML(string path, string partial, ResponseData response = null)
        {
            if (response == null)
            { return Razor.Parse(File.ReadAllText(path + partial)); }
            else
            { return Razor.Parse(File.ReadAllText(path + partial), response); }

        }

        private static void GetRevenue(long activityId, ResponseData response, ResponseData.RevAddress address)
        {
            PopulateDteRevenue(activityId, response, address);

            response.PayitemCount = response.RevenueItems.SelectMany(x => x.Records).Count();

            response.PayitemComments = response.JobComments.Where(x => x.CommentType == "P").Select(x => x.Comment).FirstOrDefault();
        }

        public static void PopulateDteRevenue(long activityId, BaseActivityQueryResponse response, ResponseData.RevAddress address)
        {
            using (var _context = new ApplicationContext())
            {
                var revenueItems = _context
                .Get<RevenueActivity>()
                .AsNoTracking()
                .Where(RevenueActivity.IsActiveFilter)
                .Where(x => x.ActivityId == activityId)
                .Where(x => x.Address == address.address)
                .Where(x => x.City.CityCode == address.City)
                .Where(x => x.City.StateCode == address.State)
                .GroupBy(x => new { x.PayItem.PayItemCode, x.PayItem.PayItemDescription, x.PayItem.PayItemCustomDescription })
                .Select(x => new BaseActivityQueryResponse.Revenue
                {
                    PayItemCode = x.Key.PayItemCode,
                    PayItemDescription = x.Key.PayItemDescription,
                    PayItemCustomDescription = x.Key.PayItemCustomDescription,
                    Records = x.Select(r => new BaseActivityQueryResponse.Revenue.Record
                    {
                        Id = r.Id,
                        PayItemId = r.PayItemId,
                        PayItemCode = r.PayItem.PayItemCode,
                        WbsCode = r.PayItem.WbsCode,
                        WbsDescription = r.PayItem.WbsDescription,
                        UnitOfMeasure = r.PayItem.UnitOfMeasure,
                        CityId = r.CityId,
                        City = r.City.CityCode,
                        State = r.City.StateCode,
                        Address = r.Address,
                        WorkOrderNumber = r.WorkOrderNumber,
                        PurchaseOrderNumber = r.PurchaseOrderNumber,
                        CustomerId = r.CustomerId,
                        Customer = r.Customer.Code,
                        Quantity = r.Quantity,
                        RelatedWorkOrderNumber = r.RelatedWorkOrderNumber,
                        PayItemDescription = r.PayItem.PayItemDescription,
                    })
                    .OrderBy(r => r.WbsCode)
                }).ToList();


                var revenueItemsByAddress = _context
                    .Get<RevenueActivity>()
                    .AsNoTracking()
                    .Where(RevenueActivity.IsActiveFilter)
                    .Where(x => x.ActivityId == activityId)
                    .GroupBy(x => new { x.Address, x.RelatedWorkOrderNumber, x.PayItem.PayItemCode })
                    .Select(x => new BaseActivityQueryResponse.Revenue
                    {
                        Address = x.Key.Address,
                        PayItemCode = x.Key.PayItemCode,
                        WorkOrderNumber = x.Key.RelatedWorkOrderNumber,
                        Records = x.Select(r => new BaseActivityQueryResponse.Revenue.Record
                        {
                            Id = r.Id,
                            PayItemId = r.PayItemId,
                            PayItemCode = r.PayItem.PayItemCode,
                            WbsCode = r.PayItem.WbsCode,
                            WbsDescription = r.PayItem.WbsDescription,
                            UnitOfMeasure = r.PayItem.UnitOfMeasure,
                            CityId = r.CityId,
                            City = r.City.CityCode,
                            State = r.City.StateCode,
                            Address = r.Address,
                            WorkOrderNumber = r.WorkOrderNumber,
                            PurchaseOrderNumber = r.PurchaseOrderNumber,
                            CustomerId = r.CustomerId,
                            Customer = r.Customer.Code,
                            Quantity = r.Quantity,
                            RelatedWorkOrderNumber = r.RelatedWorkOrderNumber
                        })
                        .OrderBy(r => r.WbsCode)
                    }).ToList();

                response.RevenueItems = revenueItems.OrderBy(x => x.PayItemCode);
                response.RevenueItemsByAddress = revenueItemsByAddress.OrderBy(x => x.Address);
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
            var result = GetStandardAdditionalInfo.GetData(activityId, dfrId);

            if (result.AdditionalInfo != null)
            {
                response.ReviewerComments = result.AdditionalInfo.Where(x => x.Name.Contains("Reviewer")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
            }
        }

        public static void GetCutSheetsData(long resurfacingId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateCutSheetsData(resurfacingId, response);
        }


        public static void GetRestorationData(long resurfacingId, ResponseData response)
        {
            SharedBaseActivityHandler.PopulateRestorationData(resurfacingId, response);
        }

        public static  List<ResponseData.RevAddress> GetWorkOrderAddress(long ActivityId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {
                List<ResponseData.RevAddress> revadd = new List<ResponseData.RevAddress>();

                string[] splitworkOrder = workOrder.Split(',');

                var id = long.Parse(splitworkOrder[1]);

                var WO = splitworkOrder[0];

                var revenueItems = _context
                                .Get<RevenueActivity>()
                                .AsNoTracking()
                                .Where(RevenueActivity.IsActiveFilter)
                                .Where(x => x.ActivityId == id && x.WorkOrderNumber == WO)
                                .Select(x => new { address = x.Address, City = x.City.CityCode, State = x.City.StateCode }
                                )
                                .Distinct()
                                .ToList();
                foreach (var item in revenueItems)
                {
                    ResponseData.RevAddress rev = new ResponseData.RevAddress();
                    rev.address = item.address;
                    rev.City = item.City;
                    rev.State = item.State;
                    revadd.Add(rev);
                }
                return revadd;
            }
        }

        public static  List<ResponseData.RevAddress> GetFirstWorkOrderAddress(long ActivityId, string workOrder)
        {
            using (var _context = new ApplicationContext())
            {
                List<ResponseData.RevAddress> revadd = new List<ResponseData.RevAddress>();


                string[] splitworkOrder = workOrder.Split(',');

                var id = long.Parse(splitworkOrder[1]);

                var WO = splitworkOrder[0];

                var address = _context
                .Get<RevenueActivity>()
                .AsNoTracking()
                .Where(RevenueActivity.IsActiveFilter)
                .Where(r => r.ActivityId == id && r.WorkOrderNumber == WO)
                 .Select(x => new { address = x.Address, City = x.City.CityCode, State = x.City.StateCode })
                 .First();

                var item = new ResponseData.RevAddress()
                {
                    address = address.address,
                    City = address.City,
                    State = address.State
                };

                revadd.Add(item);
                return revadd;
            }
        }

        private static void  GetRevenueWorkOrder(GetDFRToken.Request request, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                if (request.workOrders != "" || request.workOrders != null)
                {
                    string[] splitworkOrder = request.workOrders.Split(',');

                    var id = long.Parse(splitworkOrder[1]);

                    var WO = splitworkOrder[0];

                    var revenueItemsByAddress1 = _context
                    .Get<RevenueActivity>()
                    .AsNoTracking()
                    .Where(RevenueActivity.IsActiveFilter)
                    .Where(r => r.ActivityId == id && r.WorkOrderNumber == WO)
                    .Select(r => new BaseActivityQueryResponse.Revenue.Record
                    {
                        Id = r.Id,
                        PayItemId = r.PayItemId,
                        PayItemCode = r.PayItem.PayItemCode,
                        WbsCode = r.PayItem.WbsCode,
                        WbsDescription = r.PayItem.WbsDescription,
                        UnitOfMeasure = r.PayItem.UnitOfMeasure,
                        CityId = r.CityId,
                        City = r.City.CityCode,
                        State = r.City.StateCode,
                        Address = r.Address,
                        WorkOrderNumber = r.WorkOrderNumber,
                        PurchaseOrderNumber = r.PurchaseOrderNumber,
                        CustomerId = r.CustomerId,
                        Customer = r.Customer.Code,
                        Quantity = r.Quantity,
                        RelatedWorkOrderNumber = r.RelatedWorkOrderNumber,
                        PayItemDescription = r.PayItem.PayItemDescription
                    })
                        .OrderBy(r => r.WbsCode)
                        .ToList();

                    var responseRecords = new List<BaseActivityQueryResponse.Revenue>();

                    if (revenueItemsByAddress1.Count > 0)
                    {
                        foreach (var payItem in revenueItemsByAddress1)
                        {
                            var record = new List<BaseActivityQueryResponse.Revenue.Record>();

                            var firstRecord = new BaseActivityQueryResponse.Revenue.Record()
                            {
                                Id = payItem.Id,
                                PayItemId = payItem.PayItemId,
                                PayItemCode = payItem.PayItemCode,
                                WbsCode = payItem.WbsCode,
                                WbsDescription = payItem.WbsDescription,
                                UnitOfMeasure = payItem.UnitOfMeasure,
                                CityId = payItem.CityId,
                                City = payItem.City,
                                State = payItem.State,
                                Address = payItem.Address,
                                WorkOrderNumber = payItem.WorkOrderNumber,
                                PurchaseOrderNumber = payItem.PurchaseOrderNumber,
                                CustomerId = payItem.CustomerId,
                                Customer = payItem.Customer,
                                Quantity = payItem.Quantity,
                                RelatedWorkOrderNumber = payItem.RelatedWorkOrderNumber
                            };

                            record.Add(firstRecord);

                            var item = new BaseActivityQueryResponse.Revenue()
                            {
                                Address = payItem.Address,
                                PayItemCode = payItem.PayItemCode,
                                WorkOrderNumber = payItem.WorkOrderNumber,
                                PayItemDescription = payItem.PayItemDescription,
                                Records = record

                            };
                            responseRecords.Add(item);
                        }
                    }

                    response.RevenueItems = responseRecords;
                }
            }
        }
    }
}
