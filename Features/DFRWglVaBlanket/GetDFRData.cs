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
    public class GetWglVaBlanketDFRData
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

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            public CrewlinkServices.Features.DailyActivity.DFRWglBundle.GetAdditionalInfo.Response DFRAdditionaInfo { get; set; }

            public IEnumerable<RevenueData> WglVaBlanketRevenue { get; set; } = new List<RevenueData>();

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
                        _cache.Insert(cacheKeyValue, request, 10);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();

                 PopulateJobInfo(request.ActivityId, response);

                SharedBaseActivityHandler.PopulateJobDetails(request.ActivityId, response);

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");

                response.LogoImagePath = Path.Combine(BaseURL, "Images\\ifs-logo-bw.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "Images\\ifs-logo-bw.png");

                response.ShowForemanComments = request.ShowForemanComments;

                var tempVale = "";

                int currentDFRId = SharedDFRDataRepository.GetDfrId("WGL VA BLANKET");

                response.UniqueFilename = request.FileName;

                GetAdditionalInfo(request.ActivityId, response, currentDFRId);

                 GetRevenue(request.ActivityId, response, request.WorkOrderNumber.Trim());

                var dfrTemplate = ReturnFileHTML(BaseURL, "DFR_VA_Blanket_Payitem.cshtml", response);

                 GetWorkOrderNumber(request.ActivityId, response, request.ShowForemanComments, request.WorkOrderNumber.Trim());

                if (request.ShowForemanComments)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_VA_Blanket_Foreman_Comments.cshtml", response);
                }

                if (request.ShowLabor)
                {
                     GetLabor(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_VA_Blanket_Labor.cshtml", response);
                }

           
                if (request.ShowApproverReviewerComments && response.ReviewerComments != null)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Standard_Additional_Info.cshtml", response);
                }

                if (response.ReviewerComments == null && request.ShowApproverReviewerComments)
                {
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Dte_ApproverReviwer_No_Comments.cshtml", response);
                }

                tempVale += ReturnFileHTML(BaseURL, "DFR_VA_Blanket_Additional_Info.cshtml", response);

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

                response.FileName = request.FileName.ToString();

                response.ProcessDateAndTime = request.ProcessDateAndTime;


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
                var result = _context.Get<RevenueActivity>()
           .Where(RevenueActivity.IsActiveFilter)
           .Where(x => x.ActivityId == activityID)
           .Select(x => new
           {
               WorkOrderNumber = x.WorkOrderNumber,
               JobPayItemId = x.Id
           }).Distinct().ToList();

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
                {
                 
                revenueItems = revenueItems.Where(x => x.WorkOrderNumber == workOrderNumber).ToList(); }

                response.WglVaBlanketRevenue = revenueItems;

                response.PurchaseOrder = response.WglVaBlanketRevenue.Select(x => x.PurchaseOrderNumber).FirstOrDefault();

                response.WorkOrder = response.WglVaBlanketRevenue.Select(x => x.WorkOrderNumber).FirstOrDefault();

                response.Location = response.WglVaBlanketRevenue.Select(x => x.Address).FirstOrDefault();

                response.PayitemCount = response.WglVaBlanketRevenue.Count();

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
                    }).Where(x => x.TotalHours > 0).ToList();

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

        private static void GetAdditionalInfo(long activityId, ResponseData response, int dfrId)
        {
            var result = GetWglVaBlanketDFRAdditionalInfo.GetData(activityId, dfrId);

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

            int[] subLength = { subcontractorsVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, subcontractorsTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, subcontractorsQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, subcontractorsWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            int[] matLength = { materialsVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, materialsTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, materialsQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, materialsWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            int[] renLength = { rentalsVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, rentalsTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, rentalsQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, rentalsWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            int[] truLength = { truckingVendor.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, truckingTicket.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, truckingQty.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, truckingWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };
            //int[] cmtLength = { foremanComments.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0, foremanWorkOrder.OrderByDescending(x => x.ItemOrder).FirstOrDefault().ItemOrder ?? 0 };

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
