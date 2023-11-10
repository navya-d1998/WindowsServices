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
using static CrewlinkServices.Features.DailyActivity.DFRWglBundle.GetWglBundle.QueryResponse;

namespace Crewlink.WindowsServices.Features
{
    public class GetPecoDFRData
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

            public int SubcontractorCount { get; set; }
            public int MaterialCount { get; set; }
            public int RentalCount { get; set; }
            public int TruckingCount { get; set; }

            public int UserId { get; set; }

            public string generalForeman { get; set; }

            public string dayOrNight { get; set; }
            public string polepickup { get; set; }
            public string restoration { get; set; }

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            public CrewlinkServices.Features.DailyActivity.DFRWglBundle.GetAdditionalInfo.Response DFRAdditionaInfo { get; set; }

            public IEnumerable<RevenueData> PecoRevenue { get; set; } = new List<RevenueData>();

            public List<AdditionalInfo.LaborAdditionalInfo> laborAdditionalInfo { get; set; }


            public List<AdditionalInfo.EquipmentAdditionalInfo> equipmentAdditionalInfo { get; set; }
            public List<AdditionalInfo.PolePickupAdditionalInfo> polepickupAdditionalInfo { get; set; }
            public List<AdditionalInfo.RestorationAdditionalInfo> restorationAdditionalInfo { get; set; }
            public class RevenueData
            {
                public string PayItemCode { get; set; }

                public string PayItemDescription { get; set; }

                public decimal Quantity { get; set; }

                public string UnitOfMeasure { get; set; }

                public string PurchaseOrderNumber { get; set; }

                public string WorkOrderNumber { get; set; }

                public string Address { get; set; }
            }

            public class AdditionalInfo
            {
                public class LaborAdditionalInfo
                {
                    public WglData LaborName = new WglData();

                    public WglData LaborHours_ST = new WglData();

                    public WglData LaborHours_OT = new WglData();

                    public WglData LaborHours_DT = new WglData();

                }

                public class EquipmentAdditionalInfo
                {
                    public WglData TruckName = new WglData();
                    public WglData TruckHours = new WglData();
                }

                public class PolePickupAdditionalInfo
                {
                    public WglData PickupAddress = new WglData();

                    public WglData PickupCrossStreet = new WglData();

                    public WglData PolePieceSize = new WglData();

                    public WglData PoleQuad = new WglData();

                    public WglData NumPolePieces = new WglData();

                }
                public class RestorationAdditionalInfo
                {
                    public WglData RestAddress = new WglData();

                    public WglData RestCrossStreet = new WglData();

                    public WglData Dimensions = new WglData();

                    public WglData RestQuad = new WglData();

                    public WglData Pole = new WglData();


                }
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

                public string PayLevelDescription { get; set; }
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
                        cacheKeyValue.Split('_').Count() == 14)
                    {
                        string[] SplitKey = cacheKeyValue.Split('_');
                        request = new GetDFRToken.Request();
                        request.FileName = SplitKey[0] + "_" + SplitKey[1] + "_" + SplitKey[2] + "_" + SplitKey[3];
                        request.ActivityId = long.Parse(SplitKey[4]);
                        request.ProcessDateAndTime = SplitKey[5] + " " + SplitKey[6];
                        request.ShowPayitem = bool.Parse(SplitKey[7]);
                        request.ShowLabor = bool.Parse(SplitKey[8]);
                        request.ShowEquipment = bool.Parse(SplitKey[9]);
                        request.ShowForemanComments = bool.Parse(SplitKey[10]);
                        request.ShowApproverReviewerComments = bool.Parse(SplitKey[11]);
                        request.ShowSignature = bool.Parse(SplitKey[12]);
                        request.WorkOrderNumber = SplitKey[13];
                        _cache.Insert(cacheKeyValue, request, 10);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();

                 PopulateJobInfo(request.ActivityId, response);

                 SharedBaseActivityHandler.PopulateJobDetails(request.ActivityId, response);

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");


              //  string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "..\\..\\..\\Crewlink.Services\\Features\\DailyActivity\\Templates\\");

                response.LogoImagePath = Path.Combine(BaseURL, "Images\\ifs-logo-bw.png");


                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "Images\\ifs-logo-bw.png");

                response.ShowForemanComments = request.ShowForemanComments;

                var tempVale = "";

                int currentDFRId = SharedDFRDataRepository.GetDfrId("PECO");

                response.UniqueFilename = request.FileName;

