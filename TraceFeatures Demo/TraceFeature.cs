using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using Element = ArcGIS.Core.Data.UtilityNetwork.Element;

namespace ExportTraceAsJson
{
  internal class TraceFeature : Button
  {
    protected override void OnClick()
    {
      QueuedTask.Run(async () =>
      {
        UtilityNetworkLayer utilityNetworkLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<UtilityNetworkLayer>().FirstOrDefault();
        if (utilityNetworkLayer is null)
        {
          return;
        }

        using (UtilityNetwork utilityNetwork = utilityNetworkLayer.GetUtilityNetwork())
        using (TraceManager traceManager = utilityNetwork.GetTraceManager())
        {
          TraceArgument traceArgument = await Shared.GetTraceArgumentAsync(utilityNetwork);

          ConnectedTracer connectedTracer = traceManager.GetTracer<ConnectedTracer>();
          IReadOnlyList<Result> traceResults = connectedTracer.Trace(traceArgument, ServiceSynchronizationType.Asynchronous);

          #region Display trace results in the Map

          Dictionary<MapMember, List<long>> selectionDictionary = new Dictionary<MapMember, List<long>>();
          foreach (Result traceResult in traceResults)
          {
            if (traceResult is FeatureElementResult featureElementResult)
            {
              IEnumerable<Element> elements = featureElementResult.FeatureElements;
              foreach (Element element in elements)
              {
                Table table = utilityNetwork.GetTable(element.NetworkSource);
                string tableName = table.GetName();
                FeatureLayer featLayer = MapView.Active.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().FirstOrDefault(f => f.GetFeatureClass().GetName().Contains(tableName));

                if (selectionDictionary.ContainsKey(featLayer))
                {
                  selectionDictionary[featLayer].Add(element.ObjectID);
                }
                else
                {
                  selectionDictionary.Add(featLayer, new List<long>() { element.ObjectID });
                }
              }

              SelectionSet selectionSet = SelectionSet.FromDictionary(selectionDictionary);
              MapView.Active.Map.SetSelection(selectionSet);

              // Zoom to selection and redraw
              MapView.Active.ZoomToSelectedAsync();
              MapView.Active.Redraw(true);
            }
          }

          #endregion
        }
      });
    }
  }
}
