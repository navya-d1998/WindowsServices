using Crewlink.WindowsServices.Files.Shared;
using Crewlink.WindowsServices.Files.CutSheets;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Features.DailyActivity.Labor;
using CrewlinkServices.Features.DailyActivity.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CrewlinkServices.Features.DailyActivity.Labor.Get;
using static CrewlinkServices.Features.DailyActivity.Labor.Get.Response.ActivityLaborData;
using static CrewlinkServices.Features.DailyActivity.Labor.SpecialProjPayLevelOverrides;
using Crewlink.WindowsServices.Files.Restoration;
using CrewlinkServices.Features.DailyActivity;

namespace CrewLink.WindowsServices.Files.Shared
{
    public class SharedBaseActivityHandler
    {
        public static void PopulateImageData(long activityId, long resurfacingId, BaseActivityQueryResponse response)
        {
            response.JobImages = GetJobImageInfo(activityId,resurfacingId);
        }

        public static GetJobImageInfo.Response GetJobImageInfo(long ActivityId, long ResurfacingId)
        {
            var response = new GetJobImageInfo.Response();

            response.ImageDataInfo =  GetImageDataInfo(ActivityId, ResurfacingId);

             GetUploaderInfo(response);

            return response;
        }
        private static IList<JobImages> GetImageDataInfo(long ActivityId, long ResurfacingId)
        {
            if (ActivityId.Equals(0))
            {
                var result = SharedDFRDataRepository.GetResurfacingImages(ResurfacingId);

                return result;
            }
            else
            {
                var result = SharedDFRDataRepository.GetExistingJobImageInfo(ActivityId);

                return result;
            }
        }

        public static void GetUploaderInfo(GetJobImageInfo.Response response)
        {
            using (var _context = new ApplicationContext())
            {
                foreach (var image in response.ImageDataInfo)
                {
                    if (image.CreatedBy != null)
                    {
                        image.UploadedBy = (from employee in _context.Get<Employee>()
                                            join user in _context.Get<User>()
                                            on employee.Id equals user.EmployeeId
                                            where user.Id == image.CreatedBy
                                            select employee.EmployeeName).FirstOrDefault();
                    }
                }
            }
        }
        public static void PopulateJobDetails(long activityId, BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {
                var pipeLinePerDiem = new List<PipeLineAndFacilitiesPD>();

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
                var isPipelineNFacilitiesProject = _context
                       .Get<Contract>()
                       .AsNoTracking()
                       .Where(x => x.ContractNumber == jobDetails.ContractNumber)
                       .Select(x => x.IsPipelineNFacilitiesProject).First();

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
                        string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                        throw new Exception(fullexceptiondetails);
                    }

                }

                if (isPipelineNFacilitiesProject)
                {
                    pipeLinePerDiem = PipeLinePerDiemsByJobNumber(jobDetails.JobNumber);
                }

