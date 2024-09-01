using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FPS_personal_project
{
    /// <summary>
    /// Runtime data structure to hold player informationr which must survive events like player death/disconnect
    /// </summary>
    
    public struct PlayerData : INetworkStruct
    {
        [Networked,Capacity(24)]
        public string Nickname { get => default; set { } }
        public PlayerRef PlayerRef;
        public int Kills;
        public int Deaths;
        public int Suicides;
        public int LastKillTick;
        public int StatisticPosition;
        public bool IsAlive;
        public bool IsConnected;
    }

    public enum EGamePlayState
    {
        Skirmish = 0,
        Running = 1,
        Finished = 2
    }

    /// <summary>
	/// Drives gameplay logic - state, timing, handles player connect/disconnect/spawn/despawn/death, calculates statistics.
	/// </summary>
    public class GamePlay : NetworkBehaviour
    {
        public GameUI GameUI;
        public Player PlayerPrefab;
        public float GameDuration = 180f;
        public float PlayerRespawnTime = 5f;
        public bool AutoSpawnPointNum = true;
        public int SpawnPointNum = 20;
        [Networked][Capacity(32)]
        public int ActivePlayers { get; set; }
        //public float DoubleDamageDuration = 30f;

        [Networked][Capacity(32)][HideInInspector]
        public NetworkDictionary<PlayerRef, PlayerData> AllPlayerData { get; }
        [Networked][HideInInspector]
        public TickTimer RemainingTime { get; set; }
        [Networked][HideInInspector]
        public EGamePlayState State { get; set; }

        //public bool DoubleDamageActive => State == EGameplayState.Running && RemainingTime.RemainingTime(Runner).GetValueOrDefault() < DoubleDamageDuration;

        private bool _isNicknameSent;
        private float _runningStateTime;
        private List<Player> _spawnedPlayers = new(16);
        private List<PlayerRef> _pendingPlayers = new(16);
        private List<PlayerData> _tempPlayerData = new(16);
        private List<Transform> _recentSpawnPoints = new(20);

        public void Awake()
        {
            SpawnPointNum = AutoSpawnPointNum ? GameObject.FindGameObjectsWithTag("SpawnPoint").Length : SpawnPointNum;
            _recentSpawnPoints = new(SpawnPointNum);
            
           
        }
        public void PlayerKilled(PlayerRef killerPlayerRef, PlayerRef victimePlayerRef, EWeaponType weaponType, bool isCriticalKill)
        {
            if (HasStateAuthority == false)
                return;

            //update statstics of the killer player
            if (AllPlayerData.TryGet(killerPlayerRef, out PlayerData killerData))
            {
                if(killerPlayerRef != victimePlayerRef)
                {
                    killerData.Kills++;
                    killerData.LastKillTick = Runner.Tick;
                }
                else
                {
                    killerData.Suicides++;
                }
                AllPlayerData.Set(killerPlayerRef, killerData);

            }

            //update statistics of the victim player
            var victimData = AllPlayerData.Get(victimePlayerRef);
            victimData.Deaths++;
            victimData.IsAlive = false;
            AllPlayerData.Set(victimePlayerRef, victimData);

            //inform all clinets about the kill
            RPC_PlayerKilled(killerPlayerRef, victimePlayerRef, weaponType, isCriticalKill);

            StartCoroutine(RespawnPlayer(victimePlayerRef, PlayerRespawnTime));

            RecalculateStatisticPositions();


        }

        public override void Spawned()
        {
            if (GameUI == null)
            {
                Debug.LogError("No game ui provided. finding it in scene");
                GameUI = FindAnyObjectByType<GameUI>();
            }
               
            
            if(Runner.Mode == SimulationModes.Server||Runner.Mode == SimulationModes.Host)
            {
                Application.targetFrameRate = TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Server;
                ActivePlayers = 0;
            }

            if(Runner.GameMode == GameMode.Shared)
            {
                throw new System.NotSupportedException("doesn't support shared.");
            }

        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority == false)//game logic is server auth.
                return;

           
            // PlayerManager is a special helper class which iterates over list of active players (NetworkRunner.ActivePlayers) and call spawn/despawn callbacks on demand.
            PlayerManager.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);

            if (State == EGamePlayState.Skirmish && ActivePlayers >1)
            {
                StartGamePlay();
            }
            
            if(State == EGamePlayState.Running)
            {
                _runningStateTime += Runner.DeltaTime;

                /*var sessionInfo = Runner.SessionInfo;

                // Hide the match after 60 seconds. Players won't be able to randomly connect to existing game and start new one instead.
                // Joining via party code should work.
                if (sessionInfo.IsVisible && (_runningStateTime > 60f || sessionInfo.PlayerCount >= sessionInfo.MaxPlayers))
                {
                    sessionInfo.IsVisible = false;
                }*/

                if (RemainingTime.Expired(Runner))
                {
                    StopGamePlay();
                }

            }

        }

        public override void Render()
        {
            
            //no need for server to run this part.
            if (Runner.Mode == SimulationModes.Server)
                return;

            //every client must send its nickname to server when the game is started
            if(_isNicknameSent == false)
            {
                /*if(PlayerPrefs.GetString("Menu.UserName") != null)
                    RPC_SetPlayerNickname(Runner.LocalPlayer, PlayerPrefs.GetString("Menu.UserName"));//TODO:store data in menu.
                else*/
                    RPC_SetPlayerNickname( Runner.LocalPlayer, "Player" + AllPlayerData.Count.ToString());
                _isNicknameSent = true;
            }

        }

        private void SpawnPlayer(PlayerRef playerRef)
        {
            if (AllPlayerData.TryGet(playerRef, out var playerData) == false)
            {
                playerData = new PlayerData();
                playerData.PlayerRef = playerRef;
                playerData.Nickname = playerRef.ToString();
                playerData.StatisticPosition = int.MaxValue;
                playerData.IsAlive = false;
                playerData.IsConnected = false;
            }

            if (playerData.IsConnected)
                return;

            Debug.LogWarning($"{playerRef} connected");

            playerData.IsConnected = true;
            playerData.IsAlive = true;

            AllPlayerData.Set(playerRef, playerData);
            ActivePlayers++;
            var spawnPoint = GetSpawnPoint();
            var player = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);

            //set playerinstance as playerobject to easily get it from other locations
            Runner.SetPlayerObject(playerRef, player.Object);

            RecalculateStatisticPositions();
        }

        private void DespawnPlayer(PlayerRef playerRef,Player player)
        {
            if (AllPlayerData.TryGet(playerRef, out var playerData)) 
            {
                if(playerData.IsConnected)
                {
                    Debug.LogWarning($"{playerRef} Disconnected.");
                }
                playerData.IsConnected = false;
                playerData.IsAlive = false;
                AllPlayerData.Set(playerRef, playerData);
            }
            ActivePlayers--;
            Runner.Despawn(player.Object);

            RecalculateStatisticPositions();
        }

        private IEnumerator RespawnPlayer(PlayerRef playerRef, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            if (Runner == null)
                yield break;

            //Despawn old player object if it exists
            var playerObject = Runner.GetPlayerObject(playerRef);
            if (playerObject != null)
            {
                Runner.Despawn(playerObject);
            }

            // Don't spawn the player for disconnected clients.
            if (AllPlayerData.TryGet(playerRef, out PlayerData playerData) == false || playerData.IsConnected == false)
                yield break;

            // Update player data.
            playerData.IsAlive = true;
            AllPlayerData.Set(playerRef, playerData);

            //respawn player
            var spawnPoint = GetSpawnPoint();
            var player = Runner.Spawn(PlayerPrefab, spawnPoint.position, spawnPoint.rotation, playerRef);

            // Set player instance as PlayerObject to easily get it from other locations.
            Runner.SetPlayerObject(playerRef, player.Object);
        }

        private Transform GetSpawnPoint()
        {
            Transform spawnPoint = default;

            //Iterate over all spawn points in scene
            var spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);
            for (int i = 0, offset = Random.Range(0, spawnPoints.Length); i < spawnPoints.Length; i++)
            {
                spawnPoint = spawnPoints[(offset + i) % spawnPoints.Length].transform;

                if (_recentSpawnPoints.Contains(spawnPoint) == false)
                    break;
            }

            // Add spawn point to list of recently used spawn points.
            _recentSpawnPoints.Add(spawnPoint);

            // Ignore only last 3 spawn points.
            if (_recentSpawnPoints.Count > 3)
            {
                _recentSpawnPoints.RemoveAt(0);
            }

            return spawnPoint;
        }

        private void StartGamePlay()
        {
            //stop all respawn coroutines(players can kill each other during skirmish)
            StopAllCoroutines();
            
            State = EGamePlayState.Running;
            RemainingTime = TickTimer.CreateFromSeconds(Runner, GameDuration);

            // Reset player data after skirmish and respawn players.
            foreach (var playerPair in AllPlayerData)
            {
                var data = playerPair.Value;

                data.Kills = 0;
                data.Deaths = 0;
                data.StatisticPosition = int.MaxValue;
                data.IsAlive = false;

                AllPlayerData.Set(data.PlayerRef, data);

                StartCoroutine(RespawnPlayer(data.PlayerRef, 0f));
            }

        }

        private void StopGamePlay()
        {
            RecalculateStatisticPositions();

            State = EGamePlayState.Finished;
        }

        private void RecalculateStatisticPositions()
        {
            if (State == EGamePlayState.Finished)
                return;

            _tempPlayerData.Clear();

            foreach(var pair in AllPlayerData)
            {
                _tempPlayerData.Add(pair.Value);
            }

            _tempPlayerData.Sort((a, b) =>
            {
                if (a.Kills != b.Kills)
                    return b.Kills.CompareTo(a.Kills);//if kills are different - sort player with more kills to front.

                return a.LastKillTick.CompareTo(b.LastKillTick);//if kills are same - player who killed first gets to the front.
            });

            for (int i = 0; i < _tempPlayerData.Count; i++)
            {
                var playerData = _tempPlayerData[i];
                playerData.StatisticPosition = playerData.Kills > 0 ? i + 1 : int.MaxValue;

                AllPlayerData.Set(playerData.PlayerRef, playerData);
            }

        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_PlayerKilled(PlayerRef killerPlayerRef, PlayerRef victimPlayerRef, EWeaponType weaponType, bool isCriticalKill)
        {
            string killerNickname = "";
            string victimNickname = "";

            if (AllPlayerData.TryGet(killerPlayerRef, out PlayerData killerData))
            {
                killerNickname = killerData.Nickname;
            }

            if (AllPlayerData.TryGet(victimPlayerRef, out PlayerData victimData))
            {
                victimNickname = victimData.Nickname;
            }
            Debug.LogWarning($"killer name :{killerNickname} \n vicitim name: {victimNickname} \n weaponType:{weaponType == null}\n critical {isCriticalKill == null}");
            GameUI.GameplayView.KillFeed.ShowKill(killerNickname, victimNickname, weaponType, isCriticalKill);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_SetPlayerNickname(PlayerRef playerRef, string nickname)
        {
            var playerData = AllPlayerData.Get(playerRef);
            playerData.Nickname = nickname;
            AllPlayerData.Set(playerRef, playerData);
        }

    }

}
