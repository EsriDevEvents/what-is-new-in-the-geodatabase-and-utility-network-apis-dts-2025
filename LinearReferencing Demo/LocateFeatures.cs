using ArcGIS.Core.Data;
using ArcGIS.Core.Data.LinearReferencing;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Linq;
using ArcGIS.Core.Data.DDL;

namespace LinearReferencingDemo
{
  internal class LocateFeatures : Button
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
        // Point feature class as an input type
        using (FeatureClass proposedBusStopFeatureClass = geodatabase.OpenDataset<FeatureClass>(name: "ProposedBusStops"))
        {
          // Read route information
          RouteInfo busRouteInfo = new RouteInfo(busRoutesFeatureClass, routeIDFieldName: "RouteID");

          // Configure events table to store event information where route and input features intersects
          string eventTableName = "ProposedBusStopEventTable";
          PointEventTableConfiguration pointEventTableConfiguration = new PointEventTableConfiguration(eventTableName,
          routeIDFieldName: "RouteID", measureFieldName: "Measure")
          {
            KeepAllFields = true // All attributes from the input feature class that intersect with RouteInfo will be included 
          };

          #region clean workspace if needed 
          if (geodatabase.GetDefinitions<TableDefinition>().Any(d => d.GetName().Contains(eventTableName)))
          {
            SchemaBuilder sb = new SchemaBuilder(geodatabase);
            sb.Delete(new TableDescription(geodatabase.GetDefinition<TableDefinition>(eventTableName)));
            sb.Build();
          };
          #endregion

          busRouteInfo.LocateFeatures(proposedBusStopFeatureClass, searchRadius: 2, pointEventTableConfiguration);

          #region Open event table and it to the ArcGIS Pro map TOC

          using (Table proposedBusStopEventTable = geodatabase.OpenDataset<Table>(eventTableName))
          {
            StandaloneTableCreationParams standaloneTableCreationParams = new StandaloneTableCreationParams(proposedBusStopEventTable)
            {
              Name = "ProposedBusStopEvents"
            };

            StandaloneTableFactory.Instance.CreateStandaloneTable(standaloneTableCreationParams, MapView.Active.Map);
          }

          #endregion
        }
      });
    }
  }
}
