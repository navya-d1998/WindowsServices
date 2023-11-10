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
using static CrewlinkServices.Features.DailyActivity.DFRSpireSewerCamera.GetAdditionalInfo.Response.MasterData;

namespace Crewlink.WindowsServices.Features
{
    public class GetSpireSewerCameraDFRData
    {
        public class ResponseData : BaseActivityQueryResponse
        {
            public ResponseData()
            {
                ResponseType = CrewlinkServices.Core.Request.Response.ResponseType.File;
            }

            public int UserId { get; set; }

            public string Foreman { get; set; }

            public string JobNumber { get; set; }

            public DateTime ActivityDate { get; set; }

            public string ContractNumber { get; set; }

            public string City { get; set; }

            //public string RA { get; set; }

            public string Phase { get; set; }

            public string Inspector { get; set; }

            //public string Maintenance { get; set; }

            public string Comments { get; set; }

            public string reviewerComments { get; set; }

            public string County { get; set; }

            public string TruckId { get; set; }

            public string State { get; set; }

            public string PayitemComment { get; set; }

            public string LogoImagePath { get; set; }

            public string ProcessDateAndTime { get; set; }

            public string WorkOrder { get; set; }

            public string JobType { get; set; } = "other";

            public string TickImagePath { get; set; }

            public string ForemanSignature { get; set; }

            public string InspectorSignature { get; set; }

            public decimal? MainLocationsTotal { get; set; }

            public decimal? LateralLocationsTotal { get; set; }

            public decimal? TwoManCrewTotal { get; set; }

            public decimal? ThreeManCrewTotal { get; set; }

            public decimal? TwoManSewerCameraTotal { get; set; }

            public decimal? SewerCameraTotal { get; set; }
            //public IEnumerable<LaborRecord> LaborRecords { get; set; }

            //public IEnumerable<EquipmentRecord> EquipmentRecords { get; set; }

            public IEnumerable<EquipmentPayitemRecord> EquipmentPayitemRecords { get; set; }

            public IEnumerable<LaborPayitemRecord> LaborPayitemRecords { get; set; }

            public IEnumerable<PayitemRecord> PayitemRecords { get; set; }

            public List<LocationDetails> MainLocations { get; set; }

            public List<LocationDetails> LateralLocations { get; set; }

            public class EquipmentPayitemRecord
            {
                public string InputValue { get; set; }

                public string EquipmentName { get; set; }

                public string Qty { get; set; }
            }

            public class LaborPayitemRecord
            {
                public string InputValue { get; set; }

                public string LaborType { get; set; }

                public string Regular { get; set; }

                public string Overtime { get; set; }
            }

            public class PayitemRecord
            {
                public string PatItem { get; set; }

                public string PayItemDescription { get; set; }

                public string Tier1Description { get; set; }

                public string WorkOrder { get; set; }

                public DateTime CreatedOn { get; set; }

                public decimal Hours { get; set; }
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
                        request.WorkOrderNumber = SplitKey[12];
                        _cache.Insert(cacheKeyValue, request, 10);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();

                response.WorkOrder = request.WorkOrderNumber;

                response.reviewerComments =  GetReviewerComments(request.ActivityId);

                 GetJobRecord(request.ActivityId, response);

                 GetJobPayitem(request.ActivityId, response);

                 SetLaborHourSummary(request.ActivityId, response);

                 SetEquipmentHourSummary(request.ActivityId, response);

                 GetAdditionalInfo(request.ActivityId, response);

                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string baseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\");

               // string baseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "..\\..\\..\\Crewlink.Services\\Features\\DailyActivity\\Templates\\");

                string templateURL = baseURL + "DFR_Spire_Sewer_Camera_Work.cshtml";

                int CurrentDfrId =  SharedDFRDataRepository.GetDfrId("SPIRE SEWER CAMERA");

                response.TickImagePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\Images\\tick.png");

                response.LogoImagePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\Images\\spire-logo.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(baseURL + "Images\\tick.png");


                var tempWorkOrder = response.WorkOrder;

                var tempCity = response.City;

                var tempState = response.State;

                response.WorkOrder = "";

                response.City = "";

                response.State = "";

                var dfrTemplateTemp = Razor.Parse(File.ReadAllText(templateURL), response);

                var CurrentHashData = FileProcess.CalculateMD5Hash(dfrTemplateTemp);

                var ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDfrId);

                if (string.IsNullOrEmpty(ArchivedHashData))
                {
                     SharedDFRDataRepository.SaveDFRData(request.ActivityId, CurrentDfrId, CurrentHashData, response.UserId);
                }
                else if (!CurrentHashData.Equals(ArchivedHashData))
                {
                     SharedDFRDataRepository.InvalidateSignature(request.ActivityId, CurrentDfrId);

                     SharedDFRDataRepository.UpdateDFRData(request.ActivityId, CurrentDfrId, CurrentHashData, response.UserId);

                    request.ShowSignature = false;
                }

                response.WorkOrder = tempWorkOrder;

