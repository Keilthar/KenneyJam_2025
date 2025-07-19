using Unity.Mathematics;
using UnityEngine;

public class CTRL_PlayerCamera : MonoBehaviour
{
    public static CTRL_PlayerCamera SGL;
    Transform _Player;
    [SerializeField] Vector3 _CameraOffSetPosition;
    [SerializeField] float _CameraTilt;
    public CTRL_Citizen _Target;
    [SerializeField] float _CameraSensivityVertical;
    [SerializeField] float _CameraRotationXMax;
    [SerializeField] float _CameraRotationXMin;

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
        _CameraTilt = (_CameraRotationXMin + _CameraRotationXMax) / 2;
        Set_Tilt(_CameraTilt);
    }

    void Update()
    {
        Vector3 _Adjustoffet = _Player.rotation * _CameraOffSetPosition;
        transform.position = _Player.position + _Adjustoffet;
        transform.rotation = _Player.rotation;

        float _inputVertical = Input.GetAxis("Mouse Y");
        if (_inputVertical != 0f)
            _CameraTilt -= _inputVertical * _CameraSensivityVertical * Time.deltaTime;
            
        Set_Tilt(_CameraTilt);
    }

    void Set_Tilt(float _NewTilt)
    {
        _CameraTilt = Mathf.Clamp(_NewTilt, _CameraRotationXMin, _CameraRotationXMax);
        Vector3 _Angles = transform.eulerAngles;
        _Angles.x = _CameraTilt;
        transform.eulerAngles = _Angles;
    }

    public CTRL_Citizen Check_Target(float _SkillRange)
    {
        Vector3 _OriginPosition = transform.position + Vector3.up;
        Vector3 _Direction = transform.forward;
        float _DistanceToCheck = _SkillRange + _CameraOffSetPosition.magnitude;

        Debug.DrawRay(_OriginPosition, _Direction * _DistanceToCheck, Color.red);
        if (Physics.Raycast(_OriginPosition, _Direction, out RaycastHit _Hit, _DistanceToCheck, LayerMask.GetMask("Citizens")))
        {
            MNGR_UIs.SGL.Set_Crosshair_EyeBig();
            return _Hit.transform.GetComponent<CTRL_Citizen>();
        }
        else
        {
            MNGR_UIs.SGL.Set_Crosshair_EyeLittle();
            return null;
        }
    }
}
