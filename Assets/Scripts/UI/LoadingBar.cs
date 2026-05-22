using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LoadingBar : MonoBehaviour
{
    private SceneTransition sceneTransition;
    public Image loadingBar; //Ref da image que vai ser preenchida
    public float fillSpeed = 0.1f; //Velocidade de preenchimento

    private void Start()
    {
        sceneTransition = SceneTransition.Instance;
        loadingBar.fillAmount = 0f;
        StartCoroutine(UpdateLoadingBar());
    }

    private IEnumerator UpdateLoadingBar()
    {
        // Aguarda o AsyncOperation ser iniciado
        while (sceneTransition.CurrentAsyncLoad == null)
            yield return null;

        // Preenche a barra suavemente conforme o progresso (0 a 0.9 = cena pronta)
        while (sceneTransition.CurrentAsyncLoad.progress < 0.9f)
        {
            float target = sceneTransition.CurrentAsyncLoad.progress / 0.9f;
            loadingBar.fillAmount = Mathf.MoveTowards(loadingBar.fillAmount, target, fillSpeed * Time.deltaTime);
            yield return null;
        }

        // Completa a barra até 1
        while (loadingBar.fillAmount < 1f)
        {
            loadingBar.fillAmount = Mathf.MoveTowards(loadingBar.fillAmount, 1f, fillSpeed * Time.deltaTime);
            yield return null;
        }
    }
}