                if (request.ShowSignature)
                {
                    BindSignature(request.ActivityId, CurrentDfrId, baseURL, response);
                }

                response.FileName = request.FileName.ToString();

                response.City = tempCity;

                response.State = tempState;

                 GetLaborPayitem(request.ActivityId, response);

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
        public static string GetReviewerComments(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                Activity item =  _context.Get<Activity>().Where(x => x.Id == activityId).FirstOrDefault();

                var comments = item.SuperitendentComment.ToString();

                return comments;
            }
        }
        public static void GetJobRecord(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var jobdetails = (from activity in _context.Get<Activity>()
                                  join jobmaster in _context.Get<CrewlinkServices.Core.Models.Job>()
                                  on activity.JobId equals jobmaster.Id
                                  where activity.Id == activityId
                                  select (new ResponseData
                                  {
                                      ActivityDate = activity.ActivityDate,
                                      JobNumber = activity.Job.JobNumber,
                                      Foreman = activity.Foreman.Employee.EmployeeName,
                                      ContractNumber = activity.Job.ContractNumber,
                                      City = activity.RevenueActivity.Where(c => c.WorkOrderNumber == response.WorkOrder).Select(c => c.City.CityCode).FirstOrDefault(),
                                      State = activity.RevenueActivity.Where(c => c.WorkOrderNumber == response.WorkOrder).Select(s => s.City.StateCode).FirstOrDefault(),
                                      UserId = activity.ForemanUserId,
                                      PayitemComment = activity.Comments.Where(x => x.Type == "P").Select(x => x.Comment).FirstOrDefault()
                                  })).FirstOrDefault();

                response.ActivityDate = jobdetails.ActivityDate;
                response.JobNumber = jobdetails.JobNumber;
                response.Foreman = jobdetails.Foreman;
                response.ContractNumber = jobdetails.ContractNumber;
                response.City = jobdetails.City;
                response.State = jobdetails.State;
                response.UserId = jobdetails.UserId;
                response.PayitemComment = jobdetails.PayitemComment;
            }
        }

        public static void GetAdditionalInfo(long activityId, ResponseData response)
        {
            int CurrentDFRId =  SharedDFRDataRepository.GetDfrId("SPIRE SEWER CAMERA");

            var result = GetSpireSewerCameraDFRAdditionalInfo.GetData(activityId, CurrentDFRId);

            response.County = result.AdditionalInfo.County.Value;

            response.Comments = result.AdditionalInfo.Comments.Value;

            response.Phase = result.AdditionalInfo.PhaseNumber.Value;

            response.Inspector = result.AdditionalInfo.Inspector.Value;

            response.JobType = result.AdditionalInfo.ActivityType.Value;

            response.TruckId = result.AdditionalInfo.TruckId.Value;

            response.MainLocations = result.AdditionalInfo.MainLocations;

            response.LateralLocations = result.AdditionalInfo.LateralLocations;

            response.MainLocationsTotal = 0;

            response.LateralLocationsTotal = 0;

            foreach (var mainLocation in response.MainLocations)
            {
                if (!string.IsNullOrEmpty(mainLocation.Feet.Value))
                {
                    response.MainLocationsTotal += Convert.ToDecimal(mainLocation.Feet.Value);
                }
            }

            foreach (var lateralLocation in response.LateralLocations)
            {
                if (!string.IsNullOrEmpty(lateralLocation.Feet.Value))
                {
                    response.LateralLocationsTotal += Convert.ToDecimal(lateralLocation.Feet.Value);
                }
            }

            if (!string.IsNullOrEmpty(response.JobType))
            {
                var businessType_ = new BusinessTypeConfiguration();
                int jobType;
                int.TryParse(response.JobType, out jobType);
                response.JobType = businessType_
                    .Where(x => x.Type == "SPIRE_SEWER_CAMERA")
                    .Where(x => x.Id == jobType)
                    .Select(x => x.Name)
                    .FirstOrDefault();
            }
        }

