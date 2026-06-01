using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class CreateEnemyPrefabsBatch
{
    [MenuItem("Tools/Create Enemy Prefabs (Batch)")]
    public static void Execute()
    {
        var enemies = new (string dataName, string prefabName, string[] skillNames)[]
        {
            ("EnemyData 1",      "겁쟁이바다민족",  new string[0]),
            ("EnemyData 2",      "바다민족거인",    new[] { "기합", "심호흡" }),
            ("자바문어",          "자바문어",        new[] { "신체재생" }),
            ("자바문어의 다리",   "자바문어의다리",  new string[0]),
            ("와이반",            "와이반",          new[] { "용언세뇌" }),
            ("린드블룸",          "린드블룸",        new string[0]),
            ("드래곤",            "드래곤",          new[] { "용의 마력" }),
            ("우앙개미",          "우앙개미",        new[] { "지원 요청" }),
            ("리엔스",            "리엔스",          new[] { "거인의 힘" }),
        };

        foreach (var (dataName, prefabName, skillNames) in enemies)
            CreatePrefab(dataName, prefabName, skillNames);

        // 리엔스 EnemyData에 패마·보스 트레잇 추가
        UpdateRiensTraits();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CreateEnemyPrefabsBatch] 완료!");
    }

    static void CreatePrefab(string dataAssetName, string prefabName, string[] skillNames)
    {
        var dataPath = $"Assets/Data/Enemy/{dataAssetName}.asset";
        var data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPath);
        if (data == null) { Debug.LogError($"EnemyData 없음: {dataPath}"); return; }

        var skillList = new List<SkillData>();
        foreach (var s in skillNames)
        {
            var sd = AssetDatabase.LoadAssetAtPath<SkillData>($"Assets/Data/Skills/{s}.asset");
            if (sd != null) skillList.Add(sd);
            else Debug.LogWarning($"SkillData 없음: {s}.asset");
        }

        var go = new GameObject(prefabName);
        go.AddComponent<SpriteRenderer>();
        go.AddComponent<UnitMovement>();

        var enemy = go.AddComponent<Enemy>();
        enemy.EnemyData = data;

        var unit = go.AddComponent<EnemyUnit>();
        unit.skillData = skillList.ToArray();

        var prefabPath = $"Assets/Prefabs/Enemy/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        Debug.Log($"프리팹 생성: {prefabPath}");
    }

    static void UpdateRiensTraits()
    {
        var riensData = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/Enemy/리엔스.asset");
        if (riensData == null) { Debug.LogError("리엔스 EnemyData 없음"); return; }

        var parryTrait  = AssetDatabase.LoadAssetAtPath<TraitData>("Assets/Data/Traits/패마.asset");
        var bossTrait   = AssetDatabase.LoadAssetAtPath<TraitData>("Assets/Data/Traits/보스.asset");

        var so = new SerializedObject(riensData);
        var traitsProp = so.FindProperty("traits");

        // 기존 null 제거 후 패마·보스 추가
        var existing = new List<TraitData>();
        for (int i = 0; i < traitsProp.arraySize; i++)
        {
            var t = traitsProp.GetArrayElementAtIndex(i).objectReferenceValue as TraitData;
            if (t != null) existing.Add(t);
        }
        if (parryTrait != null && !existing.Contains(parryTrait)) existing.Add(parryTrait);
        if (bossTrait  != null && !existing.Contains(bossTrait))  existing.Add(bossTrait);

        traitsProp.arraySize = existing.Count;
        for (int i = 0; i < existing.Count; i++)
            traitsProp.GetArrayElementAtIndex(i).objectReferenceValue = existing[i];

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(riensData);
        Debug.Log($"리엔스 트레잇 업데이트: {existing.Count}개");
    }
}
