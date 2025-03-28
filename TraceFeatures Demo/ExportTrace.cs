using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using System;
using System.IO;
using System.Linq;
using System.Web;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using ArcGIS.Desktop.Framework.Dialogs;

namespace ExportTraceAsJson
{
  internal class ExportTrace : Button
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

          //IReadOnlyList<Result> traceResults = connectedTracer.Trace(traceArgument, ServiceSynchronizationType.Asynchronous);

          TraceExportOptions exportOptions = new TraceExportOptions()
          {
            ServiceSynchronizationType = ServiceSynchronizationType.Asynchronous,
            IncludeDomainDescriptions = true,
          };

          string jsonPath = Path.Combine(Environment.CurrentDirectory, $"TraceResults_{DateTime.Now.Ticks}.json");
          Uri jsonUri = new Uri(jsonPath);
          
          // Export trace params: jsonPath, traceArgument, exportOptions
          connectedTracer.Export(jsonUri, traceArgument, exportOptions);

          string jsonAbsolutePath = HttpUtility.UrlDecode(jsonUri.AbsolutePath);
          if (jsonUri.IsFile && File.Exists(jsonAbsolutePath))
          {
            MessageBox.Show($"Trace Results exported as a JSON File @ {jsonAbsolutePath}");
          }
        }
      });
    }
  }
}
