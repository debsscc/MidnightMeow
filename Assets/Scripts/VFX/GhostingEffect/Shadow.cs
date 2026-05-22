using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Pool;

public class Shadow : MonoBehaviour
{
    public static Shadow me;
    public GameObject Sombra;
    public List<GameObject> SombraList = new List<GameObject>();
    private float cronometer;
    public float speed;
    public Color _color;

    void Awake()
    {
        me = this;

    }

    public GameObject GetShadows()
    {
        for(int i = 0; i < SombraList.Count; i++)
        {
            if (!SombraList[i].activeInHierarchy)
            {
                SombraList[i].transform.position = transform.position;
                SombraList[i].transform.rotation = transform.rotation;
                SombraList[i].transform.localScale = transform.localScale;
                SombraList[i].SetActive(true);
                var sr = GetComponent<SpriteRenderer>();
                var objSr = SombraList[i].GetComponent<SpriteRenderer>();
                objSr.sprite = sr.sprite;
                objSr.sortingLayerID = sr.sortingLayerID;
                objSr.sortingOrder = sr.sortingOrder - 1;
                SombraList[i].GetComponent<Solid>()._color = _color;
                return SombraList[i];
            }
        }
        GameObject obj = Instantiate(Sombra, transform.position, transform.rotation) as GameObject;
        obj.transform.localScale = transform.localScale;
        var sourceSr = GetComponent<SpriteRenderer>();
        var newObjSr = obj.GetComponent<SpriteRenderer>();
        newObjSr.sprite = sourceSr.sprite;
        newObjSr.sortingLayerID = sourceSr.sortingLayerID;
        newObjSr.sortingOrder = sourceSr.sortingOrder - 1;
        obj.GetComponent<Solid>()._color = _color;
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