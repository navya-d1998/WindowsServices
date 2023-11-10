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
using static CrewlinkServices.Features.DailyActivity.DFRDte.GetDte.QueryResponse;

namespace Crewlink.WindowsServices.Features
{
    public class GetPecoFLRDFRData
    {
        public class ResponseData : BaseActivityQueryResponse
        {
            public string SuperintendentName { get; set; }
            public string ProcessDateAndTime { get; set; }
            public string LogoImagePath { get; set; }
            public string Location { get; set; }

            public string PayitemComments { get; set; }
            public string LaborComments { get; set; }
            public string EquipmentComments { get; set; }
            public string ReviewerComments { get; set; }

            public int PayitemCount { get; set; }
            public int LaborCount { get; set; }
            public int EquipmentCount { get; set; }


            public string TabNumber { get; set; }
            public string PecoWorkOrderNumber { get; set; }

            public string workOrderList { get; set; }
            public List<string> PecoWorkOrderList { get; set; }

            public bool workOrderCountFlag { get; set; }

            public int workOrderCount { get; set; }
            public string Quad { get; set; }
            public string TrNumber { get; set; }
            public string PecoDocNameOrNumber { get; set; }
            public string CallOutTime { get; set; }
            public string ArrivalTime { get; set; }
            public string EstimatedTime { get; set; }
            public string BackOnTime { get; set; }
            public string FlrComments { get; set; }

            public string ForemanSignature { get; set; }
            public string InspectorSignature { get; set; }
            public int UserId { get; set; }
            public decimal JobPriceTotalTE { get; set; }
            public decimal JobPriceTotalUS { get; set; }
            public IEnumerable<LaborActivityTotal> LaborActivity { get; set; } = new List<LaborActivityTotal>();

            public IEnumerable<EquipmentActivityTotal> EquipmentActivity { get; set; } = new List<EquipmentActivityTotal>();

            public CrewlinkServices.Features.DailyActivity.DFRPecoFLR.GetAdditionalInfo.Response DFRAdditionalInfo { get; set; }

            public IEnumerable<RevenueData> PecoFlrRevenue { get; set; } = new List<RevenueData>();

            public IEnumerable<RevenueData> PecoFlrRevenueCrew { get; set; } = new List<RevenueData>();

            public IEnumerable<RevenueData> PecoFlrRevenueRestoration { get; set; } = new List<RevenueData>();

            public IEnumerable<RevenueData> PecoFlrRevenueEquipment { get; set; } = new List<RevenueData>();

            public List<AdditionalInfoData> PecoFlrAdditionalInfo { get; set; } = new List<AdditionalInfoData>();

            public class LaborActivityTotal
            {
                public string EmployeeNumber { get; set; }
                public string EmployeeName { get; set; }
                public string EmployeeCraft { get; set; }
                public decimal StandardHours { get; set; }
                public decimal OvertimeHours { get; set; }
                public decimal DoubleTimeHours { get; set; }
                public int? Quantity { get; set; }
                public decimal TotalHours { get; set; }
                public decimal Price { get; set; }
            }

            public class EquipmentActivityTotal
            {
                public string EquipmentName { get; set; }
                public string EquipmentCode { get; set; }
                public decimal Hours { get; set; }
                public int? Quantity { get; set; }
                public decimal TotalHours { get; set; }
                public decimal Price { get; set; }
            }

            public class AdditionalInfoData
            {
                public Guid Id { get; set; }
                public string Value { get; set; }
                public string WorkOrderNumber { get; set; }
                public string Name { get; set; }
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
                        request.ShowSignature = bool.Parse(SplitKey[11]);
                        request.ShowImageAttachments = bool.Parse(SplitKey[12]);
                        request.DFRType = SplitKey[13];
                        request.WorkOrderNumber = SplitKey[14];
                        _cache.Insert(cacheKeyValue, request, 10);
                    }
                    else throw new Exception("Key not decrypted correctly.");
                }

                var response = new ResponseData();

                PopulateJobInfo(request.ActivityId, response);
                SharedBaseActivityHandler.PopulateJobDetails(request.ActivityId, response);


                var pathValue = System.Configuration.ConfigurationManager.AppSettings["TemplatePath"];

                string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, pathValue, "Templates\\PecoFlr\\");


               // string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "..\\..\\..\\Crewlink.Services\\Features\\DailyActivity\\Templates\\PecoFlr\\");
                //string BaseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Features\\DailyActivity\\Templates\\");
                response.LogoImagePath = Path.Combine(BaseURL, "ifs-logo-bw.png");

