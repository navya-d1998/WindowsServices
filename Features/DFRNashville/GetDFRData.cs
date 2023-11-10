namespace Crewlink.WindowsServices.Features
{

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
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;
using Crewlink.WindowsServices.Features;



    public class GetNashvilleDFRData
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

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();



            public CrewlinkServices.Features.DailyActivity.DFRStandard.GetAdditionalInfo.Response DFRAdditionalInfo { get; set; }



            public List<NashvilleDFNData> NashvilleData { get; set; }

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

            public class LaborActivityTotal
            {
                public string EmployeeNumber { get; set; }

                public string EmployeeName { get; set; }

                public long ForemanCrewId { get; set; }

                public decimal StandardHours { get; set; }

                public decimal OvertimeHours { get; set; }

                public decimal DoubleTimeHours { get; set; }

                public string PayLevelDescription { get; set; }

                public List<LaborDetailsByPayItem> HoursByPayItem { get; set; }

                public int PayItemCount { get; set; }
            }
            public class LaborDetailsByPayItem
            {
                public string PayItem { get; set; }
                public string Town { get; set; }
                public decimal StandardHours { get; set; }
                public decimal OvertimeHours { get; set; }
                public decimal DoubleTimeHours { get; set; }
                public decimal TotalHours { get; set; }
                public string StartStation { get; set; }
                public string EndStation { get; set; }
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
        public  ResponseData ExecuteRequest(string req)
        {
            try
            {

                var _crypto = new Crypto();
                var _cache = new HttpCacheStore();

                //Change back to Base64 and remove bearer section.
                string converted = req.Replace('-', '+');
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
                        // request.ShowHoursByPayItem = bool.Parse(SplitKey[12]);
                        request.ShowDailyFieldNotes = bool.Parse(SplitKey[13]);
                        request.ShowRentalEquipments = bool.Parse(SplitKey[14]);
                        request.ShowStations = bool.Parse(SplitKey[15]);
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

                string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\Nashville\\");

                response.LogoImagePath = Path.Combine(BaseURL, "..\\Images\\ifs-logo-bw.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "..\\Images\\ifs-logo-bw.png");

                var tempVale = "";

                //int CurrentDFRId = await _dfrDataRepository.GetDfrId("STANDARD");

                //await GetAdditionalInfo(request.ActivityId, response, CurrentDFRId);

                var dfrTemplate = ReturnFileHTML(BaseURL, "DFR_Standard.cshtml", response);

                var onlyResurfacing = false;

                using (var _context = new ApplicationContext())
                {

                    if (!request.ResurfacingId.Equals(0))
                    {
                        onlyResurfacing = _context
                            .Get<Resurfacing>()
                            .Any(x => x.Id == request.ResurfacingId && x.JobId != null);
                    }
                }

                if (request.ShowImageAttachments)
                {
                    using (var _context = new ApplicationContext())
                    {
                        if (request.ResurfacingId.Equals(0))
                        {
                            request.ResurfacingId = _context
                                .Get<Resurfacing>()
                                .Where(x => x.ActivityId == request.ActivityId)
                                .Select(x => x.Id)
                                .FirstOrDefault();
                        }
                    }

                    SharedBaseActivityHandler.PopulateImageData(request.ActivityId, request.ResurfacingId, response);
                    response.JobImages.ImageDataInfo = response.JobImages.ImageDataInfo.OrderBy(image => image.ImageOrder).ToList();
                }

                if (request.ShowPayitem)
                {
                     GetRevenue(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Nashville_Payitem.cshtml", response);
                }

                if (request.ShowLabor)
                {
                     GetLabor(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Nashville_Labor.cshtml", response);
                }

                if (request.ShowEquipment)
                {
                     GetEquipment(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Nashville_Equipment.cshtml", response);
                }

                if (request.ShowHoursByPayItem)
                {
                    GetLabor(request.ActivityId, response);
                    tempVale += ReturnFileHTML(BaseURL, "DFR_Nashville_Labor_By_PayItem.cshtml", response);
                }
                if (request.ShowDailyFieldNotes)
                {
                    getNashvilleDFNdata(request.ActivityId, response);
                    tempVale += ReturnFileHTML(BaseURL, "DailyFieldNotes.cshtml", response);

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

                int CurrentDFRId = SharedDFRDataRepository.GetDfrId("DUKE LINE 469 PIPELINE");

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
                    //dfrTemplate += ReturnFileHTML(BaseURL, "DFR_Standard_Signature.cshtml", response);
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


        public static void BindSignature(long activityId, int dfrId, string baseURL, ResponseData response)
        {
            var signatures = SharedDFRDataRepository.GetSignature(activityId, dfrId);

            if (!signatures.Count.Equals(0))
            {
                //string PartialPath = baseURL + "Images\\Temp\\" + response.FileName;
                string PartialPath = baseURL.Replace("Nashville\\", "") + "Images\\Temp\\" + response.FileName;

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
          SharedBaseActivityHandler.PopulateRevenue(activityId, response);

            response.PayitemCount = response.RevenueItems.SelectMany(x => x.Records).Count();

            response.PayitemComments = response.JobComments.Where(x => x.CommentType == "P").Select(x => x.Comment).FirstOrDefault();
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
                        DoubleTimeHours = result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours)),
                        PayLevelDescription = result.Select(x => x.Records.Select(p => p.PayLevelDescription).FirstOrDefault()).FirstOrDefault()
                    }).ToList();

            foreach (var employee in laborGroup)
            {
                employee.HoursByPayItem =  GetLaborDetailsByPayItem(activityId, employee.ForemanCrewId);
                employee.PayItemCount = employee.HoursByPayItem.Count();
            }

            response.LaborActivity = laborGroup.OrderBy(x => x.EmployeeName).ToList();
            response.PayitemCount = laborGroup.Select(p => p.HoursByPayItem).FirstOrDefault().Count();
            response.LaborCount = laborGroup.Count();
        }

        private static List<ResponseData.LaborDetailsByPayItem> GetLaborDetailsByPayItem(long activityId, long foremanCrewId)
        {
            using (var _context = new ApplicationContext())
            {
                var hoursByPayItem =
                                (from l in _context.Get<LaborByPayitem>()
                                 join p in _context.Get<CrewlinkServices.Core.Models.PayItemMapping>() on l.PayitemId equals p.Id
                                 //join c in _context.Get<Core.Models.DukeCorridorCity>() on l.CityId equals c.Id
                                 where l.ActivityId == activityId && l.ForemanCrewId == foremanCrewId && l.IsActive == true
                                 select new ResponseData.LaborDetailsByPayItem
                                 {
                                     PayItem = p.PayItemCode,
                                     // Town = c.CityCode,
                                     StandardHours = l.StandardHours,
                                     OvertimeHours = l.OvertimeHours,
                                     DoubleTimeHours = l.DoubletimeHours,
                                     TotalHours = l.StandardHours + l.OvertimeHours + l.DoubletimeHours,
                                     StartStation = l.StartStation,
                                     EndStation = l.EndStation
                                 }).ToList();

                return hoursByPayItem;
            }
        }

        private static void GetEquipment(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
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
        public static void getNashvilleDFNdata(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {

                try
                {
                    var item =  _context.Get<NashvilleDFNData>().Where(x => x.Job_id == activityId && x.IsActive == true).ToList();
                    if (item.Count == 0)
                    {
                        return;
                    }
                    else
                    {
                        response.NashvilleData = item;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception();
                }
            }
        }
        public static void GetStations(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var stations = new List<PayItemsRentalEquipments>();
                stations =  _context.Get<PayItemsRentalEquipments>().Where(x => x.Job_id == activityId && x.IsActive == true && (x.StationFlag == true)).ToList();
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
                    // response.PayItemRentalEquipment.Add(dummyPayItemRentalEquipment);
                }
                response.Stations = dummyPayItemStations;
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
