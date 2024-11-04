using Fusion.Statistics;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using Unity.VisualScripting;
using UnityEngine;

public class MultiplayServerHostingHandler : MonoBehaviour
{
    public static MultiplayServerHostingHandler Instance = null;

    private string _externalServerIP = "0.0.0.0";
    private string _externalConnectionString => $"{_externalServerIP}:{NetworkRunnerHandler.GetServerPortFromStartupArgs().ToString()}";

    IServerQueryHandler serverQueryHandler;
    private IMultiplayService _multiplayService;
    const int _multiplayServiceTimeout = 20000;//20000ms = 20s

    private string _allocationId;
    private MultiplayEventCallbacks _serverCallbacks;
    private IServerEvents _serverEvents;

    private BackfillTicket _localBackfillTicket;
    private CreateBackfillTicketOptions _createBackfillTicketOptions;
    private const int _ticketCheckMs = 1000;

    private MatchmakingResults _matchmakingPayload;

    private bool _backfilling = false;
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
    }


    async void Start()
    {
#if UNITY_SERVER 
        _externalServerIP = NetworkRunnerHandler.GetServerIpFromStartupArgs().ToString();
        await InitUnityServices();

        
#endif
    }

#if UNITY_SERVER
    private void Update()
    {
        if (serverQueryHandler != null)
            serverQueryHandler.UpdateServerCheck();
    }
#endif

    public void SetCurrentNumberOfPlayers(ushort numberOfPlayers)
    {
        Debug.LogWarning(serverQueryHandler == null);
        if(serverQueryHandler != null)
            serverQueryHandler.CurrentPlayers = numberOfPlayers;
    }


    async Task InitUnityServices()
    {
        try
        {
            _multiplayService = MultiplayService.Instance;
            await UnityServices.InitializeAsync();
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            return;
        }
        try
        {
            serverQueryHandler = 
                await _multiplayService.StartServerQueryHandlerAsync(9, "test", "test", "testBuild", "testMap");
        }
        catch(Exception ex)
        {
            Debug.LogException(ex);
        }

        try
        {
            _matchmakingPayload = await GetMatchmakerPayload(_multiplayServiceTimeout);
            if(_matchmakingPayload != null)
            {
                Debug.Log($"Got payload {_matchmakingPayload}");
                await StartBackfill(_matchmakingPayload);
            }
            else
            {
                Debug.LogWarning("Getting the mmPayload timed out. starting with defaults.");
            }
        }
        catch(Exception e)
        {
            Debug.LogException(e);
            return;
        }

        Debug.LogWarning("services initialized without error");
    }

    private async Task<MatchmakingResults>GetMatchmakerPayload(int timeout)
    {
        var matchmakerPayloadTask = SubscribeAndAwaitMatchmakerAllocation();
        if(await Task.WhenAny(matchmakerPayloadTask, Task.Delay(timeout)) == matchmakerPayloadTask)
        {
            return matchmakerPayloadTask.Result;
        }

        return null;
    }

    private async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if(_multiplayService == null) { return null; }

        _allocationId = null;
        _serverCallbacks = new MultiplayEventCallbacks();//subscribing
        _serverCallbacks.Allocate += OnMultiplayAllocation;
       _serverEvents =  await _multiplayService.SubscribeToServerEventsAsync(_serverCallbacks);

        _allocationId = await AwaitAllocationId();
        var MatchmakerPayload = await GetMatchmakerAllocationPayloadAsync();
        return MatchmakerPayload;

        
    }

    private void Dispose()
    {
        _serverCallbacks.Allocate -= OnMultiplayAllocation;
        _serverEvents?.UnsubscribeAsync();
    }

    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        Debug.Log($"On Allocation: {allocation.AllocationId}");
        if (string.IsNullOrEmpty(allocation.AllocationId)) return;
        _allocationId = allocation.AllocationId;

        
    }

    private async Task<string> AwaitAllocationId()
    {
        var config = _multiplayService.ServerConfig;
        Debug.Log($"Awaiting Allocation. Server Config is: \n" +
            $"-ServerID:{config.ServerId}\n" +
            $"-AllocationID:{config.AllocationId}\n" +
            $"-Port:{config.Port}\n" +
            $"-Qport:{config.QueryPort}\n" +
            $"-logs:{config.ServerLogDirectory}");
        while(string.IsNullOrEmpty(_allocationId))
        {
            var configId = config.AllocationId;
            if(!string.IsNullOrEmpty(configId) && string.IsNullOrEmpty(_allocationId))
            {
                _allocationId = configId;
                break;
            }

            await Task.Delay(100);

        }
        return _allocationId;

    }

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        try
        {
            var payloadAllocation = 
                await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
            var modelAsJson = JsonConvert.SerializeObject(payloadAllocation, Formatting.Indented);
            Debug.Log($"{nameof(GetMatchmakerAllocationPayloadAsync)}\n {modelAsJson}");
            return payloadAllocation;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        return null;
    }

    private async Task StartBackfill(MatchmakingResults payload)
    {
        var backfillProperties = new BackfillTicketProperties(payload.MatchProperties);
        _localBackfillTicket = 
            new BackfillTicket { Id = payload.MatchProperties.BackfillTicketId, Properties = backfillProperties };
        await BeginBackfilling(payload);
    }

    private async Task BeginBackfilling(MatchmakingResults payload)
    {
        
        
        if (string.IsNullOrEmpty(_localBackfillTicket.Id))
        {
            var matchProperties = payload.MatchProperties;
            _createBackfillTicketOptions = new Unity.Services.Matchmaker.CreateBackfillTicketOptions
            {
                Connection = _externalConnectionString,
                QueueName = payload.QueueName,
                Properties = new BackfillTicketProperties(matchProperties)
            };

            _localBackfillTicket.Id =
                await MatchmakerService.Instance.CreateBackfillTicketAsync(_createBackfillTicketOptions);
        }
        _backfilling = true;

#pragma warning disable 4014
        BackfillLoop();
#pragma warning restore 4014
    }

    private async Task BackfillLoop()
    {
        while(_backfilling && NeedsPlayers())
        {
            _localBackfillTicket =
                await MatchmakerService.Instance.ApproveBackfillTicketAsync(_localBackfillTicket.Id);
            if(!NeedsPlayers())
            {
                await MatchmakerService.Instance.DeleteBackfillTicketAsync(_localBackfillTicket.Id);
                _localBackfillTicket.Id = null;
                
                return;
            }
            await Task.Delay(_ticketCheckMs);
        }
        _backfilling = false;
    }

    public void ClientDisconnected()
    {
        if(!_backfilling && serverQueryHandler.CurrentPlayers>0&&NeedsPlayers())
        {
            BeginBackfilling(_matchmakingPayload);
        }
    }

    private bool NeedsPlayers()
    {
        return serverQueryHandler.CurrentPlayers < serverQueryHandler.MaxPlayers;
    }
}
