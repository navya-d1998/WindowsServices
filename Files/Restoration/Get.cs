using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Features.DailyActivity.Restoration;
using System.Data.Entity;

namespace Crewlink.WindowsServices.Files.Restoration
{
    public class GetRestorationData
    {

        public static Get.Response ExecuteRequest(long[] ResurfacingIds)
        {
            var restorationActivity = new List<Get.Response.RestorationActivityData>();

            foreach (var resurfacingId in ResurfacingIds)
            {
                var activityData = new Get.Response.RestorationActivityData();

                var surfacesRestored = SharedDFRDataRepository.GetExistingSurfacesRestoredData(resurfacingId);

                var addMaterials = SharedDFRDataRepository.GetExistingAddMaterialsData(resurfacingId);

                var weatherConditions =  GetWeatherConditions(resurfacingId);

                var resurfacingData =  GetWorkOrderNumber(resurfacingId);

     
                activityData.ResurfacingId = resurfacingId;
                activityData.SurfacesRestoredData = surfacesRestored;
                activityData.AddMaterialData = addMaterials;

                activityData.WeatherConditions = weatherConditions;
     
                activityData.WorkOrderNumber = resurfacingData.WorkOrderNumber;

                activityData.PurchaseOrderNumber = resurfacingData.PurchaseOrderNumber;
         
                activityData.RestorationOrderNumber = resurfacingData.RestorationOrderNumber;
    
                activityData.StreetAddress = resurfacingData.Address;
     
                GetOtherDetails(activityData, resurfacingId);
      
                GetSurfacesAndMaterialsName(activityData);
         
                GetInchesAndFeet(activityData);
    
                restorationActivity.Add(activityData);
            }
     
            return new Get.Response
            {
                RestorationActivity = restorationActivity
            };
        }


        public static  string GetWeatherConditions(long resurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                return _context
                .Get<ActivityComment>()
                .Where(x => x.Type == ActivityCommentType.Restoration && x.ResurfacingId == resurfacingId)
                .Select(x => x.Comment).FirstOrDefault();
            }
        }

        public static  Resurfacing GetWorkOrderNumber(long resurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                try
                {
                    return (_context
                                        .Get<Resurfacing>()
                                        .FirstOrDefault(x => x.Id == resurfacingId));
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }


        }

        public static  string GetAddress(long resurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                return (_context
                .Get<Resurfacing>()
                .FirstOrDefault(x => x.Id == resurfacingId)).Address;
            }

        }

        public static void GetOtherDetails(Get.Response.RestorationActivityData activityData, long resurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                var resurfacing = _context
                .Get<Resurfacing>()
                .Include(x => x.City)
                .FirstOrDefault(x => x.Id == resurfacingId);

                if (resurfacing == null)
                {
                    resurfacing = _context
                    .Get<Resurfacing>()
                    .FirstOrDefault(x => x.Id == resurfacingId);
                }

                activityData.CityId = resurfacing.CityId;
                activityData.CustomerComplaint = resurfacing.CustomerComplaint;
                activityData.TrafficControl = resurfacing.TrafficControl;
                activityData.StateCode = resurfacing.City?.StateCode;
                activityData.CityCode = resurfacing.City?.CityCode;
                activityData.BacklogWorkDate = resurfacing.BacklogWorkDate;
            }
        }

        public static void GetSurfacesAndMaterialsName(Get.Response.RestorationActivityData activityData)
        {
            using (var _context = new ApplicationContext())
            {
                foreach (var surface in activityData.SurfacesRestoredData)
                {
                    var surfaceType = _context
                        .Get<SurfaceType>()
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == surface.SurfaceTypeId);

                    surface.SurfaceTypeName = surfaceType?.SurfaceTypeName ?? "";

                    var materialType = _context
                        .Get<MaterialType>()
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == surface.MaterialTypeId);

                    surface.MaterialTypeName = materialType?.MaterialTypeName ?? "";
                }

                foreach (var surface in activityData.AddMaterialData)
                {
                    var addMaterial = _context
                        .Get<AddMaterial>()
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == surface.AddMaterialId);

                    surface.AddMaterialName = addMaterial?.AddMaterialName ?? "";
                }
            }
        }

        public static void GetInchesAndFeet(Get.Response.RestorationActivityData activityData)
        {
            using (var _context = new ApplicationContext())
            {
                foreach (var surface in activityData.SurfacesRestoredData)
                {
                    surface.OriginalLengthFeet = surface.OriginalLength.ToString().Split('.')[0];

                    var inches = decimal.Parse(surface.OriginalLength.ToString().Split('.')[1]);

                    inches = ((inches / 100) * 12);

                    surface.OriginalLengthInches = Math.Round(inches).ToString();

                    surface.OriginalDepthFeet = surface.OriginalDepth.ToString().Split('.')[0];

                    var inchesDepth = decimal.Parse(surface.OriginalDepth.ToString().Split('.')[1]);

                    inchesDepth = ((inchesDepth / 100) * 12);

                    surface.OriginalDepthInches = Math.Round(inchesDepth).ToString();

                    surface.OriginalWidthFeet = surface.OriginalWidth.ToString().Split('.')[0];

                    var inchesWidth = decimal.Parse(surface.OriginalWidth.ToString().Split('.')[1]);

                    inchesWidth = ((inchesWidth / 100) * 12);

                    surface.OriginalWidthInches = Math.Round(inchesWidth).ToString();
                }

                foreach (var surface in activityData.SurfacesRestoredData)
                {
                    surface.RestoredLengthFeet = surface.RestoredLength.ToString().Split('.')[0];

                    var inches = decimal.Parse(surface.RestoredLength.ToString().Split('.')[1]);

                    inches = ((inches / 100) * 12);

                    surface.RestoredLengthInches = Math.Round(inches).ToString();

                    surface.RestoredDepthFeet = surface.RestoredDepth.ToString().Split('.')[0];

                    var inchesDepth = decimal.Parse(surface.RestoredDepth.ToString().Split('.')[1]);

                    inchesDepth = ((inchesDepth / 100) * 12);

                    surface.RestoredDepthInches = Math.Round(inchesDepth).ToString();

                    surface.RestoredWidthFeet = surface.RestoredWidth.ToString().Split('.')[0];

                    var inchesWidth = decimal.Parse(surface.RestoredWidth.ToString().Split('.')[1]);

                    inchesWidth = ((inchesWidth / 100) * 12);

                    surface.RestoredWidthInches = Math.Round(inchesWidth).ToString();
                }
            }
        }



    }
}
