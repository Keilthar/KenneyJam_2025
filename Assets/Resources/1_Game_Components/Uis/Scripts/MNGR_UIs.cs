using UnityEngine;

public class MNGR_UIs : MonoBehaviour
{
    public static MNGR_UIs SGL;
    Transform _Crosshair_EyeLittle;
    Transform _Crosshair_EyeBig;

    void Awake()
    {
        if (SGL == null)
            SGL = this;
        else
            Debug.LogError("Duplicated Singleton : " + this.name);

        _Crosshair_EyeLittle = transform.Find("Crosshair_EyeLittle");
        _Crosshair_EyeBig = transform.Find("Crosshair_EyeBig");
        Set_Crosshair_EyeLittle();
    }

    void Start()
    {

    }

    public void Set_Crosshair_EyeLittle()
    {
        _Crosshair_EyeLittle.gameObject.SetActive(true);
        _Crosshair_EyeBig.gameObject.SetActive(false);
    }

    public void Set_Crosshair_EyeBig()
    {
        _Crosshair_EyeLittle.gameObject.SetActive(false);
        _Crosshair_EyeBig.gameObject.SetActive(true);
    }
}
