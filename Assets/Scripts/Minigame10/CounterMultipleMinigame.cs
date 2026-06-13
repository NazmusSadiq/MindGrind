using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class CounterMultipleMinigame : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    [SerializeField] private TMP_Text leftCounterText;
    [SerializeField] private TMP_Text rightCounterText;

    [SerializeField] private TMP_Text leftTargetText;
    [SerializeField] private TMP_Text rightTargetText;


    [Header("Buttons")]
    [SerializeField] private Button leftCounterButton;
    [SerializeField] private Button rightCounterButton;
    [SerializeField] private Button bonusButton;



    [Header("Bonus")]
    [SerializeField] private GameObject bonusTargetBox;
    [SerializeField] private TMP_Text bonusTargetText;



    [Header("Score")]
    [SerializeField] private MinigameBestScoreStore bestScoreStore;



    private int score;


    private int leftCounter = 1;
    private int rightCounter = 100;


    private int leftTarget;
    private int rightTarget;


    private int bonusTarget;


    private float targetTimer;
    private float bonusTimer;


    private bool changeLeft;


    private bool running;


    private int bonusAppearCount = 0;


    private Coroutine counterRoutine;



    private void Start()
    {

        if(!CheckSetup())
        {
            enabled=false;
            return;
        }


        score=0;

        running=true;


        AssignButtons();


        GenerateTargets();


        bonusTargetBox.SetActive(false);
        bonusButton.gameObject.SetActive(false);


        UpdateUI();


        counterRoutine = StartCoroutine(CounterLoop());

    }




    private IEnumerator CounterLoop()
    {

        while(running)
        {

            yield return new WaitForSeconds(0.8f);



            leftCounter++;

            rightCounter--;



            UpdateUI();



            if(leftCounter >= 100 && rightCounter <= 1)
            {
                EndGame();
                yield break;
            }

        }

    }





    private void Update()
    {

        if(!running)
            return;



        // target changing
        targetTimer += Time.deltaTime;


        if(targetTimer >= 4)
        {

            targetTimer=0;



            if(changeLeft)
            {
                leftTarget = Random.Range(1,11);
            }
            else
            {
                rightTarget = Random.Range(1,11);
            }


            changeLeft=!changeLeft;


            UpdateTargetUI();

        }




        // bonus timer

        if(bonusAppearCount < 2)
        {

            bonusTimer += Time.deltaTime;


            if(bonusTimer >= 20)
            {

                bonusTimer = 0;


                SpawnBonus();


                StartCoroutine(HideBonusAfterTime());

            }

        }

    }







    private void AssignButtons()
    {

        leftCounterButton.onClick.AddListener(CheckLeftCounter);

        rightCounterButton.onClick.AddListener(CheckRightCounter);

        bonusButton.onClick.AddListener(CheckBonus);

    }






    private void CheckLeftCounter()
    {

        if(!running)
            return;



        if(leftCounter % leftTarget == 0)
        {
            score += 10;
        }
        else
        {
            score -= 5;
        }



        UpdateUI();

    }







    private void CheckRightCounter()
    {

        if(!running)
            return;



        if(rightCounter % rightTarget == 0)
        {
            score += 10;
        }
        else
        {
            score -= 5;
        }



        UpdateUI();

    }







    private void CheckBonus()
    {

        if(!running)
            return;



        if(leftCounter % bonusTarget == 0 &&
           rightCounter % bonusTarget == 0)
        {

            score += 50;

        }



        bonusTargetBox.SetActive(false);
        bonusButton.gameObject.SetActive(false);


        UpdateUI();

    }






    private void GenerateTargets()
    {

        leftTarget = Random.Range(2,11);

        rightTarget = Random.Range(2,11);


        UpdateTargetUI();

    }







    private void SpawnBonus()
    {

        bonusAppearCount++;


        bonusTarget = Random.Range(2,11);



        bonusTargetText.text =
        "Bonus: " + bonusTarget;



        bonusTargetBox.SetActive(true);

        bonusButton.gameObject.SetActive(true);

    }






    private IEnumerator HideBonusAfterTime()
    {

        yield return new WaitForSeconds(10f);



        bonusTargetBox.SetActive(false);

        bonusButton.gameObject.SetActive(false);

    }







    private void UpdateUI()
    {

        leftCounterText.text =
        leftCounter.ToString();



        rightCounterText.text =
        rightCounter.ToString();



        scoreText.text =
        "Score: " + score;

    }






    private void UpdateTargetUI()
    {

        leftTargetText.text =
        "Target: " + leftTarget;



        rightTargetText.text =
        "Target: " + rightTarget;

    }







    private bool CheckSetup()
    {

        return scoreText &&
        leftCounterText &&
        rightCounterText &&
        bestScoreStore &&
        leftCounterButton &&
        rightCounterButton;

    }








    private void EndGame()
    {

        running=false;



        if(counterRoutine != null)
        {
            StopCoroutine(counterRoutine);
        }




        string id =
        SceneManager.GetActiveScene().name;



        int best =
        MinigameBestScoreStore.UpdateBestScore(
        id,
        score);



        bestScoreStore.ShowStats(
        score,
        best);



        Debug.Log(
        "Game Over Score : " + score);

    }

}