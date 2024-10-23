using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveLimiter : MonoBehaviour
{
    public static MoveLimiter instance;

    [SerializeField]
    bool _initCharacterMove = true;

    public bool CharacterMove;

    private void OnEnable()
    {
        instance = this;   
    }

    // Start is called before the first frame update
    void Start()
    {
        CharacterMove = _initCharacterMove;
    }
}
