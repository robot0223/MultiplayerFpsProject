using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using StatusOptions = Unity.Services.Matchmaker.Models.MultiplayAssignment.StatusOptions;
using UnityEngine;
using Unity.Services.Matchmaker;
using System;
using Unity.Services.Matchmaker.Models;
using UnityEngine.SceneManagement;




#if UNITY_EDITOR
using ParrelSync;
#endif

public class MatchmakerClient : MonoBehaviour
{
    private string _ticketId;
    public static bool IsMatchmaking = false;
    public static string MatchmakingStatus;
#if UNITY_EDITOR
    public static bool MatchMaked = false;
#endif  

    private void Awake()
    {
        SignIn();
    }
    private async void SignIn()
    {
        await ClientSignIn("testPlayer");//TODO:remove serviceProfileName in production
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async Task ClientSignIn(string serviceProfileName = null)
    {
        if(serviceProfileName !=null)
        {
#if UNITY_EDITOR
            serviceProfileName = $"{serviceProfileName}{GetCloneNumberSuffix()}";
#endif
            var initOptions = new InitializationOptions() ;
            initOptions.SetProfile(serviceProfileName) ;
            await UnityServices.InitializeAsync(initOptions) ;
        }
        else
        {
            await UnityServices.InitializeAsync();
        }

        Debug.Log($"Signed In Anonymously as{serviceProfileName}({PlayerID()})");   
    }

    private string PlayerID()
    {
       return AuthenticationService.Instance.PlayerId ;
    }

#if UNITY_EDITOR
    private string GetCloneNumberSuffix()
    {
        {
            string projectPath = ClonesManager.GetCurrentProjectPath();
            int lastUnderscore = projectPath.LastIndexOf('_');
            string projectCloneSuffix = projectPath.Substring(lastUnderscore + 1);
            if (projectCloneSuffix.Length != 1)
                projectCloneSuffix = "";
            return projectCloneSuffix;
        }
    }
#endif

    public void StartClient()
    {
        if(!IsMatchmaking)
            CreateATicket();
    }

    private async void CreateATicket()
    {
        var options = new CreateTicketOptions("DeathMatch") ;

        var players = new List<Player>
        {
            new Player(
                PlayerID(),
                new MatchmakingPlayerData
                {
                    Skill = 100//just here as a reminder.
                }
                )
        };

        var ticketResponse = await MatchmakerService.Instance.CreateTicketAsync(players, options) ;
        _ticketId = ticketResponse.Id;
        Debug.Log($"ticket Id:{_ticketId}");
        IsMatchmaking = true;
        PollTicketStatus();

    }

    private async void PollTicketStatus()
    {
        MultiplayAssignment multiplayAssignment = null;
        bool gotAssignment = false;
        do
        {
            await Task.Delay(TimeSpan.FromSeconds(1f));
            var ticketStatus = await MatchmakerService.Instance.GetTicketAsync(_ticketId);
            if (ticketStatus == null) continue;
            if (ticketStatus.Type == typeof(MultiplayAssignment))
            {
                multiplayAssignment = ticketStatus.Value as MultiplayAssignment;
            }

            switch (multiplayAssignment.Status)
            {
                case StatusOptions.Found:
                    gotAssignment = true;
                    MatchmakingStatus = "MatchFound";
                    TicketAssigned(multiplayAssignment);
                    break;
                case StatusOptions.InProgress:
                    MatchmakingStatus = "Matchmaking....";
                    IsMatchmaking = true;
                    break;
                case StatusOptions.Failed:
                    gotAssignment = true;
                    IsMatchmaking = false;
                    MatchmakingStatus = "Matchmaking Failed";
                    Debug.LogError($"Failed to get ticket status. Error: {multiplayAssignment.Message}");
                    break;
                case StatusOptions.Timeout:
                    gotAssignment = true;
                    MatchmakingStatus = "Matchmaking Timed out";
                    IsMatchmaking = false;
                    Debug.LogError("Failed to get ticket stauts. Timed out");
                    break;
                default:
                    throw new InvalidOperationException();


            }
        } while (!gotAssignment);
    }

    private void TicketAssigned(MultiplayAssignment assignment)
    {
        Debug.Log($"Ticket Assigned ip:{assignment.Ip} port:{assignment.Port}");
        NetworkRunnerHandler.targetIp = assignment.Ip;
        NetworkRunnerHandler.targetPort = assignment.Port;

#if UNITY_EDITOR
        MatchMaked = true;
#endif
        //start logic.. for now it switches scene.
        SceneManager.LoadScene("Level_00_Main");
        
        //StartCoroutine(WaitForServerLoadAndExecute(10));

    }

    /*IEnumerator WaitForServerLoadAndExecute(int time)
    {
        Debug.Log("here front");
        yield return new WaitForSeconds(time);
        SceneManager.LoadScene("Level_00_Main");
        Debug.Log("Here,(back)");
    }*/

    [Serializable]
    public class MatchmakingPlayerData//used with rules!
    {
        public int Skill;//have no rules, but just programmed to remember how to do this.
    }
}