                System.Drawing.Image imageInfo = System.Drawing.Image.FromFile(BaseURL + "ifs-logo-bw.png");

                var tempVale = "";
                int CurrentDFRId =  SharedDFRDataRepository.GetDfrId("FLR_UNIT_SHEET_STREET_LIGHTS");
                var dfrTemplate = "";

                 GetRevenue(request.ActivityId, response, request);
                 GetAdditionalInfo(request.ActivityId, response, CurrentDFRId);
                 GetLabor(request.ActivityId, response);
                 GetEquipment(request.ActivityId, response);

                var CurrentHashData = FileProcess.CalculateMD5Hash(tempVale);
                var ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);

                if (request.ShowImageAttachments)
                {
                   SharedBaseActivityHandler.PopulateImageData(request.ActivityId, request.ResurfacingId, response);
                }
                if (request.ShowSignature)
                {
                    BindSignature(request.ActivityId, CurrentDFRId, BaseURL, response);
                }
                if (request.DFRType.ToLower() == "pecoflrunitsheetstreetlights")
                {
                    if (request.WorkOrderNumber.ToLower() == "all")
                    {
                        IEnumerable<RevenueData> revenue = new List<RevenueData>();

                        revenue = response.PecoFlrRevenue;
                        var count = response.PecoWorkOrderList.Count();
                        var Count = 0;
                        foreach (var workorder in response.PecoWorkOrderList)
                        {
                            List<RevenueData> dummy = new List<RevenueData>();
                            Count++;
                            foreach (var rev in revenue)
                            {
                                if (rev.WorkOrderNumber == workorder)
                                {

                                    if (count == Count)
                                    {
                                        response.workOrderCountFlag = true;
                                    }
                                    dummy.Add(rev);
                                }

                            }
                            foreach (var info in response.PecoFlrAdditionalInfo)
                            {
                                if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == workorder)
                                {
                                    response.Quad = info.Value;
                                }
                                if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == workorder)
                                {
                                    response.TrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == workorder)
                                {
                                    response.PecoDocNameOrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.CallOutTime = info.Value;
                                }
                                if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.ArrivalTime = info.Value;
                                }
                                if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.EstimatedTime = info.Value;
                                }
                                if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.BackOnTime = info.Value;
                                }
                                if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == workorder)
                                {
                                    response.FlrComments = info.Value;
                                }
                            }
                            response.workOrderList = workorder.ToString();
                            response.PecoFlrRevenue = dummy;
                            response.JobPriceTotalTE = response.JobPriceTotalUS = response.PecoFlrRevenue.Where(r => r.WorkOrderNumber == workorder).Sum(x => x.Price);
                            response.Location = response.PecoFlrRevenue.Select(x => x.Address).FirstOrDefault();
                            dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Unit_Sheet_Philly_Street_Lights.cshtml", response);

                            // dummy.Remove(rev);
                        }
                    }
                    else
                    {
                        foreach (var info in response.PecoFlrAdditionalInfo)
                        {
                            if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.Quad = info.Value;
                            }
                            if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.TrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.PecoDocNameOrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.CallOutTime = info.Value;
                            }
                            if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.ArrivalTime = info.Value;
                            }
                            if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.EstimatedTime = info.Value;
                            }
                            if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.BackOnTime = info.Value;
                            }
                            if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.FlrComments = info.Value;
                            }
                        }
                        dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Unit_Sheet_Philly_Street_Lights.cshtml", response);
                    }

                    // dfrTemplate += ReturnFileHTML(BaseURL, "Peco_Flr_DFR_Signature.cshtml", response);
                    CurrentDFRId =  SharedDFRDataRepository.GetDfrId("FLR_UNIT_SHEET_STREET_LIGHTS");
                    ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);
                }
                else if (request.DFRType.ToLower() == "pecoflrunitsheetphillydigs")
                {
                    if (request.WorkOrderNumber.ToLower() == "all")
                    {
                        IEnumerable<RevenueData> revenue = new List<RevenueData>();

                        revenue = response.PecoFlrRevenue;
                        var count = response.PecoWorkOrderList.Count();
                        var Count = 0;
                        foreach (var workorder in response.PecoWorkOrderList)
                        {
                            List<RevenueData> dummy = new List<RevenueData>();
                            Count++;
                            foreach (var rev in revenue)
                            {
                                if (rev.WorkOrderNumber == workorder)
                                {

                                    if (count == Count)
                                    {
                                        response.workOrderCountFlag = true;
                                    }
                                    dummy.Add(rev);
                                }

                            }
                            foreach (var info in response.PecoFlrAdditionalInfo)
                            {
                                if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == workorder)
                                {
                                    response.Quad = info.Value;
                                }
                                if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == workorder)
                                {
                                    response.TrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == workorder)
                                {
                                    response.PecoDocNameOrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.CallOutTime = info.Value;
                                }
                                if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.ArrivalTime = info.Value;
                                }
                                if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.EstimatedTime = info.Value;
                                }
                                if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.BackOnTime = info.Value;
                                }
                                if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == workorder)
                                {
                                    response.FlrComments = info.Value;
                                }
                            }
                            response.workOrderList = workorder.ToString();
                            response.PecoFlrRevenue = dummy;
                            response.JobPriceTotalTE = response.JobPriceTotalUS = response.PecoFlrRevenue.Where(r => r.WorkOrderNumber == workorder).Sum(x => x.Price);
                            response.Location = response.PecoFlrRevenue.Select(x => x.Address).FirstOrDefault();
                            dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Unit_Sheet_Philly_Secd_Digs.cshtml", response);
                            // dummy.Remove(rev);
                        }
                    }
                    else
                    {
                        foreach (var info in response.PecoFlrAdditionalInfo)
                        {
                            if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.Quad = info.Value;
                            }
                            if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.TrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.PecoDocNameOrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.CallOutTime = info.Value;
                            }
                            if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.ArrivalTime = info.Value;
                            }
                            if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.EstimatedTime = info.Value;
                            }
                            if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.BackOnTime = info.Value;
                            }
                            if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.FlrComments = info.Value;
                            }
                        }
                        dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Unit_Sheet_Philly_Secd_Digs.cshtml", response);
                    }

                    //  dfrTemplate += ReturnFileHTML(BaseURL, "Peco_Flr_DFR_Signature.cshtml", // response);
                    CurrentDFRId =  SharedDFRDataRepository.GetDfrId("FLR_UNIT_SHEET_PHILLY_DIGS");
                    ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);
                }
                else if (request.DFRType.ToLower() == "pecoflrunitsheetsuburbs")
                {
                    if (request.WorkOrderNumber.ToLower() == "all")
                    {
                        IEnumerable<RevenueData> revenue = new List<RevenueData>();

                        revenue = response.PecoFlrRevenue;
                        var count = response.PecoWorkOrderList.Count();
                        var Count = 0;
                        foreach (var workorder in response.PecoWorkOrderList)
                        {
                            List<RevenueData> dummy = new List<RevenueData>();
                            Count++;
                            foreach (var rev in revenue)
                            {
                                if (rev.WorkOrderNumber == workorder)
                                {

                                    if (count == Count)
                                    {
                                        response.workOrderCountFlag = true;
                                    }
                                    dummy.Add(rev);
                                }

                            }
                            foreach (var info in response.PecoFlrAdditionalInfo)
                            {
                                if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == workorder)
                                {
                                    response.Quad = info.Value;
                                }
                                if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == workorder)
                                {
                                    response.TrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == workorder)
                                {
                                    response.PecoDocNameOrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.CallOutTime = info.Value;
                                }
                                if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.ArrivalTime = info.Value;
                                }
                                if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.EstimatedTime = info.Value;
                                }
                                if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.BackOnTime = info.Value;
                                }
                                if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == workorder)
                                {
                                    response.FlrComments = info.Value;
                                }
                            }
                            response.workOrderList = workorder.ToString();
                            response.PecoFlrRevenue = dummy;
                            response.JobPriceTotalTE = response.JobPriceTotalUS = response.PecoFlrRevenue.Where(r => r.WorkOrderNumber == workorder).Sum(x => x.Price);
                            response.Location = response.PecoFlrRevenue.Select(x => x.Address).FirstOrDefault();
                            dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Unit_Sheet_Suburbs.cshtml", response);
                            // dummy.Remove(rev);
                        }
                    }
                    else
                    {
                        foreach (var info in response.PecoFlrAdditionalInfo)
                        {
                            if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.Quad = info.Value;
                            }
                            if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.TrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.PecoDocNameOrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.CallOutTime = info.Value;
                            }
                            if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.ArrivalTime = info.Value;
                            }
                            if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.EstimatedTime = info.Value;
                            }
                            if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.BackOnTime = info.Value;
                            }
                            if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.FlrComments = info.Value;
                            }
                        }
                        dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Unit_Sheet_Suburbs.cshtml", response);
                    }

                    // dfrTemplate += ReturnFileHTML(BaseURL, "Peco_Flr_DFR_Signature.cshtml", response);
                    CurrentDFRId =  SharedDFRDataRepository.GetDfrId("FLR_UNIT_SHEET_SUBURBS");

                    ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);
                }
                else if (request.DFRType.ToLower() == "pecoflrtestreetlights")
                {
                    if (request.WorkOrderNumber.ToLower() == "all")
                    {
                        IEnumerable<RevenueData> revenueCrew = new List<RevenueData>();
                        IEnumerable<RevenueData> revenueEquipment = new List<RevenueData>();
                        IEnumerable<RevenueData> revenueRestoration = new List<RevenueData>();

                        revenueCrew = response.PecoFlrRevenueCrew;
                        revenueEquipment = response.PecoFlrRevenueEquipment;
                        revenueRestoration = response.PecoFlrRevenueRestoration;

                        foreach (var workorder in response.PecoWorkOrderList)
                        {
                            List<RevenueData> dummyCrew = new List<RevenueData>();
                            List<RevenueData> dummyEquipment = new List<RevenueData>();
                            List<RevenueData> dummyRestoration = new List<RevenueData>();
                            foreach (var rev in revenueCrew)
                            {
                                if (rev.WorkOrderNumber == workorder)
                                {
                                    dummyCrew.Add(rev);
                                }

                            }
                            foreach (var equip in revenueEquipment)
                            {
                                if (equip.WorkOrderNumber == workorder)
                                {
                                    dummyEquipment.Add(equip);
                                }
                            }
                            foreach (var restore in revenueRestoration)
                            {
                                if (restore.WorkOrderNumber == workorder)
                                {
                                    dummyRestoration.Add(restore);
                                }
                            }
                            foreach (var info in response.PecoFlrAdditionalInfo)
                            {
                                if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == workorder)
                                {
                                    response.Quad = info.Value;
                                }
                                if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == workorder)
                                {
                                    response.TrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == workorder)
                                {
                                    response.PecoDocNameOrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.CallOutTime = info.Value;
                                }
                                if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.ArrivalTime = info.Value;
                                }
                                if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.EstimatedTime = info.Value;
                                }
                                if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.BackOnTime = info.Value;
                                }
                                if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == workorder)
                                {
                                    response.FlrComments = info.Value;
                                }
                            }
                            response.workOrderList = workorder.ToString();
                            response.PecoFlrRevenueCrew = dummyCrew;
                            response.PecoFlrRevenueEquipment = dummyEquipment;
                            response.PecoFlrRevenueRestoration = dummyRestoration;
                            response.JobPriceTotalTE = response.JobPriceTotalUS = response.PecoFlrRevenue.Where(r => r.WorkOrderNumber == workorder).Sum(x => x.Price);
                            response.Location = dummyCrew.Select(x => x.Address).FirstOrDefault() ?? dummyEquipment.Select(x => x.Address).FirstOrDefault() ?? dummyRestoration.Select(x => x.Address).FirstOrDefault();
                            dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Time_and_Equipment_Philly_Street_Lights.cshtml", response);
                            // dummy.Remove(rev);
                        }
                    }
                    else
                    {
                        foreach (var info in response.PecoFlrAdditionalInfo)
                        {
                            if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.Quad = info.Value;
                            }
                            if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.TrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.PecoDocNameOrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.CallOutTime = info.Value;
                            }
                            if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.ArrivalTime = info.Value;
                            }
                            if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.EstimatedTime = info.Value;
                            }
                            if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.BackOnTime = info.Value;
                            }
                            if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.FlrComments = info.Value;
                            }
                        }
                        dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Time_and_Equipment_Philly_Street_Lights.cshtml", response);
                    }

                    // dfrTemplate += ReturnFileHTML(BaseURL, "Peco_Flr_DFR_Signature.cshtml", response);
                    CurrentDFRId =  SharedDFRDataRepository.GetDfrId("FLR_TE_STREET_LIGHTS");
                    ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);
                }
                else if (request.DFRType.ToLower() == "pecoflrtephillydigs")
                {
                    if (request.WorkOrderNumber.ToLower() == "all")
                    {
                        IEnumerable<RevenueData> revenueCrew = new List<RevenueData>();
                        IEnumerable<RevenueData> revenueEquipment = new List<RevenueData>();
                        IEnumerable<RevenueData> revenueRestoration = new List<RevenueData>();

                        revenueCrew = response.PecoFlrRevenueCrew;
                        revenueEquipment = response.PecoFlrRevenueEquipment;
                        revenueRestoration = response.PecoFlrRevenueRestoration;

                        foreach (var workorder in response.PecoWorkOrderList)
                        {
                            List<RevenueData> dummyCrew = new List<RevenueData>();
                            List<RevenueData> dummyEquipment = new List<RevenueData>();
                            List<RevenueData> dummyRestoration = new List<RevenueData>();
                            foreach (var rev in revenueCrew)
                            {
                                if (rev.WorkOrderNumber == workorder)
                                {
                                    dummyCrew.Add(rev);
                                }

                            }
                            foreach (var equip in revenueEquipment)
                            {
                                if (equip.WorkOrderNumber == workorder)
                                {
                                    dummyEquipment.Add(equip);
                                }
                            }
                            foreach (var restore in revenueRestoration)
                            {
                                if (restore.WorkOrderNumber == workorder)
                                {
                                    dummyRestoration.Add(restore);
                                }
                            }
                            foreach (var info in response.PecoFlrAdditionalInfo)
                            {
                                if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == workorder)
                                {
                                    response.Quad = info.Value;
                                }
                                if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == workorder)
                                {
                                    response.TrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == workorder)
                                {
                                    response.PecoDocNameOrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.CallOutTime = info.Value;
                                }
                                if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.ArrivalTime = info.Value;
                                }
                                if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.EstimatedTime = info.Value;
                                }
                                if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.BackOnTime = info.Value;
                                }
                                if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == workorder)
                                {
                                    response.FlrComments = info.Value;
                                }
                            }
                            response.workOrderList = workorder.ToString();
                            response.PecoFlrRevenueCrew = dummyCrew;
                            response.PecoFlrRevenueEquipment = dummyEquipment;
                            response.PecoFlrRevenueRestoration = dummyRestoration;
                            response.JobPriceTotalTE = response.JobPriceTotalUS = response.PecoFlrRevenue.Where(r => r.WorkOrderNumber == workorder).Sum(x => x.Price);
                            response.Location = dummyRestoration.Select(x => x.Address).FirstOrDefault() ?? dummyEquipment.Select(x => x.Address).FirstOrDefault() ?? dummyCrew.Select(x => x.Address).FirstOrDefault();
                            dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Time_and_Equipment_Philly_Secd_Digs.cshtml", response);
                            // dummy.Remove(rev);
                        }
                    }
                    else
                    {
                        foreach (var info in response.PecoFlrAdditionalInfo)
                        {
                            if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.Quad = info.Value;
                            }
                            if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.TrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.PecoDocNameOrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.CallOutTime = info.Value;
                            }
                            if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.ArrivalTime = info.Value;
                            }
                            if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.EstimatedTime = info.Value;
                            }
                            if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.BackOnTime = info.Value;
                            }
                            if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.FlrComments = info.Value;
                            }
                        }
                        dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Time_and_Equipment_Philly_Secd_Digs.cshtml", response);
                    }


                    CurrentDFRId =  SharedDFRDataRepository.GetDfrId("FLR_TE_PHILLY_DIGS");
                    ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);
                }
                else if (request.DFRType.ToLower() == "pecoflrtesuburbs")
                {
                    if (request.WorkOrderNumber.ToLower() == "all")
                    {
                        IEnumerable<RevenueData> revenueCrew = new List<RevenueData>();
                        IEnumerable<RevenueData> revenueEquipment = new List<RevenueData>();
                        IEnumerable<RevenueData> revenueRestoration = new List<RevenueData>();

                        revenueCrew = response.PecoFlrRevenueCrew;
                        revenueEquipment = response.PecoFlrRevenueEquipment;
                        revenueRestoration = response.PecoFlrRevenueRestoration;

                        foreach (var workorder in response.PecoWorkOrderList)
                        {
                            List<RevenueData> dummyCrew = new List<RevenueData>();
                            List<RevenueData> dummyEquipment = new List<RevenueData>();
                            List<RevenueData> dummyRestoration = new List<RevenueData>();
                            foreach (var rev in revenueCrew)
                            {
                                if (rev.WorkOrderNumber == workorder)
                                {
                                    dummyCrew.Add(rev);
                                }

                            }
                            foreach (var equip in revenueEquipment)
                            {
                                if (equip.WorkOrderNumber == workorder)
                                {
                                    dummyEquipment.Add(equip);
                                }
                            }
                            foreach (var restore in revenueRestoration)
                            {
                                if (restore.WorkOrderNumber == workorder)
                                {
                                    dummyRestoration.Add(restore);
                                }
                            }
                            foreach (var info in response.PecoFlrAdditionalInfo)
                            {
                                if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == workorder)
                                {
                                    response.Quad = info.Value;
                                }
                                if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == workorder)
                                {
                                    response.TrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == workorder)
                                {
                                    response.PecoDocNameOrNumber = info.Value;
                                }
                                if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.CallOutTime = info.Value;
                                }
                                if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.ArrivalTime = info.Value;
                                }
                                if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.EstimatedTime = info.Value;
                                }
                                if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == workorder)
                                {
                                    response.BackOnTime = info.Value;
                                }
                                if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == workorder)
                                {
                                    response.FlrComments = info.Value;
                                }
                            }
                            response.workOrderList = workorder.ToString();
                            response.PecoFlrRevenueCrew = dummyCrew;
                            response.PecoFlrRevenueEquipment = dummyEquipment;
                            response.PecoFlrRevenueRestoration = dummyRestoration;
                            response.JobPriceTotalTE = response.JobPriceTotalUS = response.PecoFlrRevenue.Where(r => r.WorkOrderNumber == workorder).Sum(x => x.Price);
                            response.Location = dummyCrew.Select(x => x.Address).FirstOrDefault() ?? dummyEquipment.Select(x => x.Address).FirstOrDefault() ?? dummyRestoration.Select(x => x.Address).FirstOrDefault();
                            dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Time_and_Equipment_Suburbs.cshtml", response);
                            // dummy.Remove(rev);
                        }
                    }
                    else
                    {
                        foreach (var info in response.PecoFlrAdditionalInfo)
                        {
                            if (info.Name.ToLower() == "quad" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.Quad = info.Value;
                            }
                            if (info.Name.ToLower() == "tr_number" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.TrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "peco_doc_name" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.PecoDocNameOrNumber = info.Value;
                            }
                            if (info.Name.ToLower() == "call_out_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.CallOutTime = info.Value;
                            }
                            if (info.Name.ToLower() == "arrival_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.ArrivalTime = info.Value;
                            }
                            if (info.Name.ToLower() == "estimated_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.EstimatedTime = info.Value;
                            }
                            if (info.Name.ToLower() == "back_on_time" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.BackOnTime = info.Value;
                            }
                            if (info.Name.ToLower() == "flr_comments" && info.WorkOrderNumber == request.WorkOrderNumber)
                            {
                                response.FlrComments = info.Value;
                            }
                        }
                        dfrTemplate += ReturnFileHTML(BaseURL, "FLR_Time_and_Equipment_Suburbs.cshtml", response);
                    }

                    // FLR_Time_and_Equipment_Suburbs.cshtml

                    CurrentDFRId =  SharedDFRDataRepository.GetDfrId("FLR_TE_SUBURBS");
                    ArchivedHashData = SharedDFRDataRepository.GetDFRData(request.ActivityId, CurrentDFRId);
                }

                if (string.IsNullOrEmpty(ArchivedHashData))
                {
                    SharedDFRDataRepository.SaveDFRData(request.ActivityId, CurrentDFRId, CurrentHashData, response.UserId);
                }
                else if (!CurrentHashData.Equals(ArchivedHashData))
                {
                     SharedDFRDataRepository.InvalidateSignature(request.ActivityId, CurrentDFRId);

                    SharedDFRDataRepository.UpdateDFRData(request.ActivityId, CurrentDFRId, CurrentHashData, response.UserId);

                    request.ShowSignature = false;
                }

           
                response.FileName = request.FileName.ToString();

                response.ProcessDateAndTime = request.ProcessDateAndTime;

                dfrTemplate += tempVale;

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

        public static void BindSignature(long activityId, int dfrId, string baseURL, ResponseData response)
        {
            var signatures = SharedDFRDataRepository.GetSignature(activityId, dfrId);
            //baseURL = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Features\\DailyActivity\\Templates\\");
            if (!signatures.Count.Equals(0))
            {
                //string PartialPath = baseURL + "Images\\Temp\\" + response.FileName;
                string PartialPath = baseURL.Replace("PecoFlr\\", "") + "Images\\Temp\\" + response.FileName;

                foreach (var signature in signatures)
                {
                    if (signature.UserType.Equals(1))
                    {
                        try
                        {
                            response.InspectorSignature = PartialPath + "_1.png";
                            FileProcess.SaveBLOBAsImage(response.InspectorSignature, signature.ESignature);
                        }
                        catch (Exception e)
                        {
                            throw new Exception();
                        }

                    }
                    else if (signature.UserType.Equals(0))
                    {
                        response.ForemanSignature = PartialPath + "_0.png";
                        FileProcess.SaveBLOBAsImage(response.ForemanSignature, signature.ESignature);
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

        private static void GetRevenue(long activityId, ResponseData response, GetDFRToken.Request request)
        {
            using (var _context = new ApplicationContext())
            {
                var workOrderNumber = request.WorkOrderNumber.Trim();

               SharedBaseActivityHandler.PopulateRevenue(activityId, response);

        
                var revenueItems = 
                            (from a in _context.Get<RevenueActivity>()
                             join j in _context.Get<Activity>() on a.ActivityId equals j.Id
                             join w in _context.Get<Job>() on j.JobId equals w.Id
                             join c in _context.Get<CrewlinkServices.Core.Models.Customer>() on a.CustomerId equals c.Id
                             join p in _context.Get<PayItemMapping>() on a.PayItemId equals p.Id
                             join u in _context.Get<UnitPriceItemMC>() on new { A = c.Code, B = p.PayItemCode, C = w.JobNumber } equals new { A = u.CustomerCode, B = u.BillItemcode, C = u.JobNumber }
                             where a.ActivityId == activityId
                             select new RevenueData
                             {
                                 PayItemCode = a.PayItem.PayItemCode,
                                 PayItemDescription = a.PayItem.PayItemDescription,
                                 UnitOfMeasure = a.PayItem.UnitOfMeasure,
                                 Address = a.Address + ", " + a.City.CityCode + ", " + a.City.StateCode,
                                 WorkOrderNumber = a.WorkOrderNumber,
                                 PurchaseOrderNumber = a.PurchaseOrderNumber,
                                 Quantity = a.Quantity,
                                 Price = u.UnitPrice * a.Quantity
                             }).Distinct().OrderBy(x => x.Address).ThenBy(x => x.PayItemCode).ToList();

                response.PecoWorkOrderList = revenueItems.Select(x => x.WorkOrderNumber).Distinct().ToList();
                if (!workOrderNumber.ToLower().Equals("all"))
                { revenueItems = revenueItems.Where(x => x.WorkOrderNumber == workOrderNumber).ToList(); }

                if (workOrderNumber.ToLower().Equals("all"))
                {
                    string temp = "";
                    response.PecoWorkOrderList = revenueItems.Select(x => x.WorkOrderNumber).Distinct().ToList();

                    int count = 1;
                    foreach (var item in response.PecoWorkOrderList)
                    {
                        if (count == response.PecoWorkOrderList.Count)
                        {
                            temp += item;
                        }
                        else
                        {
                            temp += item + ", ";
                        }

                        count++;
                    }
                    response.workOrderCount = response.PecoWorkOrderList.Count;
                    response.workOrderList = temp;
                }
                response.PecoFlrRevenue = revenueItems;

                response.PecoFlrRevenueCrew = (from r in revenueItems
                                               join c in _context.Get<FlrPayItemsCategory>() on r.PayItemCode equals c.PayItem into crew
                                               from payitem in crew.DefaultIfEmpty()
                                               where payitem == null ? true :
                                                     payitem.Category == "CREW HOURS" || (payitem.Category == null)
                                               select new RevenueData
                                               {
                                                   PayItemCode = r.PayItemCode,
                                                   PayItemDescription = r.PayItemDescription,
                                                   UnitOfMeasure = r.UnitOfMeasure,
                                                   Address = r.Address,
                                                   WorkOrderNumber = r.WorkOrderNumber,
                                                   PurchaseOrderNumber = r.PurchaseOrderNumber,
                                                   Quantity = r.Quantity,
                                                   Price = r.Price
                                               }).Distinct().ToList();

                response.PecoFlrRevenueRestoration = (from r in revenueItems
                                                      join c in _context.Get<FlrPayItemsCategory>() on r.PayItemCode equals c.PayItem
                                                      where c.Category == "RESTORATION"
                                                      select new RevenueData
                                                      {
                                                          PayItemCode = r.PayItemCode,
                                                          PayItemDescription = r.PayItemDescription,
                                                          UnitOfMeasure = r.UnitOfMeasure,
                                                          Address = r.Address,
                                                          WorkOrderNumber = r.WorkOrderNumber,
                                                          PurchaseOrderNumber = r.PurchaseOrderNumber,
                                                          Quantity = r.Quantity,
                                                          Price = r.Price
                                                      }).Distinct().ToList();

                response.PecoFlrRevenueEquipment = (from r in revenueItems
                                                    join c in _context.Get<FlrPayItemsCategory>() on r.PayItemCode equals c.PayItem
                                                    where c.Category == "EQUIPMENT" && c.Description == r.PayItemDescription
                                                    select new RevenueData
                                                    {
                                                        PayItemCode = r.PayItemCode,
                                                        PayItemDescription = r.PayItemDescription,
                                                        UnitOfMeasure = r.UnitOfMeasure,
                                                        Address = r.Address,
                                                        WorkOrderNumber = r.WorkOrderNumber,
                                                        PurchaseOrderNumber = r.PurchaseOrderNumber,
                                                        Quantity = r.Quantity,
                                                        Price = r.Price
                                                    }).Distinct().ToList();

                response.Location = revenueItems.Select(x => x.Address).FirstOrDefault();
                response.PayitemCount = response.RevenueItems.SelectMany(x => x.Records).Count();
                response.PecoWorkOrderNumber = response.PecoFlrRevenue.Select(x => x.WorkOrderNumber).FirstOrDefault();
                response.PayitemComments = response.JobComments.Where(x => x.CommentType == "P").Select(x => x.Comment).FirstOrDefault();
                response.JobPriceTotalTE = response.JobPriceTotalUS = response.PecoFlrRevenue.Sum(x => x.Price);
            }
        }

        private static void GetLabor(long activityId, ResponseData response)
        {
            using (var _context = new ApplicationContext())
            {
               SharedBaseActivityHandler.PopulateLabor(activityId, response);

                response.LaborComments = response.JobComments.Where(x => x.CommentType == "L").Select(x => x.Comment).FirstOrDefault();

                var laborGroup = response.LaborRecords
                        .GroupBy(x => new
                        {
                            x.Title
                        })
                        .Select(result => new ResponseData.LaborActivityTotal
                        {
                            EmployeeCraft = result.Key.Title,
                            StandardHours = result.Sum(x => x.Records.Sum(y => y.StandardHours)),
                            OvertimeHours = result.Sum(x => x.Records.Sum(y => y.OvertimeHours)),
                            DoubleTimeHours = result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours)),
                            TotalHours = (result.Sum(x => x.Records.Sum(y => y.StandardHours)) + result.Sum(x => x.Records.Sum(y => y.OvertimeHours)) + result.Sum(x => x.Records.Sum(y => y.DoubleTimeHours))),
                            Quantity = result.Count()
                        }).ToList();


                response.LaborCount = response.LaborRecords.SelectMany(x => x.Records).Count();
                response.LaborActivity = laborGroup;
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
                            x.EquipmentName
                        })
                        .Select(result => new ResponseData.EquipmentActivityTotal
                        {
                            EquipmentName = result.Key.EquipmentName,
                            Hours = result.Sum(x => x.Entries.Sum(y => y.Hours)),
                            TotalHours = result.Sum(x => x.Entries.Sum(y => y.Hours)),
                            Price = result.Sum(x => x.Entries.Sum(y => y.Hours)) * result.Select(x => x.Entries.Select(y => y.RatePerHour).FirstOrDefault()).FirstOrDefault(),
                            Quantity = result.Count()
                        }).ToList();

                response.EquipmentActivity = equipmentGroup;

                response.EquipmentCount = equipmentGroup.Count();
            }
        }

        public static void GetAdditionalInfo(long activityId, ResponseData response, int dfrId)
        {
            response.DFRAdditionalInfo = GetPecoFLRDFRAdditionalInfo.GetData(activityId, dfrId);
            foreach (var data in response.DFRAdditionalInfo.AdditionalInfo.FLRAddiotionalInfo)
            {
                response.PecoFlrAdditionalInfo.Add(new ResponseData.AdditionalInfoData
                {
                    Id = data.Data[0].Id,
                    Value = data.Data[0].Value,
                    WorkOrderNumber = data.Data[0].WorkOrderNumber,
                    Name = data.Name
                });
            }
     }

    }
}
