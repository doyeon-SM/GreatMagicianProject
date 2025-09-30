using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StoryStageAsset", menuName = "StoryMode/StoryStageAsset")]
public class StoryStageAsset : ScriptableObject
{
    [Header("식별자 (예: 1-1, 2-4)")]
    public string stageId = "1-1";

    [Header("웨이브 설정")]
    [Tooltip("스토리 웨이브 수 (scriptedWaves.Count와 일치 권장)")]
    public int waveCount = 1;

    [Tooltip("웨이브당 제한 시간(초). 기본 15초")]
    public float waveDuration = 15f;

    [System.Serializable]
    public class SpawnPack
    {
        public GameObject prefab;     // 소환할 몬스터(일반/보스 모두 가능)
        public int count = 1;         // 해당 웨이브에서 소환 수
        public float hpMultiplier = 1f; // 체력 배율(선택)
    }

    [System.Serializable]
    public class WavePlan
    {
        [Tooltip("1,2,3... (표시용). 배열 인덱스가 실제 웨이브 순서")]
        public int waveNumber = 1;
        [Tooltip("이 웨이브에서 소환할 몬스터 묶음들")]
        public List<SpawnPack> spawns = new List<SpawnPack>();
    }

    [Header("웨이브 스크립트(순서대로 진행)")]
    public List<WavePlan> scriptedWaves = new List<WavePlan>();

    [Header("클리어 보상")]
    [Tooltip("이 스테이지 전용 score(경험치/돈 환산에 사용).")]
    public int stageScore = 500;

    [Tooltip("추가 골드 - stageScore 외에 더 주고 싶을 때")]
    public int bonusGold = 0;

    [Tooltip("추가 EXP - stageScore 외에 더 주고 싶을 때")]
    public int bonusExp = 0;

    [Tooltip("확정 스킬 보상(전역 인덱스 기준). 필요 시 비워도 됨")]
    public List<int> guaranteedSkillIndices = new List<int>();
}
