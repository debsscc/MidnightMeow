using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

public class Shadow : MonoBehaviour
{
    [Tooltip("Prefab do trail de dash (Assets/Prefabs/UI/Shadow.prefab). Não confundir com o filho Shadow = elipse no chão.")]
    public GameObject Sombra;
    public List<GameObject> SombraList = new List<GameObject>();
    private float cronometer;
    public float speed;
    public Color _color;

    public GameObject GetShadows()
    {
        if (Sombra == null)
            return null;

        var sourceSr = GetComponent<SpriteRenderer>();
        if (sourceSr == null)
            return null;

        for (int i = 0; i < SombraList.Count; i++)
        {
            if (!SombraList[i].activeInHierarchy)
            {
                SombraList[i].transform.position = transform.position;
                SombraList[i].transform.rotation = transform.rotation;
                SombraList[i].transform.localScale = transform.localScale;
                SombraList[i].SetActive(true);
                var objSr = SombraList[i].GetComponent<SpriteRenderer>();
                if (objSr != null)
                {
                    objSr.sprite = sourceSr.sprite;
                    objSr.flipX = sourceSr.flipX;
                    objSr.sortingLayerID = sourceSr.sortingLayerID;
                    objSr.sortingOrder = sourceSr.sortingOrder - 1;
                }

                if (SombraList[i].TryGetComponent(out Solid solid))
                    solid.SyncPresentation(_color);

                return SombraList[i];
            }
        }

        GameObject obj = Instantiate(Sombra, transform.position, transform.rotation);
        obj.transform.localScale = transform.localScale;
        var newObjSr = obj.GetComponent<SpriteRenderer>();
        if (newObjSr != null)
        {
            newObjSr.sprite = sourceSr.sprite;
            newObjSr.flipX = sourceSr.flipX;
            newObjSr.sortingLayerID = sourceSr.sortingLayerID;
            newObjSr.sortingOrder = sourceSr.sortingOrder - 1;
        }

        if (obj.TryGetComponent(out Solid newSolid))
            newSolid.SyncPresentation(_color);

        SombraList.Add(obj);
        return obj;
    }

    public void Sombras_skill()
    {
        cronometer += speed* Time.deltaTime;
        if (cronometer >= 1f)
        {
            GetShadows();
            cronometer = 0;
        }
    }

}