using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Score_System : MonoBehaviour
{
    public Character character;
    public int score = 0;

    public void ResultScore()
    {
        if(score > 0)
        {
            character.Character_Gold = score;
            character.CharacterLevelUP(score);
        }
    }
}