                var ApproverRole = (from activity in _context.Get<Activity>()
                                    join jobMaster in _context.Get<Job>() on activity.JobId equals jobMaster.Id
                                    join employee in _context.Get<Employee>() on jobMaster.SuperitendentEmployeeNumber equals employee.EmployeeNumber
                                    join user in _context.Get<User>() on employee.Id equals user.EmployeeId
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
                response.LockdownStatus = Lockdown(activityId, jobDetails.ActivityDate, jobDetails.ModifiedBy);
                response.IsSpecialProject = isSpecialProject;
                response.IsPecoRelatedOverride = isPecoRelatedOverride;
                response.IsPipelineNFacilitiesProject = isPipelineNFacilitiesProject;
                response.PipelineNFacilitiesPerDiems = pipeLinePerDiem;
                response.CompanyCode = jobDetails.CompanyCode;
                GetReviewerInfo(activityId, response);
                GetContractInfo(response);
                GetEmployeeCompanyCodeInfo(response);
            }
        }
        public static void GetReviewerInfo(long activityId, BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {

                var jobReviewer =  _context
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

        public static void GetContractInfo(BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {

                var jobContract =  _context
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

        public static void GetEmployeeCompanyCodeInfo(BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {


                var employeeCompanyCodeInfo = (
                from employee in _context.Get<Employee>()
                join user in _context.Get<User>() on employee.Id equals user.EmployeeId
                where user.Id == response.ForemanUserId
                select new { employee.CompanyCode }).FirstOrDefault();

                response.EmployeeCompanyCode = employeeCompanyCodeInfo.CompanyCode;
            }
        }

        private static bool Lockdown(long activityId, DateTime activityDate, int? approverId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                    .Get<LockDown>().Where(LockDown.IsActiveQueryFilter)
                    .Where(x => x.UserId == approverId)
                    .Where(x => (activityDate >= x.WeekStart && activityDate <= x.WeekEnd))
                    .Where(x => x.IsLockedDown == true)
                    .Any();
            }
        }
        private static List<PipeLineAndFacilitiesPD> PipeLinePerDiemsByJobNumber(string jobNumber)
            {
              using (var _context = new ApplicationContext())
              {
                var jobNumberParam = new SqlParameter("@JobNumber", SqlDbType.VarChar) { Value = jobNumber };

                List<PipeLineAndFacilitiesPD> pipelinePerDiems = new List<PipeLineAndFacilitiesPD>();

                var query = $"exec GET_PIPELINE_PERDIEMS_BY_JOB_NUMBER_SP {jobNumberParam}";

                try
                {
                    pipelinePerDiems = ((ApplicationContext)_context)
                                   .Database
                                   .SqlQuery<PipeLineAndFacilitiesPD>(query, jobNumberParam)
                                   .ToList();

                }
                catch (Exception e)
                {
                    string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                    throw new Exception(fullexceptiondetails);
                }

                return pipelinePerDiems;
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
        public static void PopulateRestorationData(long resurfacingId, BaseActivityQueryResponse response)
        {
            long[] resurfacingIds = new long[] { resurfacingId };

            var request = GetRestorationData.ExecuteRequest(resurfacingIds);

            response.RestorationActivity = request.RestorationActivity[0];

            foreach (var item in response.RestorationActivity.SurfacesRestoredData)
            {
                if (item.originalDiameter != null)
                {
                    string x = item.originalDiameter.ToString();
                    decimal y = Convert.ToDecimal(x); 
                    item.originalDiameterFeet = getFeetValue(y).ToString();
                    item.originalDiameterInches = getInchesValue(y).ToString();
                }
                else
                {
                    item.originalDiameterFeet = "0";
                    item.originalDiameterInches = "0";
                }
                if (item.restoredDiameter != null)
                {
                    string x = item.restoredDiameter.ToString();
                    decimal y = Convert.ToDecimal(x);
                    item.restoredDiameterFeet = getFeetValue(y).ToString();
                    item.restoredDiameterInches = getInchesValue(y).ToString();
                }
                else
                {
                    item.restoredDiameterFeet = "0";
                    item.restoredDiameterInches = "0";
                }

            }
        }
        public static void PopulateCutSheetsData(long resurfacingId, BaseActivityQueryResponse response)
        {
            var request = GetCutSheetsData.ExecuteRequest(resurfacingId);
            response.CutSheetsActivity = request.CutSheetsActivity;
            foreach (var item in response.CutSheetsActivity.SurfacesRemovedData)
            {
                if (item.quantityDiameter != null)
                {
                    string x = item.quantityDiameter.ToString();
                    decimal y = Convert.ToDecimal(x);
                    item.quantityDiameterFeet = getFeetValue(y).ToString();
                    item.quantityDiameterInches = getInchesValue(y).ToString();
                }

            }
            response.TemporaryFillData = response.CutSheetsActivity.TemporarySurfaceData.Where(x => x.IsTempFill == true);
            response.AdditionalMaterialData = response.CutSheetsActivity.TemporarySurfaceData.Where(x => x.IsTempFill == false);
        }

        public static decimal getFeetValue(decimal quantity)
        {
            decimal x = Math.Floor(quantity);
            return x;
        }

        public static decimal getInchesValue(decimal quantity)
        {
            decimal inches = ((quantity % 1) * 12);

            decimal x = Math.Round(inches);

            return x;
        }

        public static void PopulateEquipment(long activityId, BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {
                var equipmentActivity =
                (from f in _context.Get<FleetActivity>().Where(FleetActivity.IsActiveQueryFilter)
                 join wbs in _context.Get<WbsCode>() on f.WbsCode equals wbs.Code
                 where f.ActivityId == activityId
                 select new
                 {
                     f.Id,
                     f.ForemanFleet.Fleet.EquipmentCode,
                     f.ForemanFleet.Fleet.Description,
                     f.Preference,
                     f.ForemanFleet.Fleet.EquipmentType,
                     f.WbsCode,
                     WbsDescription = wbs.Description,
                     f.Hours,
                     f.ForemanFleet.Fleet.RatePerHour
                 }).ToList();

                var groupedResults = equipmentActivity
                    .GroupBy(x => new
                    {
                        x.EquipmentCode,
                        x.Description,
                        x.EquipmentType,
                        x.Preference
                    })
                    .Select(result => new BaseActivityQueryResponse.EquipmentUsage
                    {
                        EquipmentCode = result.Key.EquipmentCode,
                        Preference = result.Key.Preference == null ? 0 : result.Key.Preference.Value,
                        EquipmentName = result.Key.Description,
                        SpectrumTypeDescription = (_context.Get<FleetTypeCodesRefTable>()
                                                    .Where(x => x.SpectrumTypeCode == result.Key.EquipmentType)
                                                    .Select(x => x.SpectrumTypeDescription)
                                                    .FirstOrDefault()),
                        Entries = result.Select(x => new BaseActivityQueryResponse.EquipmentUsage.Entry
                        {
                            Id = x.Id,
                            WbsCode = x.WbsCode,
                            WbsDescription = x.WbsDescription,
                            Hours = x.Hours ?? 0,
                            RatePerHour = x.RatePerHour
                        })
                            .ToList()
                            .OrderBy(x => x.WbsCode)
                    }).ToList();

                AddWeeklyEquipmentTotals(activityId, groupedResults);

                PopulateDailyTotals(activityId, groupedResults);

                groupedResults = groupedResults.OrderBy(x => x.Preference).ToList();

                response.EquipmentRecords = groupedResults;
            }
        }
        private static void PopulateDailyTotals(long activityId, IEnumerable<BaseActivityQueryResponse.EquipmentUsage> equipmentHours)
        {
            var equipmentDailyTotals = SharedEquipmentDailyTotalsRepository
                .GetDailyByActivityAsync(activityId);

            foreach (var equipment in equipmentHours)
            {
                var dailyTotalsResult = equipmentDailyTotals
                    .FirstOrDefault(x => x.EquipmentCode == equipment.EquipmentCode);

                if (dailyTotalsResult == null) continue;

                equipment.TotalHoursForDay = dailyTotalsResult.Hours ?? 0m;
            }
        }
        public static void AddWeeklyEquipmentTotals(long activityId, IEnumerable<BaseActivityQueryResponse.EquipmentUsage> equipmentRecords)
        {
            var results = SharedEquipmentWeeklyTotalRepository
                .GetByActivityAsync(activityId);

            foreach (var equipmentRecord in equipmentRecords)
            {
                var result = results.FirstOrDefault(x => x.EquipmentCode == equipmentRecord.EquipmentCode);

                if (result == null) continue;

                equipmentRecord.TotalHoursForWeek = result.Hours ?? 0;
            }
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

        public static void PopulateLabor(long activityId, BaseActivityQueryResponse response)
        {
            using (var _context = new ApplicationContext())
            {
                Func<string, byte> parsePayRateCode = code =>
                {
                    byte paylevel;

                    if (!byte.TryParse(code, out paylevel))
                        paylevel = 1;

                    return paylevel;
                };

                var laborActivity = _context
                    .Get<LaborActivity>()
                    .AsNoTracking()
                    .Where(x => x.ActivityId == activityId)
                    .Where(LaborActivity.IsActiveQueryFilter)
                    .Select(x => new
                    {
                        x.Id,
                        x.CrewMember.Employee.EmployeeNumber,
                        x.CrewMember.Employee.EmployeeName,
                        x.CrewMember.Employee.Title,
                        x.Preference,
                        x.ForemanCrewId,
                        x.CrewMember.Employee.RigPayRate,
                        x.CrewMember.Employee.TruckPayRate,
                        x.CrewMember.Employee.PerdiemRate,
                        x.WbsCode,
                        x.WbsDescription,
                        x.City.CityCode,
                        x.AutoAllowance,
                        x.StandardHours,
                        x.OvertimeHours,
                        x.DoubletimeHours,
                        x.PaidTimeOff,
                        x.UnionCode,
                        x.HolidayPay,
                        x.PerDiem,
                        x.PerDiemType,
                        x.Overrides,
                        x.RigRental,
                        x.MiscCode,
                        x.TravelPay,
                        x.WaitTimeHours,
                        x.CrewMember.Employee.LicenseType,
                        x.Paylevel,
                        x.SpecialProjectsJobUnionCode,
                        x.StartAt,
                        x.EndAt,
                        x.SubtractTime,
                        x.OtType
                    }).ToList();

                response.SPUnionCodeSelected = laborActivity.Select(x => x.SpecialProjectsJobUnionCode).FirstOrDefault();

                var groupedResults = laborActivity
                    .GroupBy(x => new
                    {
                        x.EmployeeNumber,
                        x.EmployeeName,
                        x.Preference,
                        x.Title,
                        x.RigPayRate,
                        x.TruckPayRate,
                        x.PerdiemRate,
                        x.ForemanCrewId,
                        x.LicenseType
                    })
                    .Select(result => new BaseActivityQueryResponse.EmployeeLabor
                    {
                        EmployeeNumber = result.Key.EmployeeNumber,
                        EmployeeName = result.Key.EmployeeName,
                        LicenseType = result.Key.LicenseType,
                        Preference = result.Key.Preference == null ? 0 : result.Key.Preference.Value,
                        Title = result.Key.Title,
                        RigPayRate = result.Key.RigPayRate,
                        TruckPayRate = result.Key.TruckPayRate,
                        PerDiemPayRate = result.Key.PerdiemRate,
                        ForemanCrewId = result.Key.ForemanCrewId,
                        Records = result
                            .Select(x => new BaseActivityQueryResponse.EmployeeLabor.LaborData
                            {
                                Id = x.Id,
                                WbsCode = x.WbsCode,
                                WbsDescription = x.WbsDescription,
                                CityCode = x.CityCode,
                                AutoAllowance = x.AutoAllowance,
                                StandardHours = x.StandardHours,
                                OvertimeHours = x.OvertimeHours,
                                DoubleTimeHours = x.DoubletimeHours,
                                PaidTimeOff = x.PaidTimeOff,
                                UnionCode = x.UnionCode.Trim(),
                                HolidayPay = x.HolidayPay,
                                PerDiem = x.PerDiem,
                                PerDiemType = x.PerDiemType,
                                Overrides = x.Overrides,
                                RigRentals = x.RigRental,
                                MiscCode = x.MiscCode,
                                TravelPay = x.TravelPay,
                                WaitTimeHours = x.WaitTimeHours,
                                PayLevel = x.Paylevel, //parsePayRateCode(x.PayRateCode)
                                PayLevelOverride = x.Paylevel,
                                UnionCodeOverride = x.UnionCode.Trim()
                            })
                            .ToList()
                            .OrderBy(x => x.WbsCode)
                            .ThenBy(x => x.CityCode)
                    }).ToList();

                if (response.IsSpecialProject)
                {
                    foreach (var employee in groupedResults)
                    {
                        foreach (var record in employee.Records)
                        {
                            //record.UnionCodes = GetJobUnionCodes(response.JobNumber, response.ContractNumber);
                            record.UnionCodes = GetSPUnionCodes(response.CompanyCode);

                            //var defaultUnionCode = _context.Get<Employee>()
                            //        .Where(e => e.EmployeeNumber == employee.EmployeeNumber && e.EmployeeStatus == "A")
                            //        .Select(u => new JobUnionCode
                            //        {
                            //            Id = (int)u.Id,
                            //            ContractNumber = response.ContractNumber,
                            //            JobNumber = response.JobNumber,
                            //            UnionCode = u.UnionCode.Trim(),
                            //            IsActive = true
                            //        }).AsEnumerable().Select(u => new SpecialProjectsUnionCode
                            //        {
                            //            Id = u.Id,
                            //            UnionCode = u.UnionCode.Trim(),
                            //            JobNumber = u.JobNumber,
                            //            ContractNumber = u.ContractNumber,
                            //            IsActive = u.IsActive
                            //        }).FirstOrDefault();
                            var defaultUnionCode = _context.Get<Employee>()
                                    .Where(e => e.EmployeeNumber == employee.EmployeeNumber && e.EmployeeStatus == "A")
                                    .Select(u => new CrewlinkServices.Features.DailyActivity.Labor.Get.JobUnionCode
                                    {
                                        Id = (int)u.Id,
                                        ContractNumber = response.ContractNumber,
                                        JobNumber = response.JobNumber,
                                        UnionCode = u.UnionCode.Trim(),
                                        IsActive = true
                                    }).ToList().FirstOrDefault();

                            if (defaultUnionCode == null)
                            {
                                //defaultUnionCode = _context.Get<Employee>()
                                //     .Where(e => e.EmployeeNumber == employee.EmployeeNumber)
                                //     .Select(u => new JobUnionCode
                                //     {
                                //         Id = (int)u.Id,
                                //         ContractNumber = response.ContractNumber,
                                //         JobNumber = response.JobNumber,
                                //         UnionCode = u.UnionCode.Trim(),
                                //         IsActive = true
                                //     }).AsEnumerable().Select(u => new SpecialProjectsUnionCode
                                //     {
                                //         Id = u.Id,
                                //         UnionCode = u.UnionCode.Trim(),
                                //         JobNumber = u.JobNumber,
                                //         ContractNumber = u.ContractNumber,
                                //         IsActive = u.IsActive
                                //     }).FirstOrDefault();
                                defaultUnionCode = _context.Get<Employee>()
                                     .Where(e => e.EmployeeNumber == employee.EmployeeNumber)
                                     .Select(u => new JobUnionCode
                                     {
                                         Id = (int)u.Id,
                                         ContractNumber = response.ContractNumber,
                                         JobNumber = response.JobNumber,
                                         UnionCode = u.UnionCode.Trim(),
                                         IsActive = true
                                     }).ToList().FirstOrDefault();
                            }

                            if (string.IsNullOrEmpty(defaultUnionCode.UnionCode))
                            {
                                defaultUnionCode.UnionCode = "No Default Union Code";
                            }

                            if (record.UnionCode == defaultUnionCode.UnionCode)
                            {
                                //record.PayLevelList = GetPayLevelsOfDefaultEmployeeUnionCode(record.UnionCode);
                                record.PayLevelList = GetPayLevelsByEmployeeUnionCode(record.UnionCode, employee.EmployeeNumber);
                            }
                            else
                            {
                                record.PayLevelList = GetPayLevelsByEmployeeUnionCode(record.UnionCode, employee.EmployeeNumber);
                            }

                            record.UnionCodes = record.UnionCodes.Concat(new[] { defaultUnionCode });
                            record.PayLevelDescription = record.PayLevelList.Where(x => x.Level.Contains(record.PayLevel.ToString())).Select(x => x.Value).FirstOrDefault();

                            if (!record.PayLevelList?.Any() ?? false)
                            {
                                List<PayLevelCode> tempLevelList = new List<PayLevelCode>();
                                var level = new PayLevelCode();
                                level.Level = "Level" + record.PayLevel;
                                level.Value = record.PayLevelDescription ?? record.PayLevel.ToString();

                                tempLevelList.Add(level);
                                record.PayLevelList = tempLevelList;
                            }
                        }
                    }
                }

                if (response.IsPecoRelatedOverride)
                {
                    foreach (var employee in groupedResults)
                    {
                        foreach (var record in employee.Records)
                        {
                            record.PecoUnionCodes = GetPecoUnionCodes(response.CompanyCode);

                            var defaultUnionCode = _context.Get<Employee>()
                                    .Where(e => e.EmployeeNumber == employee.EmployeeNumber && e.EmployeeStatus == "A")
                                    .Select(u => new JobUnionCode
                                    {
                                        Id = (int)u.Id,
                                        ContractNumber = response.ContractNumber,
                                        JobNumber = response.JobNumber,
                                        UnionCode = u.UnionCode.Trim(),
                                        IsActive = true
                                    }).AsEnumerable().Select(u => new PecoUnionCode
                                    {
                                        Id = u.Id,
                                        UnionCode = u.UnionCode,
                                        JobNumber = u.JobNumber,
                                        ContractNumber = u.ContractNumber,
                                        IsActive = u.IsActive
                                    }).FirstOrDefault();
                            if (defaultUnionCode == null)
                            {
                                defaultUnionCode = _context.Get<Employee>()
                                    .Where(e => e.EmployeeNumber == employee.EmployeeNumber)
                                    .Select(u => new JobUnionCode
                                    {
                                        Id = (int)u.Id,
                                        ContractNumber = response.ContractNumber,
                                        JobNumber = response.JobNumber,
                                        UnionCode = u.UnionCode.Trim(),
                                        IsActive = true
                                    }).AsEnumerable().Select(u => new PecoUnionCode
                                    {
                                        Id = u.Id,
                                        UnionCode = u.UnionCode,
                                        JobNumber = u.JobNumber,
                                        ContractNumber = u.ContractNumber,
                                        IsActive = u.IsActive
                                    }).FirstOrDefault();
                            }

                            if (string.IsNullOrEmpty(defaultUnionCode.UnionCode))
                            {
                                defaultUnionCode.UnionCode = "No Default Union Code";
                            }

                            if (record.UnionCode == defaultUnionCode.UnionCode)
                            {
                                record.PayLevelList = GetPayLevelsOfDefaultEmployeeUnionCode(record.UnionCode);
                            }
                            else
                            {
                                record.PayLevelList = GetPecoUnionCodePayLevels(response.CompanyCode, record.UnionCode);
                            }

                            record.PayLevelDescription = record.PayLevelList.Where(x => x.Level.Contains(record.PayLevel.ToString())).Select(x => x.Value).FirstOrDefault();
                            record.PecoUnionCodes = record.PecoUnionCodes.Concat(new[] { defaultUnionCode });

                            if (!record.PayLevelList?.Any() ?? false)
                            {
                                List<PayLevelCode> tempLevelList = new List<PayLevelCode>();
                                var level = new PayLevelCode();
                                level.Level = "Level" + record.PayLevel;
                                level.Value = record.PayLevelDescription ?? record.PayLevel.ToString();

                                tempLevelList.Add(level);
                                record.PayLevelList = tempLevelList;
                            }
                        }
                    }
                }

                AddWeeklyLaborTotals(activityId, groupedResults);

                AddDailyLaborTotals(activityId, groupedResults);

                PopulateDailyTimings(activityId, groupedResults);

                PopulateWeeklyTimings(activityId, groupedResults);

                //   PopulateLaborTimings(activityId, groupedResults);


                groupedResults = groupedResults.OrderBy(x => x.Preference).ToList();

                response.LaborRecords = groupedResults;
            }
        }
        public static List<EmployeeLabor.TimingsRecord> getWeeklyTimings(long activityId, DateTime activityDate, string employeeNumber)
        {
            using (var _context = new ApplicationContext())
            {
                var isSpecialProject = GetSpecialProjectStatus(activityId);

                var workWeek = Helper.GetWorkWeekByDate(activityDate);

                if (isSpecialProject)
                {
                    workWeek = Helper.GetWorkWeekByDateSpecialProjects(activityDate);
                }

                var employeeNumberParameter = new SqlParameter("@employeeNumber", SqlDbType.VarChar) { Value = employeeNumber };

                var startDateParameter = new SqlParameter("@activityStartDate", SqlDbType.Date) { Value = workWeek.WeekStart };

                var endDateParameter = new SqlParameter("@activityEndDate", SqlDbType.Date) { Value = workWeek.WeekEnd };

                var query = $"exec GET_WEEKLY_TIMINGS_FOR_EMPLOYEE_SP {startDateParameter.ParameterName},{endDateParameter.ParameterName},{employeeNumberParameter}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<EmployeeLabor.TimingsRecord>(query, startDateParameter, endDateParameter, employeeNumberParameter)
                    .ToList();
            }
        }


        private static bool GetSpecialProjectStatus(long activityId)
        {
            using (var _context = new ApplicationContext())
            {

                var isSpecialProject = false;

                var contract = (from c in _context.Get<Contract>()
                                join j in _context.Get<Job>()
                                on c.ContractNumber equals j.ContractNumber
                                join a in _context.Get<Activity>()
                                on j.Id equals a.JobId
                                where a.Id == activityId
                                select c).FirstOrDefault();

                isSpecialProject = contract != null && contract.IsSpecialProject;

                return isSpecialProject;
            }
        }
        public static List<EmployeeLabor.TimingsRecord> getDailyTimings(DateTime activityDate, string employeeNumber)
        {
            using (var _context = new ApplicationContext())
            {

                var employeeNumberParameter = new SqlParameter("@employeeNumber", SqlDbType.VarChar) { Value = employeeNumber };

                var startDateParameter = new SqlParameter("@activityDate", SqlDbType.Date) { Value = activityDate };

                var query = $"exec GET_DAILY_TIMINGS_FOR_EMPLOYEE_SP {startDateParameter.ParameterName},{employeeNumberParameter}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<EmployeeLabor.TimingsRecord>(query, startDateParameter, employeeNumberParameter)
                    .ToList();
            }
        }
        private static DateTime GetActivityDate(long activityId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                .Get<Activity>()
                .Where(x => x.Id == activityId)
                .Select(x => x.ActivityDate)
                .First();
            }
        }
        public static void PopulateWeeklyTimings(long activityId, IEnumerable<BaseActivityQueryResponse.EmployeeLabor> laborRecords)
        {

            foreach (var laborRecord in laborRecords)
            {
                var activityDate = GetActivityDate(activityId);

                var result = getWeeklyTimings(activityId, activityDate, laborRecord.EmployeeNumber);
                var groupedResult = result
                                  .GroupBy(a => new { a.EmployeeNumber, a.EmployeeName, a.JobDescription, a.ActivityDate, a.JobNumber, a.JobStatus, a.Foreman })
                                  .Select(x => new
                                  {
                                      EmployeeNumber = x.Key.EmployeeNumber,
                                      EmployeeName = x.Key.EmployeeName,
                                      JobDescription = x.Key.JobDescription,
                                      ActivityDate = x.Key.ActivityDate,
                                      JobNumber = x.Key.JobNumber,
                                      JobStatus = x.Key.JobStatus,
                                      Foreman = x.Key.Foreman,
                                      records = x.Select(y => new {
                                          y.StartAt,
                                          y.EndAt,
                                          y.SubtractTime,
                                          y.OtType
                                      }).ToList()
                                  }).ToList();

                var finalList = new List<EmployeeLabor.TimingsRecord>();

                foreach (var x in groupedResult)
                {
                    TimeSpan? start = null;

                    TimeSpan? end = null;

                    TimeSpan? subtractTime = new TimeSpan(0, 0, 0);

                    string ottype = "";

                    if (x.records.Count() > 0)
                    {
                        var timingsList = x.records;
                        foreach (var el in timingsList)
                        {
                            if (el.StartAt == null && el.EndAt == null && el.OtType == null)
                                continue;

                            start = el.StartAt;
                            end = el.EndAt;
                            subtractTime = el.SubtractTime;
                            ottype = el.OtType;
                        }
                    }

                    TimeSpan? noSubtractTime = new TimeSpan(0, 0, 0);

                    finalList.Add(new EmployeeLabor.TimingsRecord
                    {
                        EmployeeNumber = x.EmployeeNumber,
                        EmployeeName = x.EmployeeName,
                        JobDescription = x.JobDescription,
                        ActivityDate = x.ActivityDate,
                        JobNumber = x.JobNumber,
                        JobStatus = x.JobStatus,
                        Foreman = x.Foreman,
                        StartAt = start,
                        EndAt = end,
                        SubtractTime = subtractTime == null ? noSubtractTime : subtractTime,
                        OtType = ottype
                    });
                }

                if (finalList == null) continue;

                laborRecord.WeeklyTimingsRecords = finalList;
            }
        }
        public static void PopulateDailyTimings(long activityId, IEnumerable<BaseActivityQueryResponse.EmployeeLabor> laborRecords)
        {

            foreach (var laborRecord in laborRecords)
            {

                var activityDate = GetActivityDate(activityId);


                var result = getDailyTimings(activityDate, laborRecord.EmployeeNumber);

                var groupedResult = result
                                   .GroupBy(a => new { a.EmployeeNumber, a.EmployeeName, a.JobDescription, a.ActivityDate, a.JobNumber, a.JobStatus, a.Foreman })
                                   .Select(x => new
                                   {
                                       EmployeeNumber = x.Key.EmployeeNumber,
                                       EmployeeName = x.Key.EmployeeName,
                                       JobDescription = x.Key.JobDescription,
                                       ActivityDate = x.Key.ActivityDate,
                                       JobNumber = x.Key.JobNumber,
                                       JobStatus = x.Key.JobStatus,
                                       Foreman = x.Key.Foreman,
                                       records = x.Select(y => new {
                                           y.StartAt,
                                           y.EndAt,
                                           y.SubtractTime,
                                           y.OtType
                                       }).ToList()
                                   }).ToList();

                var finalList = new List<EmployeeLabor.TimingsRecord>();

                foreach (var x in groupedResult)
                {
                    TimeSpan? start = null;

                    TimeSpan? end = null;

                    TimeSpan? subtractTime = new TimeSpan(0, 0, 0);

                    string ottype = "";

                    if (x.records.Count() > 0)
                    {
                        var timingsList = x.records;
                        foreach (var el in timingsList)
                        {
                            if (el.StartAt == null && el.EndAt == null && el.OtType == null)
                                continue;

                            start = el.StartAt;
                            end = el.EndAt;
                            subtractTime = el.SubtractTime;
                            ottype = el.OtType;
                        }
                    }

                    TimeSpan? noSubtractTime = new TimeSpan(0, 0, 0);

                    finalList.Add(new EmployeeLabor.TimingsRecord
                    {
                        EmployeeNumber = x.EmployeeNumber,
                        EmployeeName = x.EmployeeName,
                        JobDescription = x.JobDescription,
                        ActivityDate = x.ActivityDate,
                        JobNumber = x.JobNumber,
                        JobStatus = x.JobStatus,
                        Foreman = x.Foreman,
                        StartAt = start,
                        EndAt = end,
                        SubtractTime = subtractTime == null ? noSubtractTime : subtractTime,
                        OtType = ottype
                    });
                }

                if (finalList == null) continue;

                laborRecord.DailyTimingsRecords = finalList;
            }
        }

        public static void AddDailyLaborTotals(long activityId, IEnumerable<BaseActivityQueryResponse.EmployeeLabor> laborRecords)
        {


            var results = SharedLaborDailyTotalRepository
                .GetByActivityAsync(activityId);



            foreach (var laborRecord in laborRecords)
            {
                var result = results.FirstOrDefault(x => x.EmployeeNumber == laborRecord.EmployeeNumber);

                if (result == null) continue;

                laborRecord.TotalHoursForDay = new HoursData
                {
                    OvertimeHours = result.OvertimeHours,
                    DoubleTimeHours = result.DoubleTimeHours,
                    StandardHours = result.StandardHours,
                    Overrides = result.Overrides,
                    PaidTimeOff = result.PaidTimeOff,
                    HolidayPay = result.HolidayPay,
                    AutoAllowance = result.AutoAllowance,
                    PerDiem = result.PerDiam,
                    PerDiemType = result.PerDiamType,
                    RigRentals = result.RigRentals,
                    MiscCode = result.MiscCode,
                    TravelPay = result.TravelPay,
                    WaitTimeHours = result.WaitTimeHours
                };
            }
        }

        public static void AddWeeklyLaborTotals(long activityId, IEnumerable<BaseActivityQueryResponse.EmployeeLabor> laborRecords)
        {
            var results = SharedLaborWeeklyTotalRepository
                .GetByActivityAsync(activityId);

            foreach (var laborRecord in laborRecords)
            {
                var result = results.FirstOrDefault(x => x.EmployeeNumber == laborRecord.EmployeeNumber);

                if (result == null) continue;

                laborRecord.TotalHoursForWeek = new HoursData
                {
                    OvertimeHours = result.OvertimeHours,
                    DoubleTimeHours = result.DoubleTimeHours,
                    StandardHours = result.StandardHours,
                    Overrides = result.Overrides,
                    PaidTimeOff = result.PaidTimeOff,
                    HolidayPay = result.HolidayPay,
                    AutoAllowance = result.AutoAllowance,
                    PerDiem = result.PerDiam,
                    PerDiemType = result.PerDiamType,
                    RigRentals = result.RigRentals,
                    MiscCode = result.MiscCode,
                    TravelPay = result.TravelPay,
                    WaitTimeHours = result.WaitTimeHours
                };
            }
        }

        public static List<PayLevelCode> GetPecoUnionCodePayLevels(string companyCode, string unionCode)
        {
            var unionCodeParam = new SqlParameter("@UnionCode", SqlDbType.VarChar) { Value = unionCode };

            var companyCodeParam = new SqlParameter("@CompanyCode", SqlDbType.VarChar) { Value = companyCode };

            List<PayLevelCode> tempLevelList = new List<PayLevelCode>();

            IEnumerable<PayLevel> payLevel = new List<PayLevel>();

            var query = $"exec GET_PAY_LEVELS_FOR_PECO_UNION_CODE_SP {unionCodeParam}, {companyCodeParam}";

            using (var _context = new ApplicationContext())
            {
                try
                {
                    payLevel = ((ApplicationContext)_context)
                                   .Database
                                   .SqlQuery<PayLevel>(query, unionCodeParam, companyCodeParam)
                                   .ToList();

                }
                catch (Exception e)
                {
                    string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                    throw new Exception(fullexceptiondetails);
                }


                foreach (var p in payLevel)
                {
                    foreach (var property in p.GetType().GetProperties())
                    {
                        var level = new PayLevelCode();
                        //Trace.WriteLine(property.Name + " " + property.GetValue(p));
                        level.Level = property.Name;
                        level.Value = property.GetValue(p).ToString();
                        if (!String.IsNullOrEmpty(level.Value))
                        {
                            tempLevelList.Add(level);
                        }
                    }
                }
            }

            return tempLevelList;
        }
        private static IEnumerable<PayLevelCode> GetPayLevelsOfDefaultEmployeeUnionCode(string unionCode)
        {
            var unionCodeParam = new SqlParameter("@UnionCode", SqlDbType.VarChar) { Value = unionCode };

            List<PayLevelCode> tempLevelList = new List<PayLevelCode>();
            using (var _context = new ApplicationContext())
            {


                if (unionCodeParam.Value != null)
                {
                    IEnumerable<PayLevel> payLevel = new List<PayLevel>();

                    var query = $"exec GET_EMPLOYEE_PAY_LEVEL_DEFAULT_UNION_CODE {unionCodeParam}";

                    try
                    {
                        payLevel = ((ApplicationContext)_context)
                                       .Database
                                       .SqlQuery<PayLevel>(query, unionCodeParam)
                                       .ToList();

                    }
                    catch (Exception e)
                    {
                        string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                        throw new Exception(fullexceptiondetails);
                    }

                    foreach (var p in payLevel)
                    {
                        foreach (var property in p.GetType().GetProperties())
                        {
                            var level = new PayLevelCode();
                            level.Level = property.Name;
                            level.Value = property.GetValue(p).ToString();
                            tempLevelList.Add(level);
                        }
                    }
                }
            }
            return tempLevelList;

        }
        public static IEnumerable<PayLevelCode> GetPayLevelsByEmployeeUnionCode(string unionCode, string employeeNumber)
        {
            List<PayLevelCode> tempLevelList = new List<PayLevelCode>();

            using (var _context = new ApplicationContext())
            {

                var empCompanyCode = _context
                    .Get<Employee>()
                    .Where(e => e.EmployeeNumber == employeeNumber && e.EmployeeStatus == "A" && e.CompanyCode != null)
                    .Select(c => c.CompanyCode).ToList();

                var unionCodeParam = new SqlParameter("@UnionCode", SqlDbType.VarChar) { Value = unionCode };
                var companyCodeParam = new SqlParameter("@CompanyCode", SqlDbType.VarChar) { Value = empCompanyCode.Select(x => x).FirstOrDefault() };



                if (unionCodeParam.Value != null && companyCodeParam.Value != null)
                {
                    IEnumerable<PayLevel> payLevel = new List<PayLevel>();

                    var query = $"exec GET_EMPLOYEE_PAY_LEVEL_BY_UNION_CODE {unionCodeParam}, {companyCodeParam}";

                    try
                    {
                        payLevel = ((ApplicationContext)_context)
                                       .Database
                                       .SqlQuery<PayLevel>(query, unionCodeParam, companyCodeParam)
                                       .ToList();

                    }
                    catch (Exception e)
                    {
                        string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                        throw new Exception(fullexceptiondetails);
                    }

                    foreach (var p in payLevel)
                    {
                        foreach (var property in p.GetType().GetProperties())
                        {
                            var level = new PayLevelCode();
                            level.Level = property.Name;
                            level.Value = property.GetValue(p).ToString();
                            tempLevelList.Add(level);
                        }
                    }
                }
            }
            return tempLevelList;

        }

        private static List<PecoUnionCode> GetPecoUnionCodes(string companyCode)
        {
            var companyCodeParam = new SqlParameter("@CompanyCode", SqlDbType.VarChar) { Value = companyCode };

            List<PecoUnionCode> pecoUnionCodes = new List<PecoUnionCode>();
            //List<SpecialProjectsUnionCode> unionCodes = new List<SpecialProjectsUnionCode>();
            using (var _context = new ApplicationContext())
            {
                var query = $"exec GET_PECO_UNION_CODES {companyCodeParam}";

                try
                {
                    pecoUnionCodes = ((ApplicationContext)_context)
                                   .Database
                                   .SqlQuery<PecoUnionCode>(query, companyCodeParam)
                                   .ToList();

                }
                catch (Exception e)
                {
                    string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                    throw new Exception(fullexceptiondetails);
                }
            }

            return pecoUnionCodes;
        }
        public static List<CrewlinkServices.Features.DailyActivity.Labor.Get.JobUnionCode> GetSPUnionCodes(string companyCode)
        {
            var companyCodeParam = new SqlParameter("@CompanyCode", SqlDbType.VarChar) { Value = companyCode };

            List<CrewlinkServices.Features.DailyActivity.Labor.Get.JobUnionCode> spUnionCodes = new List<JobUnionCode>();
            //List<SpecialProjectsUnionCode> unionCodes = new List<SpecialProjectsUnionCode>();

            var query = $"exec GET_PECO_UNION_CODES {companyCodeParam}";
            using (var _context = new ApplicationContext())
            {

                try
                {
                    spUnionCodes = ((ApplicationContext)_context)
                                   .Database
                                   .SqlQuery<JobUnionCode>(query, companyCodeParam)
                                   .ToList();

                }
                catch (Exception e)
                {
                    string fullexceptiondetails = e.Message + "||||" + e.StackTrace;
                    throw new Exception(fullexceptiondetails);
                }
            }

            return spUnionCodes;
        }
    }
}
