using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIMatchmakingScreen : MonoBehaviour
{
    public TextMeshProUGUI MatchmakingTimeText;
    public TextMeshProUGUI MatchmakingStatusText;
    private float _time;
    private void Start()
    {
        MatchmakingTimeText.text = "0";
        MatchmakingStatusText.text = "Waiting...";
        _time = 0f;
    }

    private void Update()
    {
        if (MatchmakerClient.IsMatchmaking)
        {
            _time += Time.deltaTime;
            Debug.Log(_time);
            MatchmakingTimeText.text = ((int)_time).ToString();
            MatchmakingStatusText.text = MatchmakerClient.MatchmakingStatus;
        }
            
        else
        {
            _time = 0f;
        }

       
    }
}
