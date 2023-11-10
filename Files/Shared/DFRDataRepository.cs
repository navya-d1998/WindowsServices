using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Features.DailyActivity.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewLink.WindowsServices.Files.Shared
{
    public interface IDFRDataRepository
    {

        List<JobImages> GetExistingJobImageInfo(long ActivityId);

        List<JobImages> GetResurfacingImages(long ResurfacingId);

        string GetDFRData(long ActivityId, int DFRId);

        int GetDfrId(string UniqueKey);
        void SaveDFRData(long ActivityId, int DFRId, string DFRData, int UserId);

        void InvalidateSignature(long ActivityId, int DFRId);

        List<Signature> GetSignature(long ActivityId, int DFRId);

        void UpdateDFRData(long ActivityId, int DFRId, string DFRData, int UserId);

    }

    public class SharedDFRDataRepository
    {
        public static List<SectionARecord> GetDFRAdditionalInfoAsync(long ActivityId, string Section, int DfrId)
        {

            using (var _context = new ApplicationContext())
            {
                var activityIdParameter = new SqlParameter("@Job", SqlDbType.BigInt) { Value = ActivityId };

                var activitySectionParameter = new SqlParameter("@Section", SqlDbType.VarChar) { Value = Section };

                var activityDfrParameter = new SqlParameter("@DfrId", SqlDbType.Int) { Value = DfrId };

                var query = $"exec GET_MGE_ADDITIONAL_INFO {activityIdParameter.ParameterName},{activitySectionParameter.ParameterName},{activityDfrParameter.ParameterName}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<SectionARecord>(query, activityIdParameter, activitySectionParameter, activityDfrParameter)
                    .ToList();
            }
        }
        public static List<EquipmentPayitemRecord> GetSPIREHourlyWorkEqupimentAsync(long activityId, string workOderNo)
        {
            using (var _context = new ApplicationContext())
            {
                var idParameter = new SqlParameter("@JobId", SqlDbType.BigInt) { Value = activityId };

                var wOParameter = new SqlParameter("@WorkOrderID", SqlDbType.NVarChar) { Value = workOderNo };

                var query = $"exec SPIRE_DPR_HOURLY_WORK_EQUIPMENT {idParameter.ParameterName},{wOParameter.ParameterName}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<EquipmentPayitemRecord>(query, idParameter, wOParameter)
                    .ToList();
            }
        }
        public static List<LaborPayitemRecord> GetSPIREHourlyWorkLaborAsync(long activityId, string workOderNo)
        {
            using (var _context = new ApplicationContext())
            {
                var idParameter = new SqlParameter("@JobId", SqlDbType.BigInt) { Value = activityId };

                var wOParameter = new SqlParameter("@WorkOrderID", SqlDbType.NVarChar) { Value = workOderNo };

                var query = $"exec SPIRE_DPR_HOURLY_WORK_LABOR {idParameter.ParameterName},{wOParameter.ParameterName}";

                return ((ApplicationContext)_context)
                    .Database
                    .SqlQuery<LaborPayitemRecord>(query, idParameter, wOParameter)
                    .ToList();
            }
        }

        public static List<JobAddMaterials> GetExistingAddMaterialsData(long ResurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                .Get<JobAddMaterials>()
                .AsNoTracking()
                .Where(x => x.ResurfacingId == ResurfacingId)
                .Where(x => x.IsActive == true)
                .ToList();
            }
        }
        public static List<JobSurfacesRestored> GetExistingSurfacesRestoredData(long ResurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                .Get<JobSurfacesRestored>()
                .AsNoTracking()
                .Where(x => x.ResurfacingId == ResurfacingId)
                .Where(x => x.IsActive == true)
                .ToList();
            }
        }


        public static List<JobSurfacesRemoved> GetExistingSurfacesRemovedData(long ResurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                .Get<JobSurfacesRemoved>()
                .AsNoTracking()
                .Where(x => x.ResurfacingId == ResurfacingId)
                .Where(x => x.IsActive == true)
                .ToList();
            }
        }

        public static List<JobTemporarySurfaces> GetExistingTemporarySurfacesData(long ResurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                .Get<JobTemporarySurfaces>()
                .AsNoTracking()
                .Where(x => x.ResurfacingId == ResurfacingId)
                .Where(x => x.IsActive == true)
                .ToList();
            }
        }
        public static void UpdateDFRData(long ActivityId, int DFRId, string DFRData, int UserId)
        {
            using (var _context = new ApplicationContext())
            {
                var existingMapping = GetExistingData(ActivityId, DFRId);

                if (existingMapping != null)
                {
                    existingMapping.DFRData = DFRData;

                    existingMapping.Modified = DateTime.Now;

                    existingMapping.ModifiedBy = UserId;

                    _context.Update(existingMapping);
                }
            }
        }
        public static List<Signature> GetSignature(long ActivityId, int DFRId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                   .Get<Signature>()
                   .Where(Signature.IsActiveQueryFilter)
                   .Where(x => x.JobId == ActivityId && x.DFRId == DFRId)
                   .ToList();
            }
        }
        public static void InvalidateSignature(long ActivityId, int DFRId)
        {
            using (var _context = new ApplicationContext())
            {
                var existingSignatures = _context
                   .Get<Signature>()
                   .Where(Signature.IsActiveQueryFilter)
                   .Where(x => x.JobId == ActivityId && x.DFRId == DFRId)
                   .ToList();

                foreach (var existingSignature in existingSignatures)
                {
                    existingSignature.IsActive = false;
                    // Invalidated because of data change
                    existingSignature.InvalidatedBy = 2;

                    existingSignature.Modified = DateTime.Now;

                    _context.Update(existingSignature);
                }
            }
        }

        public static void SaveDFRData(long ActivityId, int DFRId, string DFRData, int UserId)
        {
            using (var _context = new ApplicationContext())
            {
                var existingMapping = GetExistingData(ActivityId, DFRId);

                if (existingMapping == null)
                {
                    var newDFRData = DFRDataMapping(ActivityId, DFRId, DFRData, UserId);

                    _context.Add(newDFRData);
                    _context.SaveChanges();

                }
            }
        }

        public static DfrData GetExistingData(long ActivityId, int DFRId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                   .Get<DfrData>()
                   .Where(DfrData.IsActiveQueryFilter)
                   .Where(x => x.JobId == ActivityId && x.DFRId == DFRId)
                   .FirstOrDefault();
            }
        }

        public static DfrData DFRDataMapping(long activityId, int dfrId, string dfrData, int UserId)
        {
            using (var _context = new ApplicationContext())
            {
                var mapping = new DfrData
                {
                    JobId = activityId,
                    DFRId = dfrId,
                    DFRData = dfrData,
                    IsActive = true,
                    Created = DateTime.Now,
                    CreatedBy = UserId
                };
                return mapping;
            }
        }


        public static int GetDfrId(string UniqueKey)
        {
            using (var _context = new ApplicationContext())
            {
                var dfrId = _context
                    .Get<Dfr>()
                    .Where(Dfr.IsActiveQueryFilter)
                    .Where(x => x.UniqueKey.ToUpper() == UniqueKey.ToUpper())
                    .Select(x => x.Id)
                    .FirstOrDefault();

                return dfrId;
            }
        }
        public static string GetDFRData(long ActivityId, int DFRId)
        {
            using (var _context = new ApplicationContext())
            {
                var result = _context
                    .Get<DfrData>()
                    .AsNoTracking()
                    .Where(DfrData.IsActiveQueryFilter)
                    .Where(x => x.JobId == ActivityId && x.DFRId == DFRId)
                    .Select(x => new
                    {
                        x.DFRData
                    }).FirstOrDefault();


                if (result != null)

                { return result.DFRData; }
            }

            return string.Empty;
        }
        //public static List<JobImages> GetExistingJobImageInfo(long ActivityId)
        //{
        //    var imagesList = new List<JobImages>();
        //    using (var _imageContext = new ImageContext())
        //    {
        //        imagesList = _imageContext
        //        .Get<JobImages>()
        //        .AsNoTracking()
        //        .Where(x => x.JobId == ActivityId)
        //        .Where(x => x.IsActive == true)
        //        .ToList();
        //    }
        //    return imagesList;
        //}
        public static List<JobImages> GetExistingJobImageInfo(long ActivityId)
        {
            using (var _imageContext = new ImageContext())
            {
                return _imageContext
                .Get<JobImages>()
                .AsNoTracking()
                .Where(x => x.JobId == ActivityId)
                .Where(x => x.IsActive == true)
                .ToList();
            }
        }

        public static List<JobImages> GetResurfacingImages(long ResurfacingId)
        {
            var imagesList = new List<JobImages>();
            using (var _imageContext = new ImageContext())
            {
                imagesList = _imageContext
                .Get<JobImages>()
                .AsNoTracking()
                .Where(x => x.ResurfacingId == ResurfacingId)
                .Where(x => x.IsActive == true)
                .ToList();
            }
            return imagesList;
        }

        public static string GetDfrName(int DfrId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                      .Get<Dfr>()
                      .Where(x => x.Id == DfrId)
                      .AsNoTracking()
                      .Select(x => x.Name).FirstOrDefault();
            }
        }

        public static void GetUploaderInfo(IList<JobImages> response)
        {
            using (var _context = new ApplicationContext())
            {
                foreach (var image in response)
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
    }

}
