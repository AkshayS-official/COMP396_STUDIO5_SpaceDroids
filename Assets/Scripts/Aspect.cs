using UnityEngine;

public class Aspect : MonoBehaviour
{
    public enum Affiliation { Player, Enemy, Civilian, Neutral }
    public Affiliation affiliation = Affiliation.Neutral;
}