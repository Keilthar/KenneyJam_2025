using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CTRL_Player : MonoBehaviour
{
    public static CTRL_Player SGL;

    [Header("Components")]
    [SerializeField] Rigidbody _Rigidbody;
    Transform _Player;
    [SerializeField] Animator _Animator;

    [Header("Movement")]
    [SerializeField] float _MoveSpeed_Base;
    [SerializeField] float _MoveSpeed_Current;
    [SerializeField] float _RotationSpeed;

    bool _IsAnimationLocked;

    [Header("Bite")]
    [SerializeField] float _JumpCountMax;
    [SerializeField] float _JumpCountCurrent;
    [SerializeField] float _JumpRange;
    [SerializeField] float _JumpCooldown;
    [SerializeField] float _JumpTimer;
    [SerializeField] float _JumpDuration;
    [SerializeField] AnimationCurve _JumpSpeed;
    [SerializeField] AnimationCurve _JumpHeight;

    [Header("Bite")]
    [SerializeField] float _BiteCooldown;
    [SerializeField] float _BiteTimer;
    [SerializeField] float _BiteRange;

    [Header("Leveling")]
    [SerializeField] int _KillCounter;

    void Awake()
    {
        if (SGL == null)
            SGL = this;
        else
            Debug.LogError("Duplicated Singleton : " + this.name);

        // Components
        _Player = transform.Find("Player_Model");
        _Rigidbody = transform.GetComponent<Rigidbody>();

        // Move speed
        _MoveSpeed_Base = 10f;
        _MoveSpeed_Current = 10f;

        _IsAnimationLocked = false;

        //Skills
        _JumpCountMax = 2;
        _JumpCountCurrent = _JumpCountMax;
        _JumpTimer = 0;
        _BiteTimer = _BiteCooldown;

        // Leveling
        _KillCounter = 0;

        // Cursor hide
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Vector3 _JumpPosition = Jump_DisplayTargetPosition();
        
        if (_IsAnimationLocked == true)
            return;

        // Movements
        Move_Player();
        Rotate_Player();

        // Skills
        Jump(_JumpPosition);
        Bite();
    }

    void Move_Player()
    {
        float _InputHorizontal = Input.GetAxis("Horizontal");
        float _InputVertical = Input.GetAxis("Vertical");
        if (_InputHorizontal == 0 && _InputVertical == 0)
        {
            _Animator.SetBool("Move", false);
            return;
        }
        else
            _Animator.SetBool("Move", true);

        Vector3 _DirectionForward = transform.forward;
        _DirectionForward.y = 0f;
        Vector3 _DirectionRight = transform.right;
        _DirectionRight.y = 0f;
        Vector3 _MoveDirection = (_InputHorizontal * _DirectionRight + _InputVertical * _DirectionForward).normalized;

        float _DistanceToCheck = 1f;
        int _LayersToCheck = LayerMask.GetMask("Buildings", "Citizens");
        if (Physics.Raycast(transform.position, _MoveDirection, out _, _DistanceToCheck, _LayersToCheck) == false)
            transform.position += _MoveDirection * _MoveSpeed_Current * Time.deltaTime;
    }

    void Rotate_Player()
    {
        float _InputHorizontal = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.up, _InputHorizontal * _RotationSpeed * Time.deltaTime);
        _Rigidbody.rotation = transform.rotation;
    }

    #region Jump

    void Jump(Vector3 _JumpPosition)
    {
        // Skill cooldown
        if (_JumpCountCurrent < _JumpCountMax)
        {
            _JumpTimer += Time.deltaTime;
            if (_JumpTimer > _JumpCooldown)
                _JumpCountCurrent++;
        }

        // Check skill availability
        if (_JumpCountCurrent == 0)
            return;

        // Check skill input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _JumpCountCurrent--;
            StartCoroutine(C_Jump(_JumpPosition));
        }
    }

    Vector3 Jump_DisplayTargetPosition()
    {
        // Init
        Vector3 _InitialPosition = transform.position;
        _InitialPosition.y = 0f;
        Vector3 _JumpDirection = transform.forward;
        _JumpDirection.y = 0f;
        Vector3 _TargetPosition = _InitialPosition + _JumpRange * _JumpDirection;

        // Check position looked at player and find any collider to jump on
        int _LayersToCheck = LayerMask.GetMask("Buildings", "Citizens", "Ground");
        float _StopDistanceFromHit = 0.5f;
        Vector3 _RayCastOrigin = CTRL_PlayerCamera.SGL.transform.position;
        Vector3 _RayCastirection = CTRL_PlayerCamera.SGL.transform.forward;
        if (Physics.Raycast(_RayCastOrigin, _RayCastirection, out RaycastHit _JumpHit, _JumpRange, _LayersToCheck))
        {
            // Jump in front of building (avoid to bugs with physic/collider)
            if (_JumpHit.transform.name == "Building")
                _TargetPosition = _JumpHit.point - _StopDistanceFromHit * _JumpDirection;
            else
                _TargetPosition = _JumpHit.point;
        }
        _TargetPosition.y = 0;

        return _TargetPosition;
    }

    IEnumerator C_Jump(Vector3 _JumpPosition)
    {
        _IsAnimationLocked = true;
        Vector3 _InitialPosition = transform.position;
        _InitialPosition.y = 0f;

        // Jump loop
        float _JumpTimer = 0f;
        Vector3 _NewPosition;
        while (_JumpTimer < _JumpDuration)
        {
            float _JumpProgression = _JumpSpeed.Evaluate(_JumpTimer / _JumpDuration);
            _NewPosition = Vector3.Lerp(_InitialPosition, _JumpPosition, _JumpProgression);
            float _Height = _JumpHeight.Evaluate(_JumpProgression);
            _NewPosition += _Height * Vector3.up;
            transform.position = _NewPosition;

            yield return new WaitForEndOfFrame();
            _JumpTimer += Time.deltaTime;
        }
        transform.position = _JumpPosition;

        _IsAnimationLocked = false;
        yield return null;
    }

    #endregion Jump
    
    #region Bite
    void Bite()
    {
        // Bite cooldown
        if (_BiteTimer < _BiteCooldown)
            _BiteTimer += Time.deltaTime;

        // Check skill availability
        if (_BiteTimer < _BiteCooldown)
            return;

        // Check skill input
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            _BiteTimer = 0f;

            Vector3 _BoxColliderPosition = transform.position + transform.forward * _BiteRange / 2 + _BiteRange / 2 * Vector3.up;
            Vector3 _BoxColliderHalf = new Vector3(2f, 1f, 0.5f * _BiteRange);
            int _LayersToCheck = LayerMask.GetMask("Citizens");
            Collider[] _HitCitizens = Physics.OverlapBox(
                                            _BoxColliderPosition,
                                            _BoxColliderHalf,
                                            transform.rotation,
                                            _LayersToCheck);

            if (_HitCitizens.Length > 0)
            {
                for (int _CitizenID = _HitCitizens.Length -1; _CitizenID >= 0; _CitizenID--)
                {
                    CTRL_Citizen _Citizen = _HitCitizens[_CitizenID].GetComponent<CTRL_Citizen>();
                    MNGR_Citizens.SGL.Kill_Citizen(_Citizen);
                } 
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 _BoxColliderPosition = transform.position + transform.forward * _BiteRange / 2 + _BiteRange / 2 * Vector3.up;
        Vector3 _BoxSize = new Vector3(2f, 1f, 0.5f * _BiteRange);
        Gizmos.matrix = Matrix4x4.TRS(_BoxColliderPosition, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, 2*_BoxSize);
    }

  
    #endregion Bite
}
