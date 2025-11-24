using System;
using UnityEngine;

public class DifficultyButton : SimpleButton
{
    public Difficulty difficulty;
}

public enum Difficulty
{
    Easy,
    Normal,
    Hard
}