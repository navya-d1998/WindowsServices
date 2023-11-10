namespace Crewlink.WindowsServices.Features
{

 using Crewlink.WindowsServices.Features;
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


    public  class GetTNEDFRData
    {

        public class ResponseData : BaseActivityQueryResponse
        {
            public string Day { get; set; }
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
            public string TickImagePath { get; set; }
            public string ForemanSignature { get; set; }
            public string InspectorSignature { get; set; }

            public int UserId { get; set; }

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            public CrewlinkServices.Features.DailyActivity.DFRStandard.GetAdditionalInfo.Response DFRAdditionalInfo { get; set; }

            public List<DrawingImages> drawingImages { get; set; }
            public List<TnEDFRData> tneDfrData { get; set; }
            // public List<TnEdata> tNeData { get; set; }

            public ActivityComment comments { get; set; }
            public string ProductionComments { get; set; }
            public List<string> prodComments { get; set; }
            public List<LaborActivity> laborData { get; set; }
            public List<string> foremanNameList { get; set; }
            public List<decimal> TotalTimeHours { get; set; }

            public List<tneLaborData> TnELaborData { get; set; }

            public class tneLaborData
            {
                public long id { get; set; }
                public string employeeName { get; set; }
                public string wbsCode { get; set; }

                public string City { get; set; }
                public decimal standardHours { get; set; }
                public decimal overTimeHours { get; set; }
                public decimal doubleTimeHours { get; set; }
            }

            public class TnEDFRData
            {
                public long payItemId { get; set; }

                public string wbsCode { get; set; }
                public string address { get; set; }
                public string description { get; set; }
                public string workOrderNumber { get; set; }

                public string startTime { get; set; }
                public string endTime { get; set; }
                public string totalTime { get; set; }
                public List<ForemanHours> foremanHours { get; set; }
                public bool? isJobCompleted { get; set; }

                public string Address { get; set; }
                public string City { get; set; }
                public string State { get; set; }
                public string comments { get; set; }
                public List<string> commentsList { get; set; }
            }
            public class ForemanHours
            {
                public string foremanName { get; set; }
                public decimal hours { get; set; }
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

            public class TotalHours
            {
                public int hours { get; set; }
                public int minutes { get; set; }
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
                var cc = cacheKeyValue.Split('_').Count();
                _cache.Clear(cacheKeyValue);

                //If not in cache (should never happen unless testing manually), try to get info from tempkey.
                if (request == null)
                {
                    if (cacheKeyValue != null &&
                        cacheKeyValue != string.Empty &&
                        cacheKeyValue.Split('_').Count() == 22)
                    {
                        string[] SplitKey = cacheKeyValue.Split('_');
                        request = new GetDFRToken.Request();
                        request.FileName = SplitKey[0] + "_" + SplitKey[1] + "_" + SplitKey[2] + "_" + SplitKey[3] + "_" + SplitKey[4];
                        request.ActivityId = long.Parse(SplitKey[5]);
                        request.ProcessDateAndTime = SplitKey[6];
                        request.ShowPayitem = bool.Parse(SplitKey[8]);
                        request.ShowLabor = bool.Parse(SplitKey[9]);
                        request.ShowEquipment = bool.Parse(SplitKey[10]);
                        request.ShowSignature = bool.Parse(SplitKey[11]);
                        request.ShowImageAttachments = bool.Parse(SplitKey[12]);
                        request.ShowCutSheets = bool.Parse(SplitKey[13]);
                        request.ShowRestoration = bool.Parse(SplitKey[14]);
                        request.ResurfacingId = long.Parse(SplitKey[15]);
                        request.Address = SplitKey[18];
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


                response.TickImagePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\Images\\tick.png");

                response.LogoImagePath = Path.Combine(BaseURL, "Images\\logo2.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "Images\\logo2.png");


                var tempVale = "";

                int CurrentDFRId = SharedDFRDataRepository.GetDfrId("TM TE DFR");

                using (var _context = new ApplicationContext())
                {

                   var comments = _context.Get<ActivityComment>().Where(x => x.ActivityId == request.ActivityId && x.Type == "P").FirstOrDefault();

                    if (comments == null || comments.Comment == null)
                    {
                        response.ProductionComments = "";
                    }
                    else
                    {
                        response.ProductionComments = comments.Comment;
                    }
                }

                 GetAdditionalInfo(request.ActivityId, response, CurrentDFRId);

                var dfrTemplate = "";

                var onlyResurfacing = false;

                var tneData = new List<TnEdata>();

                if (request.Address.ToUpper() != "ALL")
                {
                    string[] address = request.Address.Split(',');
                    var dumTnEData = new List<TnEdata>();
                    tneData =  getTandEdata(request.ActivityId);
                    dumTnEData = tneData.Where(x => x.city == address[1] && x.state == address[2]).ToList();
                    tneData = dumTnEData;
                }
                else
                {
                    tneData =  getTandEdata(request.ActivityId);
                }



                 getLaborTnE(request.ActivityId, response);

                 PopulateRevenue(request.ActivityId, response);
                var tnedfrList = new List<ResponseData.TnEDFRData>();
                foreach (var item in response.RevenueItems)
                {
                    foreach (var item1 in item.Records)
                    {
                        foreach (var tne in tneData)
                        {
                            if (item1.Address == tne.address && item1.City == tne.city && item1.State == tne.state)
                            {
                                var tnedfr = new ResponseData.TnEDFRData();
                                tnedfr.payItemId = tne.JobPayItemId;
                                tnedfr.wbsCode = item1.WbsCode;
                                tnedfr.address = item1.Address + ", " + item1.City + ", " + item1.State;
                                tnedfr.description = item1.PayItemDescription;
                                tnedfr.workOrderNumber = item1.WorkOrderNumber;
                                tnedfr.Address = item1.Address;
                                tnedfr.City = item1.City;
                                tnedfr.State = item1.State;

                                if (tne.StartTime != null)
                                {
                                    DateTime st = (DateTime)tne.StartTime;
                                    tnedfr.startTime = st.ToString("hh:mm tt");
                                }
                                else
                                {
                                    tnedfr.startTime = "-";
                                }
                                if (tne.EndTime != null)
                                {
                                    DateTime et = (DateTime)tne.EndTime;
                                    tnedfr.endTime = et.ToString("hh: mm tt");
                                }
                                else
                                {
                                    tnedfr.endTime = "-";
                                }
                                tnedfr.totalTime = tne.TotalTime;
                                tnedfr.isJobCompleted = tne.isCompleted;
                                tnedfr.comments = tne.comments;
                                tnedfr.foremanHours = null;
                                tnedfrList.Add(tnedfr);
                            }
                        }
                    }
                    response.tneDfrData = tnedfrList;
                }
                var dummyForemandata = response.TnELaborData.Select(x => x.employeeName).Distinct().ToList();
                response.foremanNameList = dummyForemandata;
                response.foremanNameList.Sort();


                foreach (var tne in response.tneDfrData)
                {
                    List<ResponseData.ForemanHours> foremanData = new List<ResponseData.ForemanHours>();
                    foreach (var item in response.TnELaborData)
                    {
                        if (tne.wbsCode == item.wbsCode && tne.City == item.City)
                        {
                            var fmd = new ResponseData.ForemanHours();
                            fmd.foremanName = item.employeeName;
                            fmd.hours = item.standardHours + item.overTimeHours + item.doubleTimeHours;
                            foremanData.Add(fmd);
                        }
                    }
                    List<ResponseData.ForemanHours> newforemanData = new List<ResponseData.ForemanHours>();
                    foreach (var item in response.foremanNameList)
                    {

                        foreach (var ite in foremanData)
                        {
                            if (item == ite.foremanName)
                            {
                                newforemanData.Add(ite);
                            }
                        }
                    }
                    tne.foremanHours = newforemanData;
                }

                var totalTime = new List<decimal>();
                foreach (var foreman in response.foremanNameList)
                {
                    decimal totalT = 0;
                    foreach (var item in response.tneDfrData)
                    {
                        foreach (var ite in item.foremanHours)
                        {
                            if (foreman == ite.foremanName)
                            {
                                totalT += ite.hours;

                            }
                        }
                    }
                    totalTime.Add(totalT);
                }
                response.TotalTimeHours = totalTime;
                var tempforemanCount = response.foremanNameList.Count();
                if (tempforemanCount < 8)
                {
                    for (int i = tempforemanCount; i <= 7; i++)
                    {
                        response.foremanNameList.Add("");
                    }
                }

                var tempx = response.tneDfrData.Count();
                if (tempx < 9)
                {
                    for (int i = tempx; i <= 9; i++)
                    {
                        var tnedfr = new ResponseData.TnEDFRData();
                        tnedfr.payItemId = 0;
                        tnedfr.wbsCode = "";
                        tnedfr.address = "";
                        tnedfr.description = "";
                        tnedfr.workOrderNumber = "";
                        tnedfr.Address = "";
                        tnedfr.City = "";
                        tnedfr.State = "";
                        tnedfr.comments = "";
                        tnedfr.startTime = null;
                        tnedfr.endTime = null;
                        tnedfr.totalTime = null;
                        tnedfr.isJobCompleted = null;
                        List<ResponseData.ForemanHours> foremanData = new List<ResponseData.ForemanHours>();
                        for (int j = 0; j < response.foremanNameList.Count(); j++)
                        {
                            var fmd = new ResponseData.ForemanHours();
                            fmd.foremanName = "";
                            fmd.hours = 0;
                            foremanData.Add(fmd);
                        }
                        tnedfr.foremanHours = foremanData;
                        response.tneDfrData.Add(tnedfr);

                    }
                }
                foreach (var item in response.tneDfrData)
                {
                    if (item.comments == null || item.comments.Length == 0 || item.comments == "")
                    {
                        List<string> arr = new List<string>();
                        arr.Add("");
                        arr.Add("");
                        item.commentsList = arr;
                        continue;
                    }
                    else if (item.comments.Length <= 50)
                    {
                        List<string> arr = new List<string>();
                        arr.Add(item.comments);
                        arr.Add("");
                        item.commentsList = arr;
                    }
                    else
                    {
                        var index = 0;
                        var dumDttr = item.comments.Replace('\n', ' ').Split(' ');
                        var arr = new List<string>();
                        for (int i = 0; i < 2; i++)
                        {
                            var str = "";
                            var cnt = 0;
                            while (str.Length < 50 && index < dumDttr.Length)
                            {
                                if (str.Length + dumDttr[index].Length > 50)
                                {
                                    break;
                                }
                                str += dumDttr[index] + ' ';
                                cnt++;
                                index++;
                            }

                            arr.Add(str);
                            if (index >= dumDttr.Length)
                            {
                                break;
                            }


                        }
                        if (arr.Count == 1)
                        {
                            arr.Add("");
                        }
                        item.commentsList = arr;
                    }


                }
                var totalCount = response.TotalTimeHours.Count();
                if (totalCount < 8)
                {
                    for (int i = totalCount; i < 8; i++)
                    {
                        response.TotalTimeHours.Add(0);
                    }
                }
                for (int i = 0; i < tempx; i++)
                {
                    ResponseData.TnEDFRData item = new ResponseData.TnEDFRData();
                    item = response.tneDfrData[i];
                    List<ResponseData.ForemanHours> foremanData = new List<ResponseData.ForemanHours>();

                    for (int j = item.foremanHours.Count(); j < response.foremanNameList.Count(); j++)
                    {
                        var fmd = new ResponseData.ForemanHours();
                        fmd.foremanName = "";
                        fmd.hours = 0;
                        item.foremanHours.Add(fmd);
                        foremanData.Add(fmd);
                    }
                }
                var count = 0;
                string dumarr = "";
                response.ProductionComments = response.ProductionComments.Replace('\n', ' ');
                string[] newArr = response.ProductionComments.Split(' ');
                List<string> outputArr = new List<string>();
                if (newArr.Length <= 3)
                {
                    outputArr.Add(response.ProductionComments);
                }
                else
                {
                    var loopCount = 0;
                    foreach (var item in newArr)
                    {
                        loopCount++;
                        if (count < 2)
                        {

                            dumarr += item + " ";
                            count++;
                            int ast = Array.IndexOf(newArr, item);
                            if (loopCount == newArr.Length)
                            {
                                outputArr.Add(dumarr);
                            }
                        }
                        else
                        {
                            dumarr += item + " ";
                            outputArr.Add(dumarr);
                            dumarr = "";
                            count = 0;
                        }

                    }
                }
                int var1 = outputArr.Count;
                int var2 = response.tneDfrData.Count * 2;
                for (int i = var1; i < var2; i++)
                {
                    outputArr.Add(" ");
                }
                response.prodComments = outputArr;
                using (var _context = new ApplicationContext())
                {

                    if (!request.ResurfacingId.Equals(0))
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
                    response.Day = response.ActivityDate.DayOfWeek.ToString();
                    dfrTemplate = ReturnFileHTML(BaseURL, "DFR_TNE.cshtml", response);
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
                string[] rt = request.ProcessDateAndTime.Split(' ');
                response.ProcessDateAndTime = rt[0];

                dfrTemplate += ReturnFileHTML(BaseURL, "DFR_TNE_SIGNATURE.cshtml", response);

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


        public static List<TnEdata> getTandEdata(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                try
                {
                    var item = _context.Get<TnEdata>().Where(x => x.Job_id == activityId && x.IsActive == true).ToList();
               
                    return item;
                }
                catch (Exception e)
                {
                    throw new Exception();
                }
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

        private static void GetRevenue(long activityId, ResponseData response)
        {
             PopulateRevenue(activityId, response);

            response.PayitemCount = response.RevenueItems.SelectMany(x => x.Records).Count();

            response.PayitemComments = response.JobComments.Where(x => x.CommentType == "P").Select(x => x.Comment).FirstOrDefault();
        }
        public static void PopulateRevenue(long activityId, BaseActivityQueryResponse response)
        {

            using (var _context = new ApplicationContext())
            {
                var revenueItems = _context
                .Get<RevenueActivity>()
                .AsNoTracking()
                .Where(RevenueActivity.IsActiveFilter)
                .Where(x => x.ActivityId == activityId)
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


                response.RevenueItems = revenueItems.OrderBy(x => x.PayItemCode);
            }
        }

        public static void getLaborTnE(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var dummyLabor = _context.Get<LaborActivity>().AsNoTracking().Where(x => x.ActivityId == activityId).Where(LaborActivity.IsActiveQueryFilter).Select(x => new
                {
                    x.Id,
                    x.CrewMember.Employee.EmployeeName,
                    x.WbsCode,
                    x.City.CityCode,
                    x.StandardHours,
                    x.OvertimeHours,
                    x.DoubletimeHours,
                }).ToList();

                List<ResponseData.tneLaborData> dmlaborList = new List<ResponseData.tneLaborData>();

                foreach (var item in dummyLabor)
                {
                    ResponseData.tneLaborData dmlabor = new ResponseData.tneLaborData();
                    dmlabor.id = item.Id;
                    dmlabor.wbsCode = item.WbsCode;
                    dmlabor.City = item.CityCode;
                    dmlabor.standardHours = item.StandardHours;
                    dmlabor.overTimeHours = item.OvertimeHours;
                    dmlabor.doubleTimeHours = item.DoubletimeHours;
                    dmlabor.employeeName = item.EmployeeName;
                    dmlaborList.Add(dmlabor);
                }

                response.TnELaborData = dmlaborList;
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





    }
}
