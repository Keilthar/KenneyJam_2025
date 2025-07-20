using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CTRL_Citizen : MonoBehaviour
{
    BoxCollider _BoxCollider;
    Rigidbody[] _RagdollBodies;
    [SerializeField] Rigidbody _Chest;
    [SerializeField] Animator _Animator;
    [SerializeField] int _PathPointID;
    [SerializeField] List<Vector3> _Path;
    bool _IsRunningAway;
    bool _IsMovingReverse;
    bool _IsDead;
    bool _IsHit;

    void Start()
    {
        // Init box collider
        _BoxCollider = GetComponent<BoxCollider>();
        _BoxCollider.enabled = true;

        // Set ragdoll off
        _RagdollBodies = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody _RB in _RagdollBodies)
        {
            _RB.constraints = RigidbodyConstraints.FreezeAll;
            _RB.isKinematic = true;
            _RB.useGravity = false;
        }

        (_Path, _PathPointID) = MNGR_Paths.SGL.Get_ClosestPath(transform.position);
        _IsRunningAway = false;
        _IsMovingReverse = UnityEngine.Random.value > 0.5f;
        _IsDead = false;
    }

    void Update()
    {
        if (_IsDead || _IsHit)
            return;

        Move();
    }

    void Move()
    {
        Vector3 _PlayerPosition = CTRL_Player.SGL.transform.position;
        float _DistanceFromPlayer = Vector3.Distance(transform.position, _PlayerPosition);
        if (_DistanceFromPlayer < MNGR_Citizens.SGL._RunAway_DistanceFromPlayer)
        {
            if (_IsRunningAway == false)
                StartCoroutine(RunAway());
        }
        else if (_IsRunningAway == false)
            MoveToNextPoint();
    }

    void MoveToNextPoint()
    {
        _Animator.SetBool("Run", true);

        // Move to next point
        Vector3 _TargetPosition = _Path[_PathPointID];
        Vector3 _MoveDirection = (_TargetPosition - transform.position).normalized;
        Vector3 _NewPosition = transform.position + _MoveDirection * MNGR_Citizens.SGL._CitizenWalkSpeed * Time.deltaTime;
        transform.LookAt(_NewPosition);
        transform.position = _NewPosition;

        // Check next point to move
        float _DistanceToTargetPoint = Vector3.Distance(_NewPosition, _TargetPosition);
        if (_DistanceToTargetPoint <= 2f)
        {
            if (_IsMovingReverse == false)
            {
                if (_PathPointID < _Path.Count - 1)
                        _PathPointID++;
                    else
                        _PathPointID = 0;
            }
            else if (_IsMovingReverse == true)
            {
                if (_PathPointID > 0)
                        _PathPointID--;
                    else
                        _PathPointID = _Path.Count - 1;
            }
        }
    }

    IEnumerator RunAway()
    {
        _IsRunningAway = true;

        float _RunAwayTime = 0f;
        bool _IsCloseToPlayer = true;
        while (_RunAwayTime < MNGR_Citizens.SGL._CitizenRunAwayMinTime || _IsCloseToPlayer == true)
        {
            if (_IsDead || _IsHit)
                yield break;

            Vector3 _PlayerPosition = CTRL_Player.SGL.transform.position;
            Vector3 _RunAwayDirection = Vector3.zero;
            Vector3 _DirectionForward = (transform.position - _PlayerPosition).normalized;
            _DirectionForward.y = 0f;
            Vector3 _DirectionForwardRight = (_DirectionForward + Vector3.Cross(Vector3.up, _DirectionForward).normalized).normalized;
            Vector3 _DirectionForwardLeft = (_DirectionForward - Vector3.Cross(Vector3.up, _DirectionForward).normalized).normalized;
            Vector3 _DirectionRight = Vector3.Cross(Vector3.up, _DirectionForward).normalized;
            Vector3 _DirectionLeft = -Vector3.Cross(Vector3.up, _DirectionForward).normalized;
            float _DistanceMoved = MNGR_Citizens.SGL._CitizenRunSpeed * Time.deltaTime;
            Vector3 _OriginPosition = transform.position + Vector3.up;
            float _DistanceToCheck = 2f;
            int _LayersToCheck = LayerMask.GetMask("Buildings", "Citizens");

            if (Physics.Raycast(_OriginPosition, _DirectionForward, out _, _DistanceToCheck, _LayersToCheck) == false)
                _RunAwayDirection = _DirectionForward;
            else if (Physics.Raycast(_OriginPosition, _DirectionForwardRight, out _, _DistanceToCheck, _LayersToCheck) == false)
                _RunAwayDirection = _DirectionForwardRight;
            else if (Physics.Raycast(_OriginPosition, _DirectionForwardLeft, out _, _DistanceToCheck, _LayersToCheck) == false)
                _RunAwayDirection = _DirectionForwardLeft;
            else if (Physics.Raycast(_OriginPosition, _DirectionRight, out _, _DistanceToCheck, _LayersToCheck) == false)
                _RunAwayDirection = _DirectionRight;
            else if (Physics.Raycast(_OriginPosition, _DirectionLeft, out _, _DistanceToCheck, _LayersToCheck) == false)
                _RunAwayDirection = _DirectionLeft;

            Debug.DrawRay(_OriginPosition, _DistanceToCheck * _RunAwayDirection, Color.green);

            // Check if blocked by decors
            if (_RunAwayDirection != Vector3.zero)
            {
                Vector3 _NewPosition = transform.position + _DistanceMoved * _RunAwayDirection;
                transform.LookAt(_NewPosition);
                transform.position = _NewPosition;
                _Animator.SetBool("Run", true);
            }
            else
            {
                _Animator.SetBool("Run", false);
                // fear animation
            }

            // Check if ran away far enough
            yield return new WaitForEndOfFrame();
            float _DistanceFromPlayer = Vector3.Distance(transform.position, _PlayerPosition);
            if (_DistanceFromPlayer < MNGR_Citizens.SGL._RunAway_DistanceFromPlayer)
            {
                _RunAwayTime = 0f;
                _IsCloseToPlayer = true;
            }
            else
            {
                _RunAwayTime += Time.deltaTime;
                _IsCloseToPlayer = false;
            }
        }


        _IsRunningAway = false;
        _PathPointID = Get_ClosestPathPoint();

        yield return null;
    }

    int Get_ClosestPathPoint()
    {
        int _ClosestPathPointID = -1;
        float _ClosestDistance = 9999f;
        for (int _PathPointID = 0; _PathPointID < _Path.Count; _PathPointID++)
        {
            float _CurrentDistance = Vector3.Distance(transform.position, _Path[_PathPointID]);
            if (_CurrentDistance < _ClosestDistance)
            {
                _ClosestPathPointID = _PathPointID;
                _ClosestDistance = _CurrentDistance;
            }
        }

        return _ClosestPathPointID;
    }

    public void Hit()
    {
        _IsHit = true;
        _Animator.SetBool("Run", false);
    }

    #region Kill
    public void Kill(Vector3 _PlayerPosition)
    {
        _IsDead = true;
        _BoxCollider.enabled = false;
        _Animator.enabled = false;

        Vector3 _ForceDirection = (transform.position - _PlayerPosition).normalized;
        float _ForceStrength = 50f;
        foreach (Rigidbody _RB in _RagdollBodies)
        {
            _RB.constraints = RigidbodyConstraints.None;
            _RB.isKinematic = false;
            _RB.useGravity = true;
            _RB.AddForce(_ForceDirection * _ForceStrength, ForceMode.Impulse);
            _RB.AddForce(Vector3.up * 50f, ForceMode.Impulse);
        }

        StartCoroutine(Despawn());
    }

    IEnumerator Despawn()
    {
        float _DespawnTimer = 0f;

        while (_DespawnTimer < MNGR_Citizens.SGL._CitiernDespawnTime)
        {
            yield return new WaitForEndOfFrame();
            _DespawnTimer += Time.deltaTime;
        }
            
        Destroy(gameObject);
    }

    #endregion Kill

}
