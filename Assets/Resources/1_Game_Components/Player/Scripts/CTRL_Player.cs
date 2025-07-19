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
    bool _IsMoveLocked;

    [Header("Bite")]
    [SerializeField] float _BiteRange;
    [SerializeField] float _BiteCooldown;
    [SerializeField] float _BiteTimer;
    [SerializeField] float _BiteJumpDuration;
    [SerializeField] AnimationCurve _BiteJumpSpeed;
    [SerializeField] AnimationCurve _BiteJumpHeight;


    [Header("Stomp")]
    [SerializeField] float _StompDistanceFromPlayer;
    [SerializeField] float _StompAreaSize;
    [SerializeField] float _StompCooldown;
    [SerializeField] float _StompTimer;

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
        _IsMoveLocked = false;
        _MoveSpeed_Base = 10f;
        _MoveSpeed_Current = 10f;

        //Skills
        _BiteTimer = _BiteCooldown;
        _StompTimer = _StompCooldown;

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
        // Movements
        Move_Player();
        Rotate_Player();

        
        // Skills
        Bite();
        Stomp();
    }

    public Vector3 Get_Position()
    {
        return transform.position;
    }

    void Move_Player()
    {
        if (_IsMoveLocked == true)
            return;

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

    #region Skills
    void Bite()
    {
        _BiteTimer += Time.deltaTime;
        if (_BiteTimer > _BiteCooldown)
            _BiteTimer = _BiteCooldown;

        CTRL_Citizen _TargetedCitizen = CTRL_PlayerCamera.SGL.Check_Target(_BiteRange);
        if (Input.GetKeyDown(KeyCode.Mouse0) && _BiteTimer == _BiteCooldown)
        {
            if (_TargetedCitizen == null)
                return;

            _BiteTimer = 0f;
            StartCoroutine(BiteJump(_TargetedCitizen));
        }
    }

    IEnumerator BiteJump(CTRL_Citizen _TargetedCitizen)
    {
        _IsMoveLocked = true;

        _Animator.SetBool("Jump", true);

        float _JumpTimer = 0f;
        Vector3 _InitialPosition = transform.position;
        Vector3 _NewPosition;

        Vector3 _JumpDirection = _TargetedCitizen.Get_Position() - transform.position;
        Vector3 _TargetPosition = transform.position + _JumpDirection - 0.5f * _JumpDirection.normalized;
        _TargetPosition.y = 0;

        while (_JumpTimer <= _BiteJumpDuration)
        {
            float _JumpProgression = _BiteJumpSpeed.Evaluate(_JumpTimer / _BiteJumpDuration);
            _NewPosition = Vector3.Lerp(_InitialPosition, _TargetPosition, _JumpProgression);
            float _Height = _BiteJumpHeight.Evaluate(_JumpProgression);
            _NewPosition += _Height * Vector3.up;
            transform.position = _NewPosition;

            yield return new WaitForEndOfFrame();
            _JumpTimer += Time.deltaTime;
        }
        transform.position = _TargetPosition;
        _IsMoveLocked = false;

        MNGR_Citizens.SGL.Kill_Citizen(_TargetedCitizen);
        _KillCounter++;

        _Animator.SetBool("Jump", false);
        yield return null;
    }

    void Stomp()
    {
        /*_StompTimer += Time.deltaTime;
        if (_StompTimer > _StompCooldown)
            _StompTimer = _StompCooldown;

        if (Input.GetKeyDown(KeyCode.Mouse1) && _StompTimer == _StompCooldown)
        {
            Vector3 _StompAreaCenter = transform.position + transform.forward * _StompDistanceFromPlayer;
            List<CTRL_Citizen> _TargetedCitizens = MNGR_Citizens.SGL.Get_CitizensInArea(_StompAreaCenter, _StompAreaSize);
            if (_TargetedCitizens.Count == 0)
                return;

            _StompTimer = 0f;
            foreach (CTRL_Citizen _TargetedCitizen in _TargetedCitizens)
            {
                MNGR_Citizens.SGL.Kill_Citizen(_TargetedCitizen);
                _KillCounter++;
            }
        }*/
    }
    
#endregion Skills
}