        public static void GetJobPayitem(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var ActivityPayitemInfo = (from payitem in _context.Get<RevenueActivity>().Where(RevenueActivity.IsActiveFilter)
                                                join payitemmaster in _context.Get<PayItemMapping>() on payitem.PayItemId equals payitemmaster.Id
                                                where payitem.ActivityId == activityId
                                                select (new ResponseData.PayitemRecord
                                                {
                                                    PatItem = payitemmaster.PayItemCode,
                                                    PayItemDescription = payitemmaster.PayItemDescription,
                                                    Tier1Description = payitemmaster.TierOneDescription,
                                                    WorkOrder = payitem.WorkOrderNumber,
                                                    CreatedOn = payitem.Created
                                                })).ToList();

            }
        }

        public static void GetLaborPayitem(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
                var LaborPayitem = _context
                             .Get<RevenueActivity>()
                             .Where(RevenueActivity.IsActiveFilter)
                             .Where(x => x.ActivityId == activityId)
                             .Where(x => x.PayItem.PayItemCode == "2MC"
                                      || x.PayItem.PayItemCode == "3MC"
                                      || x.PayItem.PayItemCode == "SCAM"
                                      || x.PayItem.PayItemCode == "0010P6"
                                      || x.PayItem.PayItemCode == "020P8"
                                      || x.PayItem.PayItemCode == "017P8L"
                                      || x.PayItem.PayItemCode == "021P7L"
                                      || x.PayItem.PayItemCode == "019P7")
                             .Select(x => new
                             {
                                 Payitem = x.PayItem.PayItemCode,
                                 Qty = x.Quantity
                             }).ToList();

                if (response.WorkOrder != "All")
                {
                    LaborPayitem =  _context
                                     .Get<RevenueActivity>()
                                     .Where(RevenueActivity.IsActiveFilter)
                                     .Where(x => x.ActivityId == activityId)
                                     .Where(x => x.WorkOrderNumber == response.WorkOrder)
                                     .Where(x => x.PayItem.PayItemCode == "2MC"
                                              || x.PayItem.PayItemCode == "3MC"
                                              || x.PayItem.PayItemCode == "SCAM"
                                              || x.PayItem.PayItemCode == "0010P6"
                                              || x.PayItem.PayItemCode == "020P8"
                                              || x.PayItem.PayItemCode == "017P8L"
                                              || x.PayItem.PayItemCode == "021P7L"
                                              || x.PayItem.PayItemCode == "019P7")
                                     .Select(x => new
                                     {
                                         Payitem = x.PayItem.PayItemCode,
                                         Qty = x.Quantity
                                     }).ToList();
                }

                response.TwoManCrewTotal = LaborPayitem
                                        .Where(x => x.Payitem == "2MC")
                                        .Select(x => x.Qty).Sum();

                response.ThreeManCrewTotal = LaborPayitem
                                        .Where(x => x.Payitem == "3MC")
                                        .Select(x => x.Qty).Sum();

                response.TwoManSewerCameraTotal = LaborPayitem
                                       .Where(x => x.Payitem == "SCAM"
                                              || x.Payitem == "020P8"
                                              || x.Payitem == "017P8L"
                                              || x.Payitem == "021P7L"
                                              || x.Payitem == "019P7")
                                       .Select(x => x.Qty).Sum();

                response.SewerCameraTotal = LaborPayitem
                                       .Where(x => x.Payitem == "0010P6")
                                       .Select(x => x.Qty).Sum();
            }
        }

        public static bool GetTypeOfWork(string keyword, List<ResponseData.PayitemRecord> jobPayitems)
        {
            return jobPayitems
                .Where(x => ((x.PatItem.StartsWith("A") || x.PatItem.StartsWith("F")) &&
                        (x.Tier1Description.ToLower().Equals("distribution main") ||
                        x.Tier1Description.ToLower().Equals("distribution services")) &&
                        x.PayItemDescription.ToLower().Contains(keyword))).Any();
        }

        public static void SetLaborHourSummary(long activityId, ResponseData response)
        {
            var laborPayitemResults =  SharedDFRDataRepository.GetSPIREHourlyWorkLaborAsync(activityId, response.WorkOrder);

            var result = laborPayitemResults
                        .Select(x => new ResponseData.LaborPayitemRecord
                        {
                            InputValue = response.WorkOrder == "all" || response.WorkOrder == x.WorkOrderNo ? x.InputValue : "",
                            LaborType = x.LaborType,
                            Regular = x.Regular,
                            Overtime = x.Overtime,
                        }).ToList();

            response.LaborPayitemRecords = result;
        }

        public static void SetEquipmentHourSummary(long activityId, ResponseData response)
        {
            var equipmentPayitemResults = SharedDFRDataRepository.GetSPIREHourlyWorkEqupimentAsync(activityId, response.WorkOrder);

            var result = equipmentPayitemResults
                        .Select(x => new ResponseData.EquipmentPayitemRecord
                        {
                            InputValue = response.WorkOrder == "all" || response.WorkOrder == x.WorkOrderNo ? x.InputValue : "",
                            EquipmentName = x.EquipmentName,
                            Qty = x.Qty,
                        }).ToList();

            response.EquipmentPayitemRecords = result;
        }

        public static void BindSignature(long activityId, int CurrentDfrId, string baseURL, ResponseData response)
        {
            var signatures = SharedDFRDataRepository.GetSignature(activityId, CurrentDfrId);

            if (signatures != null)
            {
                string PartialPath = baseURL + "Images\\Temp\\" + response.FileName;

                foreach (var signature in signatures)
                {
                    if (signature.UserType.Equals(0))
                    {
                        response.ForemanSignature = PartialPath + "_0_" + response.WorkOrder + ".png";
                        FileProcess.SaveBLOBAsImage(response.ForemanSignature, signature.ESignature);
                    }
                    else if (signature.UserType.Equals(1))
                    {
                        response.InspectorSignature = PartialPath + "_1_" + response.WorkOrder + ".png";
                        FileProcess.SaveBLOBAsImage(response.InspectorSignature, signature.ESignature);
                    }
                }
            }
        }
        }
    }
