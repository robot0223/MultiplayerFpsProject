using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System;
using UnityEngine.Diagnostics;
using UnityEditor;
using System.Linq;
using Unity.VisualScripting;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine.PlayerLoop;
using Fusion.Photon.Realtime;

public class NetworkRunnerHandler : MonoBehaviour
{
    [SerializeField]
    NetworkRunner networkRunnerPrefab;

    NetworkRunner networkRunner;

    private void Awake()
    {
        NetworkRunner networkRunnerInScene = FindObjectOfType<NetworkRunner>();

        if (networkRunnerInScene != null)
            networkRunner = networkRunnerInScene;
    }

    private void Start()
    {
        if (networkRunner == null)
        {
            networkRunner = Instantiate(networkRunnerPrefab);
            networkRunner.name = "Network Runner";

            GameMode gameMode = GameMode.Client;


#if UNITY_EDITOR
            // gameMode = GameMode.AutoHostOrClient;
            //gameMode = GameMode.Server;
#elif DEVELOPMENT_BUILD
            //gameMode = GameMode.AutoHostOrClient;
#elif UNITY_SERVER
            gameMode = GameMode.Server;
#endif
            StartCoroutine(StartNetwork(gameMode));

            /*InitializeNetworkRunner(networkRunner, gameMode,*//* "TestSession",*//* NetAddress.Any(), 
                SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),(networkRunner) => { });*/
            

        }


    }

    public void ShutdownAll()
    {
        foreach (var runner in NetworkRunner.Instances.ToList())
        {
            if (runner != null && runner.IsRunning)
            {
                runner.Shutdown();
            }
        }

        SceneManager.LoadSceneAsync("Level_Menu_Background");
        // Destroy our DontDestroyOnLoad objects to finish the reset
        Destroy(networkRunner.gameObject);
        Destroy(gameObject);
    }

    protected virtual Task InitializeNetworkRunner(NetworkRunner runner, GameMode mode, /*string sessionName,*/
        NetAddress address, SceneRef scene ,Action<NetworkRunner> onGameStarted, INetworkRunnerUpdater updater = null)
    {
        var sceneManager = runner.GetComponent<INetworkSceneManager>();
        if (sceneManager == null)
        {
            Debug.Log($"NetworkRunner does not have any component implementing {nameof(INetworkSceneManager)} interface, adding {nameof(NetworkSceneManagerDefault)}.", runner);
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        var objectProvider = runner.GetComponent<INetworkObjectProvider>();
        if (objectProvider == null)
        {
            Debug.Log($"NetworkRunner does not have any component implementing {nameof(INetworkObjectProvider)} interface, adding {nameof(NetworkObjectProviderDefault)}.", runner);
            objectProvider = runner.gameObject.AddComponent<NetworkObjectProviderDefault>();
        }

        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        //doesn't hurt for server to have input listners so not gonna do #if !unity server for now.
        runner.ProvideInput = true;

        FusionAppSettings appSettings = PhotonAppSettings.Global.AppSettings;

        string fixedRegion = GetRegionFromStartupArgs();
        if (fixedRegion != "")
            appSettings.FixedRegion = fixedRegion;

        int port = GetServerPortFromStartupArgs();
        if(port!=0)
        {
            appSettings.Port = port;
        }
        
            
        

        Debug.LogWarning($"InitializeNetworkRunner with port {port} and region {fixedRegion} done");

        return runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            Address = address,
            Scene = sceneInfo,
            SessionName = null,//null for now. and just in case:TODO
            OnGameStarted = onGameStarted,
            SceneManager = sceneManager,
            Updater = updater,
            ObjectProvider = objectProvider,
            CustomPhotonAppSettings = appSettings

        }) ;
    }

    INetworkSceneManager GetSceneManager(NetworkRunner runner)
    {
        var sceneManager = runner.GetComponents(typeof(MonoBehaviour)).OfType<INetworkSceneManager>().FirstOrDefault();

        if(sceneManager == null)
        {
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        return sceneManager;
    }


    protected IEnumerator StartNetwork(GameMode mode)
    {
        if(!networkRunner)
        {
            Debug.LogError($"{nameof(networkRunner)} not set.");
            yield break;
        }

        if (gameObject.transform.parent)
        {
            Debug.LogWarning($"{nameof(NetworkRunnerHandler)} can't be a child game object, un-parenting.");
            gameObject.transform.parent = null;
        }

#if UNITY_EDITOR
        // var task = InitializeNetworkRunner(networkRunner, mode, NetAddress.Any(), 
        //      SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), (runner) => { });
        var task = InitializeNetworkRunner(networkRunner, mode, NetAddress.Any(),
              SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), null);
#elif UNITY_SERVER
    var task = InitializeNetworkRunner(networkRunner, mode, NetAddress.Any(), 
            SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), (runner) => { });
#else
var task = InitializeNetworkRunner(networkRunner, mode, NetAddress.Any(), 
            SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), null);
#endif
        while (task.IsCompleted == false)
        {
            yield return new WaitForSeconds(1f);
        }

        if (task.IsFaulted)
        {
            Log.Debug($"Unable to start server: {task.Exception}");

            ShutdownAll();
            yield break;
        }
    }

    public string GetRegionFromStartupArgs()
    {
        if (System.Environment.CommandLine.Contains("-region eu"))
            return "eu";

        else if (System.Environment.CommandLine.Contains("-region kr"))
            return "kr";

        else if (System.Environment.CommandLine.Contains("-region cn"))
            return "cn";

        else if (System.Environment.CommandLine.Contains("-region jp"))
            return "jp";

        else if (System.Environment.CommandLine.Contains("-region asia"))
            return "asia";

        else if (System.Environment.CommandLine.Contains("-region sa"))
            return "sa";

        else if (System.Environment.CommandLine.Contains("-region us"))
            return "us";

        else if (System.Environment.CommandLine.Contains("-region usw"))
            return "usw";

        Debug.Log("no region provided defaulting to asia");
        return "asia";
    }

    public  int GetServerPortFromStartupArgs()
    {
        int port = 0;
        string[] commandLineArgs = System.Environment.GetCommandLineArgs();
        for(int i = 0; i < commandLineArgs.Length; i++)
        {
            if (commandLineArgs[i].Contains("-port"))
            {
                int.TryParse(commandLineArgs[i+1], out port);
                Debug.LogWarning($"found port{commandLineArgs[i]}and port should be {commandLineArgs[i + 1]}");
                return port;
            }
        }
        return port;
    }


}
