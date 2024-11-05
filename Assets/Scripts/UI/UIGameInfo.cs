using FPS_personal_project;
using Fusion;
using TMPro;
using UnityEngine;

public class UIGameInfo : MonoBehaviour
{
    public TextMeshProUGUI FpsText;
    public TextMeshProUGUI RttText;
    public TextMeshProUGUI RegionText;

    public GamePlay gamePlay;

    private NetworkRunner _runner;

    private void OnEnable()
    {
        _runner = gamePlay.Runner;
    }
    private void Update()
    {
        FpsText.text = ((int)(1f / Time.unscaledDeltaTime)).ToString();
        RttText.text = ((int)_runner.GetPlayerRtt(PlayerRef.None)).ToString();
        RegionText.text = _runner.SessionInfo.Region.ToString();
    }
}
