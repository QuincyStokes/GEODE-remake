using System;
using UnityEngine;

public class DifficultyButton : SimpleButton
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }
    public Difficulty difficulty;
}
