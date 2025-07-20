using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class MNGR_Citizens : MonoBehaviour
{
    public static MNGR_Citizens SGL;
    
    public float _RunAway_DistanceFromPlayer;
    public float _CitizenRunAwayMinTime;
    public float _CitizenWalkSpeed;
    public float _CitizenRunSpeed;
    public float _CitiernDespawnTime;
    Transform _Player;

    [Header("Citizens")]
    List<CTRL_Citizen> _Citizens;
    GameObject _CitizenPrefab;

    [Header("Spawn")]
    GameObject[] _Spawns;
    [SerializeField] int _CitizenDensity;
    bool _IsSpawning;

    void Awake()
    {
        if (SGL == null)
            SGL = this;
        else
            Debug.LogError("Duplicated Singleton : " + this.name);  
    }

    void Start()
    {
        _Player = CTRL_Player.SGL.transform;

        _Citizens = new List<CTRL_Citizen>();
        _Spawns = GameObject.FindGameObjectsWithTag("CitizenSpawn");
        _CitizenPrefab = Resources.Load<GameObject>("1_Game_Components/Citizens/Prefab/Prefab_Citizen");
        StartCoroutine(SpawnAllCitizen());
    }

    void Update()
    {
        if (_Citizens.Count < _CitizenDensity && _IsSpawning == false)
           StartCoroutine(SpawnAllCitizen());
    }   

    IEnumerator SpawnAllCitizen()
    {
        _IsSpawning = true;
        while(_Citizens.Count != _CitizenDensity)
        {
            Spawn_Citizen();
            yield return new WaitForSeconds(0.5f);
        }
        _IsSpawning = false;
        yield return null;
    }

    void Spawn_Citizen()
    {
        int _SpawnID = UnityEngine.Random.Range(0, _Spawns.Count());
        Vector3 _SpawnPosition = _Spawns[_SpawnID].transform.position;

        GameObject _NewCitizen = Instantiate(_CitizenPrefab, _SpawnPosition, quaternion.identity);
        _NewCitizen.transform.SetParent(transform);
        _Citizens.Add(_NewCitizen.GetComponent<CTRL_Citizen>());
    }

    public void Kill_Citizen(CTRL_Citizen _KilledCitizen)
    {
        _Citizens.Remove(_KilledCitizen);
        _KilledCitizen.Kill(_Player.position);
        Spawn_Citizen();
    }

    public void Kill_Citizens(List<CTRL_Citizen> _KilledCitizens)
    {
        for (int _CitizenID = _KilledCitizens.Count - 1; _CitizenID >= 0; _CitizenID--)
        {
            _Citizens.Remove(_KilledCitizens[_CitizenID]);
            _KilledCitizens[_CitizenID].Kill(_Player.position);
            Spawn_Citizen();
        }
    }
}
