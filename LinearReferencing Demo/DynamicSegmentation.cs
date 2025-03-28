using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data.LinearReferencing;

namespace LinearReferencingDemo
{
  internal class DynamicSegmentation : Button
  {
    protected override void OnClick()
    {
      FeatureLayer layer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault();
      if (layer is null)
      {
        MessageBox.Show("Feature layer not found!");
        return;
      }

      QueuedTask.Run(() =>
      {
        using (FeatureClass featureClass = layer.GetFeatureClass())
        using (Geodatabase geodatabase = featureClass.GetDatastore() as Geodatabase)
        using (FeatureClass busRoutesFeatureClass = geodatabase.OpenDataset<FeatureClass>(name: "BusRoutes"))
        using (FeatureClassDefinition busRoutesFeatureClassDefinition = busRoutesFeatureClass.GetDefinition())
        using (Table busStopPointEventsTable = geodatabase.OpenDataset<Table>(name: "BusStopPointEvents"))
        {
          if (!busRoutesFeatureClassDefinition.HasM())
          {
            return;
          }
          // Read route information
          RouteInfo busRouteInfo = new RouteInfo(busRoutesFeatureClass, routeIDFieldName: "RouteID");

          // Read event information
          EventInfo busStopEventInfo = new PointEventInfo(busStopPointEventsTable, routeIDFieldName: "RouteID",
            measureFieldName: "Measure_m");

          // Offset options
          // var busStopEventInfo = new PointEventInfo(busStopPointEventsTable, routeIDFieldName: "RouteID", measureFieldName: "Measure_m", offsetFieldName:"Offset");

          // Define route event source options
          PointEventSourceOptions pointEventSourceOptions = new PointEventSourceOptions(AngleType.Normal)
          {
            // Add error field to the RES attribute table for QA/QC
            AddErrorField = true,
            
            // Calculate complement angle on locating events
            ComplementAngle = true
          };

          using (RouteEventSource routeEventSource = new RouteEventSource(busRouteInfo, busStopEventInfo, pointEventSourceOptions))
          using (RouteEventSourceDefinition routeEventSourceDefinition = routeEventSource.GetDefinition())
          {
            // Locating errors 
            IReadOnlyList<RouteEventSourceError> errors = routeEventSource.GetErrors();
            
            // Route event source fields 
            IReadOnlyList<Field> routeEventSourceFields = routeEventSourceDefinition.GetFields();

            #region Add RouteEventSource to the ArcGIS Pro map
            
            // Get feature layer creation for RES
            FeatureLayerCreationParams resLayerParams = new FeatureLayerCreationParams(routeEventSource)
            {
              Name = "LocationOfBusStops_From_DS"
            };

            // Creates RES layer and add to the map
            FeatureLayer featureLayer = LayerFactory.Instance.CreateLayer<FeatureLayer>(resLayerParams, MapView.Active.Map);
            
            // Unique value render 
            UniqueValueRendererDefinition uvrDef = new UniqueValueRendererDefinition()
            {
              ValueFields = ["subtypes"],
              SymbolTemplate = SymbolFactory.Instance.ConstructPointSymbol(ColorFactory.Instance.GreenRGB,
                  10, SimpleMarkerStyle.Hexagon).MakeSymbolReference(),
              ValuesLimit = 5
            };

            //Sets the renderer to the feature layer
            featureLayer.SetRenderer(featureLayer.CreateRenderer(uvrDef));
            
            #endregion
          }
        }
      });
    }
  }
}
