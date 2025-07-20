using UnityEngine;

public class MNGR_UIs : MonoBehaviour
{
    public static MNGR_UIs SGL;

    void Awake()
    {
        if (SGL == null)
            SGL = this;
        else
            Debug.LogError("Duplicated Singleton : " + this.name);
    }

    void Start()
    {

    }

    public void Set_Crosshair_EyeLittle()
    {

    }

    public void Set_Crosshair_EyeBig()
    {

    }
}
