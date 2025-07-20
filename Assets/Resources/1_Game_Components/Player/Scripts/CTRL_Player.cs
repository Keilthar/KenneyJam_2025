using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEngine;

public enum Player_Attacks
{
    Bite,
    Scratch
}

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

    [Header("Jump")]
    [SerializeField] float _JumpCountMax;
    [SerializeField] float _JumpCountCurrent;
    [SerializeField] float _JumpRange;
    [SerializeField] float _JumpCooldown;
    [SerializeField] float _JumpTimer;
    [SerializeField] float _JumpDuration;
    [SerializeField] AnimationCurve _JumpSpeed;
    [SerializeField] AnimationCurve _JumpHeight;
    [SerializeField] float _JumpHeightPeak;

    [Header("Bite")]
    [SerializeField] float _BiteCooldown;
    [SerializeField] float _BiteTimer;
    [SerializeField] float _BiteRange;
    [SerializeField] float _BiteDuration;

    [Header("Scratch")]
    [SerializeField] float _ScratchCooldown;
    [SerializeField] float _ScratchTimer;
    [SerializeField] float _ScratchRange;
    [SerializeField] float _ScratchDuration;

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
        if (_IsAnimationLocked == true)
            return;

        // Movements
        Move_Player();
        Rotate_Player();

        // Skills
        Jump();
        Bite();
        Scratch();
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

    void Jump()
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

        // Check target
        CTRL_Citizen _JumpTarget = Get_JumpTarget();
        if (_JumpTarget == null)
            return;

        // Check skill input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _JumpCountCurrent--;
            StartCoroutine(C_Jump(_JumpTarget));
        }
    }

    CTRL_Citizen Get_JumpTarget()
    {
        CTRL_Citizen _TargetCitizen = null;

        // Check for target
        int _LayersToCheck = LayerMask.GetMask("Buildings", "Citizens");
        Vector3 _RayCastOrigin = CTRL_PlayerCamera.SGL.transform.position;
        Vector3 _RayCastirection = CTRL_PlayerCamera.SGL.transform.forward;

        // Check for citizen (and building to not jump thought them)
        if (Physics.Raycast(_RayCastOrigin, _RayCastirection, out RaycastHit _JumpHit, _JumpRange, _LayersToCheck))
        {
            if (_JumpHit.transform.gameObject.layer == LayerMask.GetMask("Buildings") == false)
                _TargetCitizen = _JumpHit.transform.GetComponent<CTRL_Citizen>();
        }

        return _TargetCitizen;
    }

    IEnumerator C_Jump(CTRL_Citizen _JumpTarget)
    {
        _IsAnimationLocked = true;
        _Animator.SetBool("Jump", true);

        Vector3 _InitialPosition = transform.position;
        _InitialPosition.y = 0f;

        // Jump loop
        float _JumpTimer = 0f;
        Vector3 _NewPosition;
        while (_JumpTimer < _JumpDuration)
        {
            // Follow target move, but jump in front of it (0.5 meter), not at its position
            Vector3 _JumpVector = _JumpTarget.transform.position - transform.position;
            Vector3 _JumpPosition = transform.position + _JumpVector - 0.5f * _JumpVector.normalized;
            _JumpPosition.y = 0f;

            float _JumpProgression = _JumpSpeed.Evaluate(_JumpTimer / _JumpDuration);
            _NewPosition = Vector3.Lerp(_InitialPosition, _JumpPosition, _JumpProgression);
            float _Height = _JumpHeightPeak * _JumpHeight.Evaluate(_JumpProgression);
            _NewPosition += _Height * Vector3.up;
            transform.position = _NewPosition;

            yield return new WaitForEndOfFrame();
            _JumpTimer += Time.deltaTime;
        }

        _IsAnimationLocked = false;
        _Animator.SetBool("Jump", false);

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
            Vector3 _BoxColliderPosition = transform.position + transform.forward * _BiteRange / 2 + _BiteRange / 2 * Vector3.up;
            Vector3 _BoxColliderHalf = new Vector3(1f, 1f, 0.5f * _BiteRange);
            int _LayersToCheck = LayerMask.GetMask("Citizens");
            Collider[] _HitCitizens = Physics.OverlapBox(
                                            _BoxColliderPosition,
                                            _BoxColliderHalf,
                                            transform.rotation,
                                            _LayersToCheck);

            if (_HitCitizens.Length > 0)
            {
                _BiteTimer = 0f;
                StartCoroutine(C_Bite(_HitCitizens));
            }
        }
    }

    IEnumerator C_Bite(Collider[] _HitCitizens)
    {
        _IsAnimationLocked = true;
        _Animator.SetBool("Attack", true);
        _Animator.SetFloat("AttackType", (float)Player_Attacks.Bite);

        transform.LookAt(_HitCitizens[0].transform.position);

        // Stop  citizen hit
        for (int _CitizenID = _HitCitizens.Length - 1; _CitizenID >= 0; _CitizenID--)
        {
            CTRL_Citizen _Citizen = _HitCitizens[_CitizenID].GetComponent<CTRL_Citizen>();
            _Citizen.Hit();
        }

        // Animation run 
        float _BiteAnimationTimer = 0f;
        while (_BiteAnimationTimer < _BiteDuration)
        {
            _BiteAnimationTimer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        // Kill citizen hit
        for (int _CitizenID = _HitCitizens.Length - 1; _CitizenID >= 0; _CitizenID--)
        {
            CTRL_Citizen _Citizen = _HitCitizens[_CitizenID].GetComponent<CTRL_Citizen>();
            MNGR_Citizens.SGL.Kill_Citizen(_Citizen);
        }

        _IsAnimationLocked = false;
        _Animator.SetBool("Attack", false);
    }

    #endregion Bite

    #region Scratch
    void Scratch()
    {
        // Bite cooldown
        if (_ScratchTimer < _ScratchCooldown)
            _ScratchTimer += Time.deltaTime;

        // Check skill availability
        if (_ScratchTimer < _ScratchCooldown)
            return;

        // Check skill input
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Vector3 _BoxColliderPosition = transform.position + transform.forward * _ScratchRange / 2 + _ScratchRange / 2 * Vector3.up;
            Vector3 _BoxColliderHalf = new Vector3(1f, 1f, 0.5f * _ScratchRange);
            int _LayersToCheck = LayerMask.GetMask("Citizens");
            Collider[] _HitCitizens = Physics.OverlapBox(
                                            _BoxColliderPosition,
                                            _BoxColliderHalf,
                                            transform.rotation,
                                            _LayersToCheck);
            _ScratchTimer = 0f;
            StartCoroutine(C_Scratch(_HitCitizens));
        }
    }

    IEnumerator C_Scratch(Collider[] _HitCitizens)
    {
        _IsAnimationLocked = true;
        _Animator.SetBool("Attack", true);
        _Animator.SetFloat("AttackType", (float)Player_Attacks.Scratch);

        if (_HitCitizens.Length > 0)
        {
            // Stop  citizen hit
            for (int _CitizenID = _HitCitizens.Length - 1; _CitizenID >= 0; _CitizenID--)
            {
                CTRL_Citizen _Citizen = _HitCitizens[_CitizenID].GetComponent<CTRL_Citizen>();
                _Citizen.Hit();
            }
        }

        // Animation run 
        float _ScratchAnimationTimer = 0f;
        while (_ScratchAnimationTimer < _ScratchDuration)
        {
            _ScratchAnimationTimer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        if (_HitCitizens.Length > 0)
        {
            // Kill citizen hit
            for (int _CitizenID = _HitCitizens.Length - 1; _CitizenID >= 0; _CitizenID--)
            {
                CTRL_Citizen _Citizen = _HitCitizens[_CitizenID].GetComponent<CTRL_Citizen>();
                MNGR_Citizens.SGL.Kill_Citizen(_Citizen);
            }
        }

        _IsAnimationLocked = false;
        _Animator.SetBool("Attack", false);
    }

    #endregion Bite
}
