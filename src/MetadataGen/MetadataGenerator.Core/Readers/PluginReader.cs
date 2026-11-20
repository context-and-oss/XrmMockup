using DG.Tools.XrmMockup;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using XrmMockup.MetadataGenerator.Core.Connection;

namespace XrmMockup.MetadataGenerator.Core.Readers;

/// <summary>
/// Reads plugin registrations from Dataverse.
/// </summary>
internal sealed class PluginReader(
    IOrganizationServiceProvider serviceProvider,
    ILogger<PluginReader> logger) : IPluginReader
{
    private readonly IOrganizationService _service = serviceProvider.Service;

    public async Task<List<MetaPlugin>> GetPluginsAsync(string[] solutions, CancellationToken ct = default)
    {
        if (solutions.Length == 0)
        {
            logger.LogInformation("No solutions specified, skipping plugin extraction");
            return [];
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Getting plugins for solutions: {Solutions}", string.Join(", ", solutions));
        }

        return await Task.Run(() =>
        {
            var plugins = new List<MetaPlugin>();
            var pluginSteps = GetPluginSteps(solutions);
            var images = GetImages(solutions);

            foreach (var pluginStep in pluginSteps.Entities)
            {
                var metaPlugin = new MetaPlugin
                {
                    Name = pluginStep.GetAttributeValue<string>("name"),
                    Rank = pluginStep.GetAttributeValue<int>("rank"),
                    FilteredAttributes = pluginStep.GetAttributeValue<string>("filteringattributes"),
                    Mode = pluginStep.GetAttributeValue<OptionSetValue>("mode").Value,
                    Stage = pluginStep.GetAttributeValue<OptionSetValue>("stage").Value,
                    MessageName = pluginStep.GetAttributeValue<EntityReference>("sdkmessageid").Name,
                    AssemblyName = pluginStep.GetAttributeValue<EntityReference>("eventhandler").Name,
                    PluginTypeAssemblyName = pluginStep.GetAttributeValue<AliasedValue>("plugintype.assemblyname").Value.ToString()!,
                    ImpersonatingUserId = pluginStep.Contains("impersonatinguserid")
                        ? pluginStep.GetAttributeValue<EntityReference>("impersonatinguserid").Id
                        : null,
                    PrimaryEntity = pluginStep.GetAttributeValue<AliasedValue>("sdkmessagefilter.primaryobjecttypecode")?.Value as string ?? "",
                    AsyncAutoDelete = pluginStep.Contains("asyncautodelete") && pluginStep.GetAttributeValue<bool>("asyncautodelete"),
                    Images = [.. images.Entities
                        .Where(x => x.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid").Id == pluginStep.Id)
                        .Select(x => new MetaImage
                        {
                            Attributes = x.GetAttributeValue<string>("attributes"),
                            EntityAlias = x.GetAttributeValue<string>("entityalias"),
                            ImageType = x.GetAttributeValue<OptionSetValue>("imagetype").Value,
                            Name = x.GetAttributeValue<string>("name")
                        })]
                };

                if (metaPlugin.PrimaryEntity == "none")
                {
                    metaPlugin.PrimaryEntity = "";
                }

                plugins.Add(metaPlugin);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Retrieved {Count} plugins", plugins.Count);
            }
            return plugins;
        }, ct);
    }

    private EntityCollection GetPluginSteps(string[] solutions)
    {
        var pluginQuery = new QueryExpression("sdkmessageprocessingstep")
        {
            ColumnSet = new ColumnSet(
                "eventhandler", "stage", "mode", "rank", "sdkmessageid",
                "filteringattributes", "name", "impersonatinguserid", "sdkmessageprocessingstepid"),
            Criteria = new FilterExpression(),
            Distinct = true
        };
        pluginQuery.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);

        var sdkMessageFilterQuery = new LinkEntity(
            "sdkmessageprocessingstep", "sdkmessagefilter",
            "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter)
        {
            Columns = new ColumnSet("primaryobjecttypecode"),
            EntityAlias = "sdkmessagefilter",
            LinkCriteria = new FilterExpression()
        };
        pluginQuery.LinkEntities.Add(sdkMessageFilterQuery);

        var pluginTypeFilterQuery = new LinkEntity(
            "sdkmessageprocessingstep", "plugintype",
            "plugintypeid", "plugintypeid", JoinOperator.LeftOuter)
        {
            Columns = new ColumnSet("assemblyname"),
            EntityAlias = "plugintype",
            LinkCriteria = new FilterExpression()
        };
        pluginQuery.LinkEntities.Add(pluginTypeFilterQuery);

        var solutionComponentQuery = new LinkEntity(
            "sdkmessageprocessingstep", "solutioncomponent",
            "sdkmessageprocessingstepid", "objectid", JoinOperator.Inner)
        {
            Columns = new ColumnSet(),
            LinkCriteria = new FilterExpression()
        };
        pluginQuery.LinkEntities.Add(solutionComponentQuery);

        var solutionQuery = new LinkEntity(
            "solutioncomponent", "solution",
            "solutionid", "solutionid", JoinOperator.Inner)
        {
            Columns = new ColumnSet(),
            LinkCriteria = new FilterExpression()
        };
        solutionQuery.LinkCriteria.AddCondition("uniquename", ConditionOperator.In, solutions);
        solutionComponentQuery.LinkEntities.Add(solutionQuery);

        return _service.RetrieveMultiple(pluginQuery);
    }

    private EntityCollection GetImages(string[] solutions)
    {
        var imagesQuery = new QueryExpression
        {
            EntityName = "sdkmessageprocessingstepimage",
            ColumnSet = new ColumnSet("attributes", "entityalias", "name", "imagetype", "sdkmessageprocessingstepid"),
            LinkEntities =
            {
                new LinkEntity(
                    "sdkmessageprocessingstepimage", "sdkmessageprocessingstep",
                    "sdkmessageprocessingstepid", "sdkmessageprocessingstepid", JoinOperator.Inner)
                {
                    LinkEntities =
                    {
                        new LinkEntity(
                            "sdkmessageprocessingstep", "solutioncomponent",
                            "sdkmessageprocessingstepid", "objectid", JoinOperator.Inner)
                        {
                            LinkEntities =
                            {
                                new LinkEntity(
                                    "solutioncomponent", "solution",
                                    "solutionid", "solutionid", JoinOperator.Inner)
                                {
                                    LinkCriteria =
                                    {
                                        Conditions =
                                        {
                                            new ConditionExpression("uniquename", ConditionOperator.In, solutions)
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        return _service.RetrieveMultiple(imagesQuery);
    }
}
