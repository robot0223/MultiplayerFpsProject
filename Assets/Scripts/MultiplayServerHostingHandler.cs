using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;

public class MultiplayServerHostingHandler : MonoBehaviour
{
    public static MultiplayServerHostingHandler Instance = null;

    IServerQueryHandler serverQueryHandler;


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
        await InitUnityServices();

        serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(5, "test", "test", "testBuild", "testMap");
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
        serverQueryHandler.CurrentPlayers = numberOfPlayers;
    }


    async Task InitUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch(UnityException e)
        {
            Debug.LogException(e);
            return;
        }

        Debug.LogWarning("services initialized without error");
    }



}
