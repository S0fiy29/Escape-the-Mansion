using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NPCManager : MonoBehaviour
{
    public GameObject npcSprite;
    public Image blackScreen;
    public float fadeDuration = 2.0f;
    public float waitDuration = 3.0f;

    private void Start()
    {
        SetBlackScreenAlpha(0);
    }

    public void StartFadeOutAndHideNPC()
    {
        StartCoroutine(FadeOutAndHideNPC());
    }

    private IEnumerator FadeOutAndHideNPC()
    {
        npcSprite.SetActive(false);
        yield return StartCoroutine(FadeToBlack(false));
    }

    public void StartFadeInAndShowNPC()
    {
        StartCoroutine(FadeInAndShowNPC());
    }

    private IEnumerator FadeInAndShowNPC()
    {
        npcSprite.SetActive(true);
        yield return StartCoroutine(FadeToBlack(false));
    }

    public IEnumerator FadeOut()
    {
        yield return StartCoroutine(FadeToBlack(true));
    }

    public IEnumerator FadeIn()
    {
        yield return StartCoroutine(FadeToBlack(false));
    }

    private IEnumerator FadeToBlack(bool isFadeOut)
    {
        float startAlpha = isFadeOut ? 0f : 1f;
        float endAlpha = isFadeOut ? 1f : 0f;
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / fadeDuration;

            SetBlackScreenAlpha(Mathf.Lerp(startAlpha, endAlpha, t));
            yield return null;
        }

        SetBlackScreenAlpha(endAlpha);
    }

    private void SetBlackScreenAlpha(float alpha)
    {
        if (blackScreen != null)
        {
            Color color = blackScreen.color;
            color.a = alpha;
            blackScreen.color = color;
        }
    }
}
