using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryStageAssetResolver : MonoBehaviour
{
      public List<StoryStageAsset> stages;
      public StoryStageAsset Resolve(string id) => stages.Find(s => s.stageId == id);
}
