using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalPlayer : MonoBehaviour
{
    [SerializeField] private GameObject FirstPerson;

    // Start is called before the first frame update
    void Start()
    {
        Instantiate(FirstPerson);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
