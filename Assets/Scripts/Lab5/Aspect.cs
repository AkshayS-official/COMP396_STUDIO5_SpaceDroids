using UnityEngine;

public class Aspect : MonoBehaviour
{
    // Used to define if an object is a Player or an Enemy
    public enum Affiliation
    {
        Player,
        Enemy
    }
    public Affiliation affiliation;
}
