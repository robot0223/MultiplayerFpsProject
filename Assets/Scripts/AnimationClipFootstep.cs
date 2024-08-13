using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationClipFootstep : MonoBehaviour
{
    void OnCharEvent(AnimationEvent e)
    {
        switch (e.stringParameter)
        {
            case "FootDown":
                Debug.LogWarning("footDown");
                break;
            case "LeftFootDown":
               
                break;
            case "RightFootDown":
               
                break;
            case "Land":
               
                break;
            default:
              
                break;

        }
    }
}