                GetAdditionalInfo(request.ActivityId, response, currentDFRId);

                GetRevenue(request.ActivityId, response, request.WorkOrderNumber.Trim());

                var dfrTemplate = ReturnFileHTML(BaseURL, "DFR_Peco_Header.cshtml", response);

                // await GetWorkOrderNumber(request.ActivityId, response, request.ShowForemanComments, request.WorkOrderNumber.Trim());

                if (request.ShowForemanComments)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Peco_Foreman_comments.cshtml", response);
                }

                if (request.ShowLabor)
                {
                    GetLabor(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Peco_Labor.cshtml", response);
                }

                if (request.ShowEquipment)
                {
                    GetEquipment(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Peco_Equipment.cshtml", response);
                }

                if (request.ShowApproverReviewerComments && response.ReviewerComments != null)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Additional_Info.cshtml", response);
                }

                tempVale += ReturnFileHTML(BaseURL, "DFR_Peco_Additional_Info.cshtml", response);

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

                dfrTemplate += ReturnFileHTML(BaseURL, "DFR_Peco_Signature.cshtml", response);

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

        private static string ReturnFileHTML(string path, string partial, ResponseData response = null)
        {
            if (response == null)
            { return Razor.Parse(File.ReadAllText(path + partial)); }
            else
            { return Razor.Parse(File.ReadAllText(path + partial), response); }

        }
        private static void GetWorkOrderNumber(long activityID, ResponseData response, bool showForemanComments, string WorkOrder)
        {
            using (var _context = new ApplicationContext())
            {
                var result =  _context.Get<RevenueActivity>()
           .Where(RevenueActivity.IsActiveFilter)
           .Where(x => x.ActivityId == activityID)
           .Select(x => new
           {
               WorkOrderNumber = x.WorkOrderNumber,
               JobPayItemId = x.Id
           }).Distinct().ToList();
                //if (showForemanComments)
                //{
                //    foreach (var cmt in response.ForemanComments)
                //    {
                //        if (cmt.WorkOrder != null && cmt.WorkOrder.Value != null)
                //            cmt.WorkOrder.Value = result.Where(x => x.JobPayItemId == Convert.ToInt64(cmt.WorkOrder.Value)).FirstOrDefault().WorkOrderNumber;
                //    }
                //}
                foreach (var sub in response.Subcontractors)
                {
                    if (sub.WorkOrder != null && sub.WorkOrder.Value != null)
                        sub.WorkOrder.Value = result.Where(x => x.JobPayItemId == Convert.ToInt64(sub.WorkOrder.Value)).FirstOrDefault()?.WorkOrderNumber;
                }
                foreach (var mat in response.Materials)
                {
                    if (mat.WorkOrder != null && mat.WorkOrder.Value != null)
                        mat.WorkOrder.Value = result.Where(x => x.JobPayItemId == Convert.ToInt64(mat.WorkOrder.Value)).FirstOrDefault()?.WorkOrderNumber;
                }
                foreach (var rent in response.Rentals)
                {
                    if (rent.WorkOrder != null && rent.WorkOrder.Value != null)
                        rent.WorkOrder.Value = result.Where(x => x.JobPayItemId == Convert.ToInt64(rent.WorkOrder.Value)).FirstOrDefault()?.WorkOrderNumber;
                }
                foreach (var tru in response.Trucking)
                {
                    if (tru.WorkOrder != null && tru.WorkOrder.Value != null)
                        tru.WorkOrder.Value = result.Where(x => x.JobPayItemId == Convert.ToInt64(tru.WorkOrder.Value)).FirstOrDefault()?.WorkOrderNumber;
                }
                if (WorkOrder != "all")
                {
                    response.Subcontractors = response.Subcontractors.Where(y => y.WorkOrder != null).Where(x => x.WorkOrder.Value == WorkOrder).ToList();
                    response.Materials = response.Materials.Where(y => y.WorkOrder != null).Where(x => x.WorkOrder.Value == WorkOrder).ToList();
                    response.Rentals = response.Rentals.Where(y => y.WorkOrder != null).Where(x => x.WorkOrder.Value == WorkOrder).ToList();
                    response.Trucking = response.Trucking.Where(y => y.WorkOrder != null).Where(x => x.WorkOrder.Value == WorkOrder).ToList();
                    //response.ForemanComments = response.ForemanComments.Where(y => y.WorkOrder != null).Where(x => x.WorkOrder.Value == WorkOrder).ToList();
                }
            }
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
                                        .Select(x => new ResponseData.RevenueData
                                        {
                                            PayItemCode = x.PayItem.PayItemCode,
                                        //PayItemDescription = x.PayItem.PayItemDescription,
                                        PayItemDescription = x.PayItem.PayItemCustomDescription,
                                            UnitOfMeasure = x.PayItem.UnitOfMeasure,
                                            Address = x.Address + " - " + x.City.CityCode + " - " + x.City.StateCode,
                                            WorkOrderNumber = x.WorkOrderNumber,
                                            PurchaseOrderNumber = x.PurchaseOrderNumber,
                                            Quantity = x.Quantity
                                        }).OrderBy(x => x.PayItemDescription).ToList();

                if (!workOrderNumber.ToLower().Equals("all"))
                { revenueItems = revenueItems.Where(x => x.WorkOrderNumber == workOrderNumber).ToList(); }

                response.PecoRevenue = revenueItems;

                response.PurchaseOrder = response.PecoRevenue.Select(x => x.PurchaseOrderNumber).FirstOrDefault();

                response.WorkOrder = response.PecoRevenue.Select(x => x.WorkOrderNumber).FirstOrDefault();

                response.Location = response.PecoRevenue.Select(x => x.Address).FirstOrDefault();

                response.PayitemCount = response.PecoRevenue.Count();

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
                                            result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours)),
                        PayLevelDescription = result.Select(x => x.Records.Select(p => p.PayLevelDescription).FirstOrDefault()).FirstOrDefault()
                    }).Where(x => x.TotalHours > 0).ToList();

            // response.LaborActivity = laborGroup;
            List<string> newLabor = new List<string>();
            List<ResponseData.LaborActivityTotal> NewLabour = new List<ResponseData.LaborActivityTotal>();
            foreach (var name in laborGroup)
            {
                string sub = name.EmployeeName;
                newLabor.Add(sub);
            }
            newLabor.Sort();
            foreach (var name in newLabor)
            {
                foreach (var labor in laborGroup)
                {
                    if (name == labor.EmployeeName)
                    {
                        NewLabour.Add(labor);
                    }
                }
            }
            response.LaborActivity = NewLabour;
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

        private static void GetAdditionalInfo(long activityId, ResponseData response, int dfrId)
        {
            var result = GetPecoDFRAdditionalInfo.GetData(activityId, dfrId);

            var subcontractorsVendor = result.AdditionalInfo.Where(x => x.Name.Contains("Subcontractors_Vendor")).SelectMany(x => x.Data).ToList();
            var subcontractorsTicket = result.AdditionalInfo.Where(x => x.Name.Contains("Subcontractors_Ticket_Number")).SelectMany(x => x.Data).ToList();
            var subcontractorsQty = result.AdditionalInfo.Where(x => x.Name.Contains("Subcontractors_Qty_Hour")).SelectMany(x => x.Data).ToList();
            var subcontractorsWorkOrder = result.AdditionalInfo.Where(x => x.Name.Contains("Subcontractors_Work_Order")).SelectMany(x => x.Data).ToList();
            var materialsVendor = result.AdditionalInfo.Where(x => x.Name.Contains("Materials_Vendor")).SelectMany(x => x.Data).ToList();
            var materialsTicket = result.AdditionalInfo.Where(x => x.Name.Contains("Materials_Ticket_Number")).SelectMany(x => x.Data).ToList();
            var materialsQty = result.AdditionalInfo.Where(x => x.Name.Contains("Materials_Qty_Hour")).SelectMany(x => x.Data).ToList();
            var materialsWorkOrder = result.AdditionalInfo.Where(x => x.Name.Contains("Materials_Work_Order")).SelectMany(x => x.Data).ToList();
            var rentalsVendor = result.AdditionalInfo.Where(x => x.Name.Contains("Rentals_Vendor")).SelectMany(x => x.Data).ToList();
            var rentalsTicket = result.AdditionalInfo.Where(x => x.Name.Contains("Rentals_Ticket_Number")).SelectMany(x => x.Data).ToList();
            var rentalsQty = result.AdditionalInfo.Where(x => x.Name.Contains("Rentals_Qty_Hour")).SelectMany(x => x.Data).ToList();
            var rentalsWorkOrder = result.AdditionalInfo.Where(x => x.Name.Contains("Rentals_Work_Order")).SelectMany(x => x.Data).ToList();
            var truckingVendor = result.AdditionalInfo.Where(x => x.Name.Contains("Trucking_Vendor")).SelectMany(x => x.Data).ToList();
            var truckingTicket = result.AdditionalInfo.Where(x => x.Name.Contains("Trucking_Ticket_Number")).SelectMany(x => x.Data).ToList();
            var truckingQty = result.AdditionalInfo.Where(x => x.Name.Contains("Trucking_Qty_Hour")).SelectMany(x => x.Data).ToList();
            var truckingWorkOrder = result.AdditionalInfo.Where(x => x.Name.Contains("Trucking_Work_Order")).SelectMany(x => x.Data).ToList();
            //var foremanComments = result.AdditionalInfo.Where(x => x.Name.Contains("Foreman_Comments")).SelectMany(x => x.Data).ToList();
            var foremanWorkOrder = result.AdditionalInfo.Where(x => x.Name.Contains("Foreman_Work_Order")).SelectMany(x => x.Data).ToList();
            var generalForeman = result.AdditionalInfo.Where(x => x.Name.Contains("GeneralForeman")).SelectMany(x => x.Data).FirstOrDefault();
            var DayOrNight = result.AdditionalInfo.Where(x => x.Name.Contains("DayOrNight")).SelectMany(x => x.Data).FirstOrDefault();
            var laborName = result.AdditionalInfo.Where(x => x.Name.Contains("Labor_Name")).SelectMany(x => x.Data).ToList();
            var LaborHoursST = result.AdditionalInfo.Where(x => x.Name.Contains("Labor_Hours_ST")).SelectMany(x => x.Data).ToList();
            var LaborHoursOT = result.AdditionalInfo.Where(x => x.Name.Contains("Labor_Hours_OT")).SelectMany(x => x.Data).ToList();
            var LaborHoursDT = result.AdditionalInfo.Where(x => x.Name.Contains("Labor_Hours_DT")).SelectMany(x => x.Data).ToList();
            var EquipmentTruck = result.AdditionalInfo.Where(x => x.Name.Contains("Equipment_Truck")).SelectMany(x => x.Data).ToList();
            var EquipmentHours = result.AdditionalInfo.Where(x => x.Name.Contains("Equipment_Hours")).SelectMany(x => x.Data).ToList();

            var polepickup = result.AdditionalInfo.Where(x => x.Name.Contains("PolePickup")).SelectMany(x => x.Data).FirstOrDefault();
            var restoration = result.AdditionalInfo.Where(x => x.Name.Contains("Restoration")).SelectMany(x => x.Data).FirstOrDefault();
            var pickupAddress = result.AdditionalInfo.Where(x => x.Name.Contains("Pickup_Address")).SelectMany(x => x.Data).ToList();
            var pickupCrossStreet = result.AdditionalInfo.Where(x => x.Name.Contains("Pickup_CrossStreet")).SelectMany(x => x.Data).ToList();
            var numPolePieces = result.AdditionalInfo.Where(x => x.Name.Contains("Pole_Pieces")).SelectMany(x => x.Data).ToList();
            var polePieceSize = result.AdditionalInfo.Where(x => x.Name.Contains("Pole_Piece_Size")).SelectMany(x => x.Data).ToList();
            var poleQuad = result.AdditionalInfo.Where(x => x.Name.Contains("Pole_Quad")).SelectMany(x => x.Data).ToList();
            var restQuad = result.AdditionalInfo.Where(x => x.Name.Contains("Rest_Quad")).SelectMany(x => x.Data).ToList();
            var restAddress = result.AdditionalInfo.Where(x => x.Name.Contains("Rest_Address")).SelectMany(x => x.Data).ToList();
            var restCrossStreet = result.AdditionalInfo.Where(x => x.Name.Contains("Rest_CrossStreet")).SelectMany(x => x.Data).ToList();
            var dimensions = result.AdditionalInfo.Where(x => x.Name.Contains("Dimensions")).SelectMany(x => x.Data).ToList();
            var pole = result.AdditionalInfo.Where(x => x.Name.Contains("Pole_Number")).SelectMany(x => x.Data).ToList();

            int[] subLength = { subcontractorsVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, subcontractorsTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, subcontractorsQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, subcontractorsWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            int[] matLength = { materialsVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, materialsTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, materialsQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, materialsWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            int[] renLength = { rentalsVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, rentalsTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, rentalsQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, rentalsWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            int[] truLength = { truckingVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, truckingTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, truckingQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, truckingWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            //int[] cmtLength = { foremanComments.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, foremanWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };

            //labor Additional Info
            response.generalForeman = generalForeman.Value;
            response.dayOrNight = DayOrNight.Value;
            List<ResponseData.AdditionalInfo.LaborAdditionalInfo> laboradditional = new List<ResponseData.AdditionalInfo.LaborAdditionalInfo>();
            foreach (var laborN in laborName)
            {
                var laborST = LaborHoursST.Where(x => x.ItemOrder == laborN.ItemOrder).FirstOrDefault();
                var laborOT = LaborHoursOT.Where(x => x.ItemOrder == laborN.ItemOrder).FirstOrDefault();
                var laborDT = LaborHoursDT.Where(x => x.ItemOrder == laborN.ItemOrder).FirstOrDefault();


                WglData hoursST = new WglData();

                if (laborST == null)
                {
                    hoursST.Value = null;
                    hoursST.ItemOrder = laborN.ItemOrder;
                }
                else
                {
                    hoursST.Id = laborST.Id;
                    hoursST.Value = laborST.Value;
                    hoursST.ItemOrder = laborST.ItemOrder;
                }

                WglData hoursOT = new WglData();

                if (laborOT == null)
                {
                    hoursOT.Value = null;
                    hoursOT.ItemOrder = laborN.ItemOrder;
                }
                else
                {
                    hoursOT.Id = laborOT.Id;
                    hoursOT.Value = laborOT.Value;
                    hoursOT.ItemOrder = laborOT.ItemOrder;
                }

                WglData hoursDT = new WglData();

                if (laborDT == null)
                {
                    hoursDT.Value = null;
                    hoursDT.ItemOrder = laborN.ItemOrder;
                }
                else
                {
                    hoursDT.Id = laborDT.Id;
                    hoursDT.Value = laborDT.Value;
                    hoursDT.ItemOrder = laborDT.ItemOrder;
                }
                ResponseData.AdditionalInfo.LaborAdditionalInfo newObj = new ResponseData.AdditionalInfo.LaborAdditionalInfo();
                WglData name = new WglData();
                name.Id = laborN.Id;
                name.Value = laborN.Value;
                name.ItemOrder = laborN.ItemOrder;

                newObj.LaborName = name;
                newObj.LaborHours_ST = hoursST;
                newObj.LaborHours_OT = hoursOT;
                newObj.LaborHours_DT = hoursDT;
                laboradditional.Add(newObj);

            }

            //polepickup Additional Info
            response.polepickup = polepickup.Value;
            List<ResponseData.AdditionalInfo.PolePickupAdditionalInfo> polepickupadditional = new List<ResponseData.AdditionalInfo.PolePickupAdditionalInfo>();
            foreach (var pickupadd in pickupAddress)
            {
                var pickupStreet = pickupCrossStreet.Where(x => x.ItemOrder == pickupadd.ItemOrder).FirstOrDefault();
                var polepiecesize = polePieceSize.Where(x => x.ItemOrder == pickupadd.ItemOrder).FirstOrDefault();
                var polequad = poleQuad.Where(x => x.ItemOrder == pickupadd.ItemOrder).FirstOrDefault();
                var numPieces = numPolePieces.Where(x => x.ItemOrder == pickupadd.ItemOrder).FirstOrDefault();

                WglData pickupStreetVal = new WglData();

                if (pickupStreet == null)
                {
                    pickupStreetVal.Value = null;
                    pickupStreetVal.ItemOrder = pickupadd.ItemOrder;
                }
                else
                {
                    pickupStreetVal.Id = pickupStreet.Id;
                    pickupStreetVal.Value = pickupStreet.Value;
                    pickupStreetVal.ItemOrder = pickupStreet.ItemOrder;
                }

                WglData polepiecesizeVal = new WglData();

                if (polepiecesize == null)
                {
                    polepiecesizeVal.Value = null;
                    polepiecesizeVal.ItemOrder = pickupadd.ItemOrder;
                }
                else
                {
                    polepiecesizeVal.Id = polepiecesize.Id;
                    polepiecesizeVal.Value = polepiecesize.Value;
                    polepiecesizeVal.ItemOrder = polepiecesize.ItemOrder;
                }

                WglData polequadVal = new WglData();

                if (polequad == null)
                {
                    polequadVal.Value = null;
                    polequadVal.ItemOrder = pickupadd.ItemOrder;
                }
                else
                {
                    polequadVal.Id = polequad.Id;
                    polequadVal.Value = polequad.Value;
                    polequadVal.ItemOrder = polequad.ItemOrder;
                }

                WglData numPiecesVal = new WglData();

                if (numPieces == null)
                {
                    numPiecesVal.Value = null;
                    numPiecesVal.ItemOrder = pickupadd.ItemOrder;
                }
                else
                {
                    numPiecesVal.Id = numPieces.Id;
                    numPiecesVal.Value = numPieces.Value;
                    numPiecesVal.ItemOrder = numPieces.ItemOrder;
                }

                ResponseData.AdditionalInfo.PolePickupAdditionalInfo newObj = new ResponseData.AdditionalInfo.PolePickupAdditionalInfo();
                WglData address = new WglData();
                address.Id = pickupadd.Id;
                address.Value = pickupadd.Value;
                address.ItemOrder = pickupadd.ItemOrder;

                newObj.PickupAddress = address;
                newObj.PickupCrossStreet = pickupStreetVal;
                newObj.PolePieceSize = polepiecesizeVal;
                newObj.PoleQuad = polequadVal;
                newObj.NumPolePieces = numPiecesVal;
                polepickupadditional.Add(newObj);

            }


            //Restoration Additional Info
            response.restoration = restoration.Value;
            List<ResponseData.AdditionalInfo.RestorationAdditionalInfo> restorationadditional = new List<ResponseData.AdditionalInfo.RestorationAdditionalInfo>();
            foreach (var restadd in restAddress)
            {
                var restStreet = restCrossStreet.Where(x => x.ItemOrder == restadd.ItemOrder).FirstOrDefault();
                var dims = dimensions.Where(x => x.ItemOrder == restadd.ItemOrder).FirstOrDefault();
                var restquad = restQuad.Where(x => x.ItemOrder == restadd.ItemOrder).FirstOrDefault();
                var pl = pole.Where(x => x.ItemOrder == restadd.ItemOrder).FirstOrDefault();

                WglData restStreetVal = new WglData();

                if (restStreet == null)
                {
                    restStreetVal.Value = null;
                    restStreetVal.ItemOrder = restadd.ItemOrder;
                }
                else
                {
                    restStreetVal.Id = restStreet.Id;
                    restStreetVal.Value = restStreet.Value;
                    restStreetVal.ItemOrder = restStreet.ItemOrder;
                }

                WglData dimsVal = new WglData();

                if (dims == null)
                {
                    dimsVal.Value = null;
                    dimsVal.ItemOrder = restadd.ItemOrder;
                }
                else
                {
                    dimsVal.Id = dims.Id;
                    dimsVal.Value = dims.Value;
                    dimsVal.ItemOrder = dims.ItemOrder;
                }

                WglData restquadVal = new WglData();

                if (restquad == null)
                {
                    restquadVal.Value = null;
                    restquadVal.ItemOrder = restadd.ItemOrder;
                }
                else
                {
                    restquadVal.Id = restquad.Id;
                    restquadVal.Value = restquad.Value;
                    restquadVal.ItemOrder = restquad.ItemOrder;
                }

                WglData poleVal = new WglData();

                if (pl == null)
                {
                    poleVal.Value = null;
                    poleVal.ItemOrder = restadd.ItemOrder;
                }
                else
                {
                    poleVal.Id = pl.Id;
                    poleVal.Value = pl.Value;
                    poleVal.ItemOrder = pl.ItemOrder;
                }

                ResponseData.AdditionalInfo.RestorationAdditionalInfo newObj = new ResponseData.AdditionalInfo.RestorationAdditionalInfo();
                WglData address = new WglData();
                address.Id = restadd.Id;
                address.Value = restadd.Value;
                address.ItemOrder = restadd.ItemOrder;

                newObj.RestAddress = address;
                newObj.RestCrossStreet = restStreetVal;
                newObj.Dimensions = dimsVal;
                newObj.RestQuad = restquadVal;
                newObj.Pole = poleVal;
                restorationadditional.Add(newObj);
            }

            List<ResponseData.AdditionalInfo.EquipmentAdditionalInfo> EquipmentAdditional = new List<ResponseData.AdditionalInfo.EquipmentAdditionalInfo>();
            foreach (var equipmentT in EquipmentTruck)
            {
                foreach (var equipmentH in EquipmentHours)
                {
                    if (equipmentT.ItemOrder == equipmentH.ItemOrder)
                    {
                        ResponseData.AdditionalInfo.EquipmentAdditionalInfo newObjT = new ResponseData.AdditionalInfo.EquipmentAdditionalInfo();
                        WglData truck = new WglData();
                        truck.Id = equipmentT.Id;
                        truck.Value = equipmentT.Value;
                        truck.ItemOrder = equipmentT.ItemOrder;
                        WglData Hours = new WglData();
                        Hours.Id = equipmentH.Id;
                        Hours.Value = equipmentH.Value;
                        Hours.ItemOrder = equipmentH.ItemOrder;
                        newObjT.TruckName = truck;
                        newObjT.TruckHours = Hours;
                        EquipmentAdditional.Add(newObjT);
                    }

                }
            }

            response.laborAdditionalInfo = laboradditional;
            response.equipmentAdditionalInfo = EquipmentAdditional;
            response.polepickupAdditionalInfo = polepickupadditional;
            response.restorationAdditionalInfo = restorationadditional;
            var sub = new WglPassThru() { };
            foreach (var vendor in subcontractorsVendor)
            {
                var wgl = new WglData();
                wgl.Id = vendor.Id;
                wgl.Value = vendor.Value;
                wgl.ItemOrder = (vendor.ItemOrder == null) ? 0 : vendor.ItemOrder;
                sub.Vendor.Add(wgl);
            }
            foreach (var ticket in subcontractorsTicket)
            {
                var wgl = new WglData();
                wgl.Id = ticket.Id;
                wgl.Value = ticket.Value;
                wgl.ItemOrder = (ticket.ItemOrder == null) ? 0 : ticket.ItemOrder;
                sub.Ticket.Add(wgl);
            }
            foreach (var qty in subcontractorsQty)
            {
                var wgl = new WglData();
                wgl.Id = qty.Id;
                wgl.Value = qty.Value;
                wgl.ItemOrder = (qty.ItemOrder == null) ? 0 : qty.ItemOrder;
                sub.Hours.Add(wgl);
            }
            foreach (var wo in subcontractorsWorkOrder)
            {
                var wgl = new WglData();
                wgl.Id = wo.Id;
                wgl.Value = wo.Value;
                wgl.ItemOrder = (wo.ItemOrder == null) ? 0 : wo.ItemOrder;
                sub.WorkOrder.Add(wgl);
            }

            for (int j = 0; j <= subLength.Max(); j++)
            {
                var subcontract = new WglRowData();
                subcontract.Vendor = sub.Vendor.Where(x => x.ItemOrder == j).FirstOrDefault();
                subcontract.Ticket = sub.Ticket.Where(x => x.ItemOrder == j).FirstOrDefault();
                subcontract.Hours = sub.Hours.Where(x => x.ItemOrder == j).FirstOrDefault();
                subcontract.WorkOrder = sub.WorkOrder.Where(x => x.ItemOrder == j).FirstOrDefault();
                response.Subcontractors.Add(subcontract);
            }
            var mat = new WglPassThru() { };
            foreach (var vendor in materialsVendor)
            {
                var wgl = new WglData();
                wgl.Id = vendor.Id;
                wgl.Value = vendor.Value;
                wgl.ItemOrder = (vendor.ItemOrder == null) ? 0 : vendor.ItemOrder;
                mat.Vendor.Add(wgl);
            }
            foreach (var ticket in materialsTicket)
            {
                var wgl = new WglData();
                wgl.Id = ticket.Id;
                wgl.Value = ticket.Value;
                wgl.ItemOrder = (ticket.ItemOrder == null) ? 0 : ticket.ItemOrder;
                mat.Ticket.Add(wgl);
            }
            foreach (var qty in materialsQty)
            {
                var wgl = new WglData();
                wgl.Id = qty.Id;
                wgl.Value = qty.Value;
                wgl.ItemOrder = (qty.ItemOrder == null) ? 0 : qty.ItemOrder;
                mat.Hours.Add(wgl);
            }
            foreach (var wo in materialsWorkOrder)
            {
                var wgl = new WglData();
                wgl.Id = wo.Id;
                wgl.Value = wo.Value;
                wgl.ItemOrder = (wo.ItemOrder == null) ? 0 : wo.ItemOrder;
                mat.WorkOrder.Add(wgl);
            }
            for (int j = 0; j <= matLength.Max(); j++)
            {
                var material = new WglRowData();
                material.Vendor = mat.Vendor.Where(x => x.ItemOrder == j).FirstOrDefault();
                material.Ticket = mat.Ticket.Where(x => x.ItemOrder == j).FirstOrDefault();
                material.Hours = mat.Hours.Where(x => x.ItemOrder == j).FirstOrDefault();
                material.WorkOrder = mat.WorkOrder.Where(x => x.ItemOrder == j).FirstOrDefault();
                response.Materials.Add(material);
            }
            var ren = new WglPassThru() { };
            foreach (var vendor in rentalsVendor)
            {
                var wgl = new WglData();
                wgl.Id = vendor.Id;
                wgl.Value = vendor.Value;
                wgl.ItemOrder = (vendor.ItemOrder == null) ? 0 : vendor.ItemOrder;
                ren.Vendor.Add(wgl);
            }
            foreach (var ticket in rentalsTicket)
            {
                var wgl = new WglData();
                wgl.Id = ticket.Id;
                wgl.Value = ticket.Value;
                wgl.ItemOrder = (ticket.ItemOrder == null) ? 0 : ticket.ItemOrder;
                ren.Ticket.Add(wgl);
            }
            foreach (var qty in rentalsQty)
            {
                var wgl = new WglData();
                wgl.Id = qty.Id;
                wgl.Value = qty.Value;
                wgl.ItemOrder = (qty.ItemOrder == null) ? 0 : qty.ItemOrder;

                ren.Hours.Add(wgl);
            }
            foreach (var wo in rentalsWorkOrder)
            {
                var wgl = new WglData();
                wgl.Id = wo.Id;
                wgl.Value = wo.Value;
                wgl.ItemOrder = (wo.ItemOrder == null) ? 0 : wo.ItemOrder;
                ren.WorkOrder.Add(wgl);
            }
            for (int j = 0; j <= renLength.Max(); j++)
            {
                var rental = new WglRowData();
                rental.Vendor = ren.Vendor.Where(x => x.ItemOrder == j).FirstOrDefault();
                rental.Ticket = ren.Ticket.Where(x => x.ItemOrder == j).FirstOrDefault();
                rental.Hours = ren.Hours.Where(x => x.ItemOrder == j).FirstOrDefault();
                rental.WorkOrder = ren.WorkOrder.Where(x => x.ItemOrder == j).FirstOrDefault();
                response.Rentals.Add(rental);
            }

            var tru = new WglPassThru() { };
            foreach (var vendor in truckingVendor)
            {
                var wgl = new WglData();
                wgl.Id = vendor.Id;
                wgl.Value = vendor.Value;
                wgl.ItemOrder = (vendor.ItemOrder == null) ? 0 : vendor.ItemOrder;
                tru.Vendor.Add(wgl);
            }
            foreach (var ticket in truckingTicket)
            {
                var wgl = new WglData();
                wgl.Id = ticket.Id;
                wgl.Value = ticket.Value;
                wgl.ItemOrder = (ticket.ItemOrder == null) ? 0 : ticket.ItemOrder;
                tru.Ticket.Add(wgl);
            }
            foreach (var qty in truckingQty)
            {
                var wgl = new WglData();
                wgl.Id = qty.Id;
                wgl.Value = qty.Value;
                wgl.ItemOrder = (qty.ItemOrder == null) ? 0 : qty.ItemOrder;
                tru.Hours.Add(wgl);
            }
            foreach (var wo in truckingWorkOrder)
            {
                var wgl = new WglData();
                wgl.Id = wo.Id;
                wgl.Value = wo.Value;
                wgl.ItemOrder = (wo.ItemOrder == null) ? 0 : wo.ItemOrder;
                tru.WorkOrder.Add(wgl);
            }
            for (int j = 0; j <= truLength.Max(); j++)
            {
                var trucking = new WglRowData();
                trucking.Vendor = tru.Vendor.Where(x => x.ItemOrder == j).FirstOrDefault();
                trucking.Ticket = tru.Ticket.Where(x => x.ItemOrder == j).FirstOrDefault();
                trucking.Hours = tru.Hours.Where(x => x.ItemOrder == j).FirstOrDefault();
                trucking.WorkOrder = tru.WorkOrder.Where(x => x.ItemOrder == j).FirstOrDefault();
                response.Trucking.Add(trucking);
            }

   

            if (result.AdditionalInfo != null)
            {
                response.ReviewerComments = result.AdditionalInfo.Where(x => x.Name.Contains("Reviewer")).SelectMany(x => x.Data.Select(y => y.Value)).FirstOrDefault();
            }
        }

    }
}
