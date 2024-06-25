using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardManager : MonoBehaviour
{
    public event EventHandler OnSelectedCard;

    [SerializeField] Transform[] cardArray;
    [SerializeField] Animator[] animatorArray;
    [SerializeField] GameObject[] levelArray;
    [SerializeField] int numberSwap = 3;

    private int index = 0;

    private bool isAnimateSwap = false;
    private bool isReading = false;
    private bool isMessaging = false;
    private bool isPressedButtonStart = false;
    private Dictionary<GameObject, bool> cardDictionary = new Dictionary<GameObject, bool>();



    private void Start()
    {
        OnSelectedCard += CardManager_OnSelectedCard;
    }

    public void ButtonStart()
    {
        isPressedButtonStart = true;
        if (isAnimateSwap)
        {
            return;
        }
        else
        {
            StartCoroutine(SwapMultipleCards(numberSwap));
        }
        cardDictionary.Clear();
    }
    private void FlipDownCard()
    {
        foreach (var animator in animatorArray)
        {
            animator.SetTrigger("FlipDown");
        }
    }
    private void FlipUpCard()
    {
        foreach (var animator in animatorArray)
        {
            animator.SetTrigger("FlipUp");
        }
    }
    private IEnumerator SwapMultipleCards(int swapCount)
    {
        isAnimateSwap = true;

        FlipDownCard();

        yield return new WaitForSeconds(3f);

        System.Random rand = new System.Random();

        for (int i = 0; i < swapCount; i++)
        {
            int index1 = rand.Next(cardArray.Length);
            int index2;
            do
            {
                index2 = rand.Next(cardArray.Length);
            } while (index1 == index2);

            yield return StartCoroutine(SwapCard(index1, index2));
        }
        isAnimateSwap = false;
    }

    private IEnumerator SwapCard(int index1, int index2)
    {
        Vector3 p1 = cardArray[index1].localPosition;
        Vector3 p2 = cardArray[index2].localPosition;

        Vector3 moveDirec = (p2 - p1).normalized;
        if (moveDirec.x != 0f)
        {
            moveDirec = Vector3.up * moveDirec.x;
        }
        else if (moveDirec.y != 0f)
        {
            moveDirec = Vector3.right * moveDirec.y;
        }

        Vector3 midPoint1 = (p1 + p2) / 2 + moveDirec * 300f;
        Vector3 midPoint2 = (p1 + p2) / 2 - moveDirec * 300f;

        float duration = 1f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            Vector3 target1 = Vector3.Lerp(p1, midPoint1, t);
            Vector3 target2 = Vector3.Lerp(midPoint1, p2, t);
            Vector3 target3 = Vector3.Lerp(p2, midPoint2, t);
            Vector3 target4 = Vector3.Lerp(midPoint2, p1, t);

            cardArray[index1].localPosition = Vector3.Lerp(target1, target2, t);
            cardArray[index2].localPosition = Vector3.Lerp(target3, target4, t);

            yield return null;
        }
    }
    public void SelectCard()
    {
        if (isAnimateSwap) return;

        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;

        if (!cardDictionary.ContainsKey(selectedObj) && isPressedButtonStart)
        {
            cardDictionary[selectedObj] = true;

            Animator animator = selectedObj.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetTrigger("FlipUp");
            }

            if (cardDictionary.Count % 2 == 0)
            {
                Debug.Log("Excute");
                OnSelectedCard?.Invoke(this, EventArgs.Empty);
            }
        }
        if (!isReading && !isMessaging && selectedObj != null && selectedObj.layer == 6)
        {
            StartCoroutine(IEPlaySFX(selectedObj.GetComponent<Card>().TypeCard));
        }
    }
    private void CardManager_OnSelectedCard(object sender, EventArgs e)
    {
        List<GameObject> selectedCards = new List<GameObject>();

        foreach (var cardEntry in cardDictionary)
        {
            if (cardEntry.Value)
            {
                selectedCards.Add(cardEntry.Key);
                if (selectedCards.Count == 2)
                {
                    break;
                }
            }
        }

        if (selectedCards.Count == 2)
        {
            GameObject firstCard = selectedCards[0];
            GameObject secondCard = selectedCards[1];

            if (firstCard.GetComponent<Card>().TypeCard == secondCard.GetComponent<Card>().TypeCard)
            {
                if (!isMessaging)
                {
                    StartCoroutine(IEMessageTing("Ting", firstCard.GetComponent<Card>().TypeCard));
                }
            }
            else
            {
                StartCoroutine(IEPlaySFX("TryAgain"));
                ResetLevel();
            }
            cardDictionary[firstCard] = false;
            cardDictionary[secondCard] = false;
        }
    }
    private void ResetLevel()
    {
        isPressedButtonStart = false;
        cardDictionary.Clear();
        FlipUpCard();
}
    private IEnumerator IEPlaySFX(string nameSFX)
    {

        isReading = true;

        AudioManager.Instance.PlaySFX(nameSFX);

        float duration = AudioManager.Instance.sfxSource.clip.length;

        yield return new WaitForSeconds(duration);

        isReading = false;
    }
    private IEnumerator IEMessageTing(string nameSFX1,string nameSFX2)
    {

        isMessaging = true;

        AudioManager.Instance.PlaySFX(nameSFX1);

        float duration1 = AudioManager.Instance.sfxSource.clip.length;

        yield return new WaitForSeconds(duration1 / 2);

        AudioManager.Instance.PlaySFX(nameSFX2);

        float duration = AudioManager.Instance.sfxSource.clip.length;

        yield return new WaitForSeconds(duration);

        isMessaging = false;

        if (cardDictionary.Count == 4)
        {
            yield return new WaitForSeconds(1f);
            NextLevel();
        }
    }
    private void NextLevel()
    {
        ResetLevel();
        levelArray[index].SetActive(false);
        index = (++index) % levelArray.Length;
        levelArray[index].SetActive(true);
    }
}
