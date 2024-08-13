using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using UnityEditor.Build.Content;
using Fusion.Sockets;
using System;
using UnityEngine.Diagnostics;
using UnityEditor;
using System.Linq;
using Unity.VisualScripting;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using static Cinemachine.CinemachineTriggerAction.ActionSettings;
using UnityEngine.PlayerLoop;

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
            gameMode = GameMode.AutoHostOrClient;
#elif UNITY_SERVER
            gameMode = GameMode.Server;
#else
         //nothing for now.
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

        Debug.Log($"InitializeNetworkRunner done");

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

        });
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
     var task = InitializeNetworkRunner(networkRunner, mode, NetAddress.Any(), 
            SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), (runner) => { });
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


}
