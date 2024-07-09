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
#endif

            InitializeNetworkRunner(networkRunner, gameMode, "TestSession", NetAddress.Any(), SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex));
            Debug.Log("ServerNetworkStarted");


        }


    }


    protected virtual Task InitializeNetworkRunner(NetworkRunner runner, GameMode mode, string sessionName,
        NetAddress address, SceneRef scene /*Action<NetworkRunner> initialized*/)
    {
        var sceneManager = GetSceneManager(runner);

        runner.ProvideInput = true;

        Debug.Log($"InitializeNetworkRunner done");

        return runner.StartGame(new StartGameArgs
        {
            GameMode = mode,
            Address = address,
            SessionName = sessionName,
            //Initialized = initialized,
            SceneManager = sceneManager
           
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


}
