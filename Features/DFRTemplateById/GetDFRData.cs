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
    public class GetTemplateByIdData
    {
        public class ResponseData : BaseActivityQueryResponse
        {
            public string SuperintendentName { get; set; }
            public string ProcessDateAndTime { get; set; }
            public string LogoImagePath { get; set; }

            public string PayitemComments { get; set; }
            public string LaborComments { get; set; }
            public string EquipmentComments { get; set; }

            public int PayitemCount { get; set; }
            public int LaborCount { get; set; }
            public int EquipmentCount { get; set; }

            public string ForemanSignature { get; set; }
            public string InspectorSignature { get; set; }
            public DateTime? ForemanSignatureDate { get; set; }
            public DateTime? InspectorSignatureDate { get; set; }

            public int UserId { get; set; }

            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            public class LaborActivityTotal
            {
                public string EmployeeNumber { get; set; }

                public string EmployeeName { get; set; }

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

        public ResponseData ExecuteRequest(string tempToken)
        {
            try
            {
                var _crypto = new Crypto();
                var _cache = new HttpCacheStore();
                //Change back to Base64 and remove bearer section.
                string converted = tempToken.Replace('-', '+');
                converted = converted.Replace('_', '/');
                converted = converted.Replace("bearer ", "");

                string cacheKeyValue = _crypto.Decryption(converted);

                var request = _cache.GetItem<GetDFRToken.Request>(cacheKeyValue);

                _cache.Clear(cacheKeyValue);

                //If not in cache (should never happen unless testing manually), try to get info from tempkey.
                if (request == null)
                {
                    if (cacheKeyValue != null &&
                        cacheKeyValue != string.Empty &&
                        cacheKeyValue.Split('_').Count() == 11)
                    {
                        string[] SplitKey = cacheKeyValue.Split('_');
                        request = new GetDFRToken.Request();
                        request.FileName = SplitKey[0] + "_" + SplitKey[1] + "_" + SplitKey[2] + "_" + SplitKey[3];
                        request.ActivityId = long.Parse(SplitKey[4]);
                        request.ProcessDateAndTime = SplitKey[5] + " " + SplitKey[6];
                        request.ShowPayitem = bool.Parse(SplitKey[7]);
                        request.ShowLabor = bool.Parse(SplitKey[8]);
                        request.ShowEquipment = bool.Parse(SplitKey[9]);
                        request.ShowSignature = bool.Parse(SplitKey[10]);
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

                response.LogoImagePath = Path.Combine(BaseURL, "Images\\logo2.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "Images\\logo2.png");

                response.FileName = request.FileName.ToString();

                response.ProcessDateAndTime = request.ProcessDateAndTime;

                var tempVale = "";

                var dfrTemplate = ReturnFileHTML(BaseURL, "DFR_Job.cshtml", response);

                if (request.ShowPayitem)
                {
                     GetRevenue(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Payitem.cshtml", response);
                }

                if (request.ShowLabor)
                {
                     GetLabor(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Labor.cshtml", response);
                }

                if (request.ShowEquipment)
                {
                     GetEquipment(request.ActivityId, response);

                    tempVale += ReturnFileHTML(BaseURL, "DFR_Equipment.cshtml", response);
                }

                dfrTemplate += tempVale;

                var CurrentHashData = FileProcess.CalculateMD5Hash(tempVale);

                int CurrentDFRId =  SharedDFRDataRepository.GetDfrId("UNIVERSAL");

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

                if (request.ShowSignature)
                {
                    BindSignature(request.ActivityId, CurrentDFRId, BaseURL, dfrTemplate, response);
                }

                dfrTemplate += ReturnFileHTML(BaseURL, "DFR_Signature.cshtml", response);

                response.FileContent = dfrTemplate;

                return response;
            }
            catch (Exception e)
            {
                string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                throw new Exception(fullexceptiondetails);
            }
        }
        public void BindSignature(long activityId, int dfrId, string baseURL, string templateContent, ResponseData response)
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

                        response.ForemanSignatureDate = signature.Modified ?? signature.Created;
                    }
                    else if (signature.UserType.Equals(1))
                    {
                        response.InspectorSignature = PartialPath + "_1.png";
                        FileProcess.SaveBLOBAsImage(response.InspectorSignature, signature.ESignature);

                        response.InspectorSignatureDate = signature.Modified ?? signature.Created;
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
                        x.EmployeeName
                    })
                    .Select(result => new ResponseData.LaborActivityTotal
                    {
                        EmployeeNumber = result.Key.EmployeeNumber,
                        EmployeeName = result.Key.EmployeeName,
                        StandardHours = result.Sum(x => x.Records.Sum(y => y.StandardHours)),
                        OvertimeHours = result.Sum(x => x.Records.Sum(y => y.OvertimeHours)),
                        DoubleTimeHours = result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours))
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

    }
}
