using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class MNGR_Paths : MonoBehaviour
{
    public static MNGR_Paths SGL;
    [SerializeField] List<List<Vector3>> _Paths;
    float _PathSpacing = 0.5f;
    void Awake()
    {
        if (SGL == null)
            SGL = this;
        else
            Debug.LogError("Duplicated Singleton : " + this.name); 

        _Paths = new List<List<Vector3>>();
        foreach (Transform _Child in transform)
        {
            SplineContainer _Container = _Child.GetComponent<SplineContainer>();
            Spline _Spline = _Container.Spline;
                _Paths.Add(Create_Path(_Spline));
        }
    }

    List<Vector3> Create_Path(Spline _Spline)
    {
        List<Vector3> _NewPath = new List<Vector3>();
        float _SplineLenght = _Spline.GetLength();
        int _PathPointCount = Mathf.FloorToInt(_SplineLenght / _PathSpacing);
        Vector3 _SplinePosition;
        for (int _PathPointID = 0; _PathPointID <= _PathPointCount; _PathPointID++)
        {
            float _Distance = _PathPointID * _PathSpacing;
            _SplinePosition = _Spline.EvaluatePosition(_Distance / _SplineLenght);
            _NewPath.Add(_SplinePosition);
        }

        _SplinePosition = _Spline.EvaluatePosition(1);
        _NewPath.Add(_SplinePosition);

        return _NewPath;
    }

    public (List<Vector3>, int) Get_ClosestPath(Vector3 _Position)
    {
        List<Vector3> _ClosestPath = new List<Vector3>();
        int _ClosestPathPointID = -1;
        int _ClosestPathID = -1;
        float _ClosestDistance = 9999999f;
        for (int _PathID = 0; _PathID < _Paths.Count; _PathID++)
        {
            for (int _PathPointID = 0; _PathPointID < _Paths[_PathID].Count; _PathPointID++)
            {
                float _CurrentDistance = Vector3.Distance(_Position, _Paths[_PathID][_PathPointID]);
                if (_CurrentDistance < _ClosestDistance)
                {
                    _ClosestPathPointID = _PathPointID;
                    _ClosestPathID = _PathID;
                    _ClosestDistance = _CurrentDistance;
                }
            }
        }

        return (_Paths[_ClosestPathID], _ClosestPathPointID);
    }
}
