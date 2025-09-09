using UnityEngine;
using UnityEngine.UI;
using UnityLibrary;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Text scoreText;
    private int score = 0;
    private List<string> gameMessages;
    
    void Start()
    {
        InitializeGame();
    }
    
    void InitializeGame()
    {
        gameMessages = new List<string>
        {
            "Great job!", "Awesome!", "Keep going!", "Fantastic!", "Well done!"
        };
        
        UpdateScore(0);
        
        // Example using library functions
        GameUtils.ShuffleList(gameMessages);
        Debug.Log($"Random message: {GameUtils.GetRandomElement(gameMessages)}");
        
        // Test mathematical functions
        for (int i = 1; i <= 10; i++)
        {
            if (MathHelper.IsPrime(i))
            {
                Debug.Log($"{i} is prime");
            }
        }
    }
    
    public void AddScore(int points)
    {
        score += points;
        UpdateScore(score);
        
        string message = GameUtils.GetRandomElement(gameMessages);
        Debug.Log(message);
    }
    
    void UpdateScore(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {GameUtils.FormatScore(newScore)}";
        }
    }
}
