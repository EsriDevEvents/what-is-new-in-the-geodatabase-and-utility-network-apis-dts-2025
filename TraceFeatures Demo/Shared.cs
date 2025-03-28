using ArcGIS.Core.Data;
using ArcGIS.Core.Data.UtilityNetwork;
using ArcGIS.Core.Data.UtilityNetwork.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ExportTraceAsJson
{
  public static class Shared
  {
    public static Task<TraceArgument> GetTraceArgumentAsync(UtilityNetwork utilityNetwork)
    {
      using (UtilityNetworkDefinition utilityNetworkDefinition = utilityNetwork.GetDefinition())
      using (NetworkSource deviceNetworkSource = GetNetworkSource(utilityNetworkDefinition, "ElectricDevice") as NetworkSource)
      using (AssetGroup circuitBreakerAssetGroup = deviceNetworkSource.GetAssetGroup("High Voltage Switch"))
      using (AssetType circuitBreakerAssetType = circuitBreakerAssetGroup.GetAssetType("AC Circuit Breaker"))
      using (NetworkSource electricDistributionDeviceNetworkSource = GetNetworkSource(utilityNetworkDefinition, "ElectricDevice"))
      using (FeatureClass electricDistributionDeviceFeatureClass = utilityNetwork.GetTable(electricDistributionDeviceNetworkSource) as FeatureClass)
      using (FeatureClassDefinition electricDistributionDeviceDefinition = electricDistributionDeviceFeatureClass.GetDefinition())
      {
        TerminalConfiguration terminalConfig = circuitBreakerAssetType.GetTerminalConfiguration();
        IReadOnlyList<Terminal> terminals = terminalConfig.Terminals;
        Terminal terminal = terminals.First(x => x.IsUpstreamTerminal);

        // Trace starting element
        Guid startingPointGuid = Guid.Parse("{A1435B99-435F-4DD8-A4AE-51760EC6FF41}");
        Element startingPointElement = utilityNetwork.CreateElement(circuitBreakerAssetType, startingPointGuid, terminal);
        List<Element> startingPoints = new List<Element>() { startingPointElement };

        // Barriers, if any
        List<Element> barriers = new List<Element>();

        // Trace configuration
        TraceConfiguration traceConfiguration = new TraceConfiguration();
        traceConfiguration.IncludeContainers = false;
        traceConfiguration.IncludeStructures = false;
        traceConfiguration.IncludeContent = false;
        traceConfiguration.OutputAssetTypes = null;
        traceConfiguration.SourceTier = null;
        traceConfiguration.TargetTier = null;
        traceConfiguration.Traversability.FunctionBarriers = null;
        traceConfiguration.Traversability.Barriers = null;
        traceConfiguration.Functions = new List<Function>();
        traceConfiguration.Propagators = null;

        // Network attributes to pull during the trace
        List<string> networkattributeNames = new List<string>();
        IReadOnlyList<NetworkAttribute> networkAttributes = utilityNetworkDefinition.GetNetworkAttributes();
        foreach (NetworkAttribute networkAttribute in networkAttributes)
        {
          networkattributeNames.Add(networkAttribute.Name);
        }

        // List of additional fields to pull during the trace operation
        List<string> deviceFields = electricDistributionDeviceDefinition.GetFields().Select(f => f.Name).ToList();

        // Step 1: Set result type as Feature
        List<ResultType> resultTypeList = new List<ResultType>() { ResultType.Feature };

        // Step 2 : Set network attributes and additional fields to be returned during trace in result options
        ResultOptions resultOptions = new ResultOptions()
        {
          IncludeGeometry = true,
          NetworkAttributes = networkattributeNames,
          ResultFields = new Dictionary<NetworkSource, List<string>>() { { deviceNetworkSource, deviceFields } }
        };

        // Step 3
        TraceArgument traceArgument = new TraceArgument(startingPoints)
        {
          Barriers = barriers,
          Configuration = traceConfiguration,
          ResultTypes = resultTypeList,
          ResultOptions = resultOptions
        };

        return Task.FromResult(traceArgument);
      }
    }

    public static NetworkSource GetNetworkSource(UtilityNetworkDefinition unDefinition, string name)
    {
      IReadOnlyList<NetworkSource> allSources = unDefinition.GetNetworkSources();
      foreach (NetworkSource source in allSources)
      {
        if (name.Contains("Partitioned Sink"))
        {
          if (source.Name.Replace(" ", "").ToUpper().Contains(name.Replace(" ", "").ToUpper()) ||
              source.Name.Replace(" ", "").ToUpper()
                .Contains(name.Replace("Partitioned Sink", "Part_Sink").Replace(" ", "").ToUpper()))
          {
            return source;
          }
        }

        if (name.Contains("Hierarchical Sink"))
        {
          if (source.Name.Replace(" ", "").ToUpper().Contains(name.Replace(" ", "").ToUpper()) ||
              source.Name.Replace(" ", "").ToUpper()
                .Contains(name.Replace("Hierarchical Sink", "Hier_Sink").Replace(" ", "").ToUpper()))
          {
            return source;
          }
        }

        if (source.Name.Replace(" ", "").ToUpper().Contains(name.Replace(" ", "").ToUpper()))
        {
          return source;
        }
      }

      return null;
    }
  }
}
