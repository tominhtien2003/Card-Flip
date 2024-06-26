using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class CardController : MonoBehaviour
{
    [SerializeField] private Transform[] cardArray;
    [SerializeField] private float moveDuration;

    private HashSet<GameObject> cardHashSet = new HashSet<GameObject>();
    private HashSet<GameObject> cardTaken = new HashSet<GameObject>();

    private Animator[] animators;
    private bool isAnimated = false;
    private bool isPressButton = false;
    private bool isMatchingCard = false;

    private void Start()
    {
        animators = GetComponentsInChildren<Animator>();
    }

    #region ShuffleCard

    public void HandleShuffleByButtonNewStart()
    {
        isPressButton = true;

        if (isAnimated) return;

        StartCoroutine(ShuffleCardsContinuously(20));
    }

    private IEnumerator ShuffleCardsContinuously(int times)
    {
        isAnimated = true;

        yield return IEFlipDown();

        Vector3[] startPositions = new Vector3[cardArray.Length];

        for (int t = 0; t < times; t++)
        {
            for (int i = 0; i < cardArray.Length; i++)
            {
                startPositions[i] = cardArray[i].position;
            }

            int[] indices = Enumerable.Range(0, cardArray.Length).ToArray();

            ShuffleArray(indices);

            yield return MoveCards(startPositions, indices);
        }
        isAnimated = false;
    }

    private IEnumerator IEFlipDown()
    {
        foreach (var animator in animators)
        {
            animator.SetTrigger("FlipDown");
        }
        yield return new WaitForSeconds(1f);
    }

    private void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = array[i];

            array[i] = array[randomIndex];

            array[randomIndex] = temp;
        }
    }

    private IEnumerator MoveCards(Vector3[] startPositions, int[] indices)
    {
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            for (int i = 0; i < cardArray.Length; i++)
            {
                cardArray[i].position = Vector3.Lerp(startPositions[i], startPositions[indices[i]], elapsed / moveDuration);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < cardArray.Length; i++)
        {
            cardArray[i].position = startPositions[indices[i]];
        }
    }

    #endregion

    #region SelectCard

    public void SelectCard()
    {
        if (isAnimated || !isPressButton || isMatchingCard) return;

        GameObject obj = EventSystem.current.currentSelectedGameObject;

        if (obj != null && obj.layer == 6 && !cardTaken.Contains(obj))
        {
            if (!cardHashSet.Contains(obj))
            {
                AudioManager.Instance.PlaySFX(obj.GetComponent<Card>().TypeCard);

                obj.GetComponent<Animator>().SetTrigger("FlipUp");
            }
            cardHashSet.Add(obj);

            if (cardHashSet.Count == 2 && !isMatchingCard)
            {
                List<GameObject> cardList = cardHashSet.ToList();

                StartCoroutine(HandleMatchedCards(cardList[0], cardList[1]));
            }
        }
    }

    private IEnumerator HandleMatchedCards(GameObject obj1, GameObject obj2)
    {
        isMatchingCard = true;

        if (obj1.GetComponent<Card>().TypeCard == obj2.GetComponent<Card>().TypeCard)
        {
            AudioManager.Instance.PlaySFX("Ting");

            yield return new WaitForSeconds(AudioManager.Instance.sfxSource.clip.length);

            cardTaken.Add(obj1);

            cardTaken.Add(obj2);
        }
        else
        {
            AudioManager.Instance.PlaySFX("TryAgain");

            yield return new WaitForSeconds(AudioManager.Instance.sfxSource.clip.length);

            obj1.GetComponent<Animator>().SetTrigger("FlipDown");

            obj2.GetComponent<Animator>().SetTrigger("FlipDown");

            yield return new WaitForSeconds(1f);
        }

        cardHashSet.Clear();

        isMatchingCard = false;
    }

    #endregion
}
