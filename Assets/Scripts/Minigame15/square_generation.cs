using UnityEngine;

public class square_generation : MonoBehaviour
{
    public float x_pos = 10, y_pos = 10;
    public float x_spacing = 5;
    public GameObject Square;
    public string[] words = new string[5] { "Circle", "Square", "Triangle", "Rectangle", "Cube" };

    private string target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        target = words[Random.Range(0, words.Length)];

        int n = target.Length;
        for (int i = 0; i < n; i++)
        {
            Vector3 spawnPosition = new Vector3(x_pos + (i * x_spacing), y_pos, transform.position.z);
            Instantiate(Square, spawnPosition, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
