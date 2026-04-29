using UnityEngine;

[System.Serializable]
public class GameLogBase
{
    public string msg;
    public Sprite icon;
    public float timelog;

    public GameLogBase(string msg, Sprite icon = null)
    {
        this.msg = msg;
        this.icon = icon;
        this.timelog = Time.time;
    }
}