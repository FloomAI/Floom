using Floom.Pipeline;
using Floom.Pipeline.Entities;
using Floom.Plugin;
using Floom.Repository;

namespace Floom.Events;

/*
 *  Events Manager (In Memory Cache for Events Subscriptions)
 *      Maintains a record of all available plugins and their event subscriptions.
 *      Updates on Floom start and on pipeline commit
 *
 *
 * For any given event, the Events Registry will return a list of EventSubscriber that are subscribed to that event with their configurations
 * For example, for event onPipelineCommit,
 * it will return list of plugins and their pipelines
 */

public enum EventSubscriberType
{
    Plugin
}

public class EventSubscriber
{
    public string EntityId { get; set; } // Unique identifier for the entity
    public EventSubscriberType EntityType { get; set; } // Type of the entity (plugin, system plugin, etc.)
    public string PipelineName { get; set; } // Associated pipeline name, if applicable
}

public class EventsManager
{
    private readonly IRepository<PipelineEntity> _pipelinesRepository;
    private readonly IRepository<PluginManifestEntity> _pluginsRepository;
    private readonly IPluginLoader _pluginLoader;
    private readonly IPluginContextCreator _pluginContextCreator;
    
    public EventsManager(IRepositoryFactory repositoryFactory, IPluginLoader pluginLoader, IPluginContextCreator pluginContextCreator)
    {
        _pipelinesRepository = repositoryFactory.Create<PipelineEntity>("pipelines");
        _pluginsRepository = repositoryFactory.Create<PluginManifestEntity>("plugins");
        _pluginLoader = pluginLoader;
        _pluginContextCreator = pluginContextCreator;
    }
    
    // Does it need to change to Event Context instead of Pipeline Context?
    private static Dictionary<string, List<EventSubscriber>> EventsSubscribersMap = new();
    private readonly SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);
    
    private async Task UpdateEventSubscribersMapForPipeline(PipelineEntity pipeline)
    {
        var pipelinePlugins = PipelineHelper.GetPipelinePluginsConfigurations(pipeline);

        foreach (var pluginConfiguration in pipelinePlugins)
        {
            var pluginManifest = await _pluginsRepository.Get(pluginConfiguration.Package, "package");
            if (pluginManifest == null) continue;
            foreach (var eventName in pluginManifest.events)
            {
                if (!EventsSubscribersMap.ContainsKey(eventName))
                {
                    EventsSubscribersMap[eventName] = new List<EventSubscriber>();
                }
            
                var subscriber = new EventSubscriber
                {
                    EntityId = pluginConfiguration.Package,
                    EntityType = EventSubscriberType.Plugin,
                    PipelineName = pipeline.name
                };
            
                EventsSubscribersMap[eventName].Add(subscriber);
            }
        }
    }

    
    // Load all plugins from pipelines collection
    // for each plugin, get its events from plugin manifest in database
    // and add it to PluginsEventsMap
    public async void OnFloomStarts()
    {
        await _updateSemaphore.WaitAsync();

        try
        {
            EventsSubscribersMap = new Dictionary<string, List<EventSubscriber>>();

            var pipelines = await _pipelinesRepository.GetAll();
            
            foreach (var pipeline in pipelines)
            {
                await UpdateEventSubscribersMapForPipeline(pipeline);
            }
        }
        finally
        {
            _updateSemaphore.Release();
        }
        
        ProcessEvent(Event.OnFloomStart);
    }
    
    public async Task OnPipelineCommit(PipelineEntity updatedPipeline)
    {
        await _updateSemaphore.WaitAsync();

        try
        {
            // Remove existing entries for this pipeline from EventsSubscribersMap
            foreach (var entry in EventsSubscribersMap)
            {
                entry.Value.RemoveAll(e => e.PipelineName == updatedPipeline.name);
            }

            await UpdateEventSubscribersMapForPipeline(updatedPipeline);
        }
        finally
        {
            _updateSemaphore.Release();
        }
        
        ProcessEvent(Event.OnPipelineCommit);
    }
    
    private async Task<List<EventSubscriber>> GetEventSubscribers(string eventName)
    {
        await _updateSemaphore.WaitAsync();

        try
        {
            // Check if the event exists in the map
            return EventsSubscribersMap.TryGetValue(eventName, out var subscribersForEvent) ?
                // Return a new list to prevent external modifications to the original list
                new List<EventSubscriber>(subscribersForEvent) :
                // Return an empty list if the event is not found
                new List<EventSubscriber>();
        }
        finally
        {
            _updateSemaphore.Release();
        }
    }

    private async Task ProcessEvent(string eventName)
    {
        var subscribers = GetEventSubscribers(eventName).Result;

        foreach (var eventSubscriber in subscribers)
        {
            switch (eventSubscriber.EntityType)
            {
                case EventSubscriberType.Plugin:
                    var pipeline = await _pipelinesRepository.Get(eventSubscriber.PipelineName, "name");
                    var pluginPackage = eventSubscriber.EntityId;
                    var pluginConfiguration = PipelineHelper.GetPluginConfigurationInPipeline(pluginPackage, pipeline);
                    var pluginContext = await _pluginContextCreator.Create(pluginConfiguration);
                    var plugin = _pluginLoader.LoadPlugin(pluginPackage);
                    
                    var pipelineContext = new PipelineContext()
                    {
                        PipelineName = pipeline.name,
                        Status = PipelineExecutionStatus.Events,
                        Pipeline = pipeline.ToModel()
                    };
                    plugin?.HandleEvent(eventName, pluginContext, pipelineContext);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}