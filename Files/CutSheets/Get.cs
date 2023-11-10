namespace Crewlink.WindowsServices.Files.CutSheets
{
using CrewLink.WindowsServices.Files.Shared;
using CrewlinkServices.Core.DataAccess;
using CrewlinkServices.Core.Models;
using CrewlinkServices.Core.Request.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using CrewlinkServices.Features.DailyActivity.CutSheets;


    //public class Response : ResponseBase
    //{
    //    public CutSheetsActivityData CutSheetsActivity = new CutSheetsActivityData();

        //public class CutSheetsActivityData
        //{
        //    public long ResurfacingId { get; set; }

        //    public IEnumerable<JobSurfacesRemoved> SurfacesRemovedData { get; set; } = new List<JobSurfacesRemoved>();

        //    public IEnumerable<JobTemporarySurfaces> TemporarySurfaceData { get; set; } = new List<JobTemporarySurfaces>();

        //    public string WeatherConditions { get; set; }

        //    public string WorkOrderNumber { get; set; }

        //    public string PurchaseOrderNumber { get; set; }

        //    public string RestorationOrderNumber { get; set; }

        //    public string StreetAddress { get; set; }

        //    public string CityCode { get; set; }

        //    public string StateCode { get; set; }

        //    public short? CityId { get; set; }

        //    public bool? TrafficControl { get; set; }

        //    public bool? CustomerComplaint { get; set; }
        //    public bool? IsJobCompleted { get; set; }
        //    public bool? IsRestorationRequired { get; set; }
        //    public string RestorationData { get; set; }

        //    public DateTime? BacklogWorkDate { get; set; }
        //}
    //}
    public class GetCutSheetsData
    {
        public static Get.Response ExecuteRequest(long ResurfacingId)
        {
            var activityData = new Get.Response.CutSheetsActivityData();

            var surfacesRemoved = SharedDFRDataRepository.GetExistingSurfacesRemovedData(ResurfacingId);

            var temporarySurfaces = SharedDFRDataRepository.GetExistingTemporarySurfacesData(ResurfacingId);

            var weatherConditions =  GetWeatherConditions(ResurfacingId);

            var resurfacingData =  GetWorkOrderNumber(ResurfacingId);

            //  var address = await GetAddress(request.ResurfacingId);

            activityData.ResurfacingId = ResurfacingId;
            activityData.SurfacesRemovedData = surfacesRemoved;
            activityData.TemporarySurfaceData = temporarySurfaces;
            activityData.WeatherConditions = weatherConditions;
            activityData.WorkOrderNumber = resurfacingData.WorkOrderNumber;
            activityData.PurchaseOrderNumber = resurfacingData.PurchaseOrderNumber;
            activityData.RestorationOrderNumber = resurfacingData.RestorationOrderNumber;
            activityData.StreetAddress = resurfacingData.Address;

             GetOtherDetails(activityData, ResurfacingId);

             GetSurfacesAndMaterialsName(activityData);

            GetInchesAndFeet(activityData);

            var result = new Get.Response();
            result.CutSheetsActivity = activityData;

            return result;
        }
        public static void GetInchesAndFeet(Get.Response.CutSheetsActivityData activityData)
        {
            foreach (var surface in activityData.SurfacesRemovedData)
            {
                surface.QuantityLengthFeet = surface.QuantityLength.ToString().Split('.')[0];

                var inches = decimal.Parse(surface.QuantityLength.ToString().Split('.')[1]);

                inches = ((inches / 100) * 12);

                surface.QuantityLengthInches = Math.Round(inches).ToString();

                surface.QuantityDepthFeet = surface.QuantityDepth.ToString().Split('.')[0];

                var inchesDepth = decimal.Parse(surface.QuantityDepth.ToString().Split('.')[1]);

                inchesDepth = ((inchesDepth / 100) * 12);

                surface.QuantityDepthInches = Math.Round(inchesDepth).ToString();

                surface.QuantityWidthFeet = surface.QuantityWidth.ToString().Split('.')[0];

                var inchesWidth = decimal.Parse(surface.QuantityWidth.ToString().Split('.')[1]);

                inchesWidth = ((inchesWidth / 100) * 12);

                surface.QuantityWidthInches = Math.Round(inchesWidth).ToString();
            }

            foreach (var surface in activityData.TemporarySurfaceData)
            {
                surface.QuantityLengthFeet = surface.QuantityLength.ToString().Split('.')[0];

                var inches = decimal.Parse(surface.QuantityLength.ToString().Split('.')[1]);

                inches = ((inches / 100) * 12);

                surface.QuantityLengthInches = Math.Round(inches).ToString();

                surface.QuantityDepthFeet = surface.QuantityDepth.ToString().Split('.')[0];

                var inchesDepth = decimal.Parse(surface.QuantityDepth.ToString().Split('.')[1]);

                inchesDepth = ((inchesDepth / 100) * 12);

                surface.QuantityDepthInches = Math.Round(inchesDepth).ToString();

                surface.QuantityWidthFeet = surface.QuantityWidth.ToString().Split('.')[0];

                var inchesWidth = decimal.Parse(surface.QuantityWidth.ToString().Split('.')[1]);

                inchesWidth = ((inchesWidth / 100) * 12);

                surface.QuantityWidthInches = Math.Round(inchesWidth).ToString();
            }
        }

        public static void GetSurfacesAndMaterialsName(Get.Response.CutSheetsActivityData activityData)
        {
            using (var _context = new ApplicationContext())
            {
                foreach (var surface in activityData.SurfacesRemovedData)
                {
                    var surfaceType =  _context
                        .Get<SurfaceType>()
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == surface.SurfaceTypeId);

                    surface.SurfaceTypeName = surfaceType?.SurfaceTypeName ?? "";

                    var materialType =  _context
                        .Get<MaterialType>()
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == surface.MaterialTypeId);

                    surface.MaterialTypeName = materialType?.MaterialTypeName ?? "";
                }

                foreach (var surface in activityData.TemporarySurfaceData)
                {
                    var temporaryFill =  _context
                        .Get<TemporaryFill>()
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == surface.TemporaryFillId);

                    surface.TemporaryFillName = temporaryFill?.TemporaryFillName ?? "";

                    var addMaterial =  _context
                        .Get<AddMaterial>()
                        .AsNoTracking()
                        .FirstOrDefault(x => x.Id == surface.AddMaterialId);

                    surface.AddMaterialName = addMaterial?.AddMaterialName ?? "";
                }
            }
        }
        public static void GetOtherDetails(Get.Response.CutSheetsActivityData activityData, long resurfacingId)
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
                activityData.IsJobCompleted = resurfacing.IsJobCompleted;
                activityData.IsRestorationRequired = resurfacing.IsRestorationRequired;
                activityData.RestorationData = resurfacing.RestorationData;
            }
        }
        public static string GetWeatherConditions(long resurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                return  _context
                .Get<ActivityComment>()
                .Where(x => x.Type == ActivityCommentType.CutSheets && x.ResurfacingId == resurfacingId)
                .Select(x => x.Comment).FirstOrDefault();
            }
        }

        public static Resurfacing GetWorkOrderNumber(long resurfacingId)
        {
            using (var _context = new ApplicationContext())
            {
                try
                {
                    return ( _context
                                        .Get<Resurfacing>()
                                        .FirstOrDefault(x => x.Id == resurfacingId));
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.ToString());
                }
            }

        }
    }
}
