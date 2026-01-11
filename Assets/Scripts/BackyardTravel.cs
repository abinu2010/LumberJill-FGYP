using UnityEngine;

public class BackyardTravel : MonoBehaviour
{
    public Transform cameraRig;          // isometric root object
    public Transform frontYardPoint;     // front anchor
    public Transform backyardPoint;      // back anchor

    public GameObject frontYardHUD;      // front ui root
    public GameObject backyardHUD;       // back ui root

    public isometricCamera isoCamera;    // iso camera script
    public GameObject frontMapObject;    // front bounds plane
    public GameObject backMapObject;     // back bounds plane

    public void GoToBackyard()
    {
        MoveTo(backyardPoint);
        if (frontYardHUD) frontYardHUD.SetActive(false);
        if (backyardHUD) backyardHUD.SetActive(true);
        if (isoCamera && backMapObject) isoCamera.SetMapObject(backMapObject);
    }

    public void GoToFrontYard()
    {
        MoveTo(frontYardPoint);
        if (frontYardHUD) frontYardHUD.SetActive(true);
        if (backyardHUD) backyardHUD.SetActive(false);
        if (isoCamera && frontMapObject) isoCamera.SetMapObject(frontMapObject);
    }

    private void MoveTo(Transform target)
    {
        if (!cameraRig || !target) return;

        Vector3 pos = cameraRig.position;
        pos.x = target.position.x;
        pos.z = target.position.z;
        cameraRig.position = pos;
    }
}
