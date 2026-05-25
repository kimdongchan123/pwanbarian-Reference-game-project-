using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CreateEnemyDataBatch
{
    const string SKILL_PATH = "Assets/Data/Skills/";
    const string TRAIT_PATH = "Assets/Data/Traits/";
    const string ENEMY_PATH = "Assets/Data/Enemy/";

    public static void UpdateSkillEffects()
    {
        void Fix(string name, SkillEffect effect)
        {
            string path = SKILL_PATH + name + ".asset";
            var sd = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (sd == null) { Debug.LogWarning($"[Batch] Skill not found: {name}"); return; }
            sd.skillEffect = effect;
            EditorUtility.SetDirty(sd);
            Debug.Log($"[Batch] {name} skillEffect → {effect}");
        }

        Fix("신체재생", SkillEffect.bodyRegen);
        Fix("용언세뇌",  SkillEffect.brainwash);
        Fix("지원 요청", SkillEffect.callForAid);
        Fix("용의 마력", SkillEffect.dragonMana);

        AssetDatabase.SaveAssets();
        Debug.Log("[Batch] 스킬 Effect 업데이트 완료");
    }

    public static void Execute()
    {
        CreateNewSkills();
        CreateNewTraits();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        FillEnemyData();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateEnemyDataBatch] 모든 에셋 생성 완료!");
    }

    // ── Skill helpers ──────────────────────────────────────────────────────────

    static SkillData GetOrCreateSkill(string name, string desc, int ct,
        SkillEffect effect = SkillEffect.none, int val = 0, int dur = 0,
        int hpRec = 0, int stRec = 0)
    {
        string path = SKILL_PATH + name + ".asset";
        var sd = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (sd != null) return sd;

        sd = ScriptableObject.CreateInstance<SkillData>();
        sd.skillName = name;
        sd.description = desc;
        sd.coolTime = ct;
        sd.skillEffect = effect;
        sd.effectValue = val;
        sd.duration = dur;
        sd.hpRecover = hpRec;
        sd.stRecover = stRec;
        AssetDatabase.CreateAsset(sd, path);
        return sd;
    }

    static void CreateNewSkills()
    {
        GetOrCreateSkill("신체재생",
            "Hp를 (최대 Hp의 10%)만큼 회복한다.\nSt를 (최대 St의 10%)만큼 회복한다.\n사망한 [자바무너의 다리](1~2)를 부활시킨다. (Hp 100%, St 100%)",
            3, SkillEffect.hpbuff);

        GetOrCreateSkill("용언세뇌",
            "자신보다 최대 St가 낮은 적 기물(1)을 선택한다.\n선택한 적 기물의 St만큼 St를 소모하고 선택한 적 기물에게 {세뇌}(8)을 부여한다.",
            4);

        GetOrCreateSkill("지원 요청",
            "다음 턴에 [우앙개미](0~2)를 소환한다.",
            2);

        GetOrCreateSkill("용의 마력",
            "ㅡ마나ㅡ(1)을 쌓는다.",
            1);
    }

    // ── Trait helpers ──────────────────────────────────────────────────────────

    static TraitData GetOrCreateTrait(string name, string desc,
        TraitTriggerType trigger, TraitEffect effect,
        int stAmt = 0, string affTarget = "")
    {
        string path = TRAIT_PATH + name + ".asset";
        var td = AssetDatabase.LoadAssetAtPath<TraitData>(path);
        if (td != null) return td;

        td = ScriptableObject.CreateInstance<TraitData>();
        td.traitName = name;
        td.description = desc;
        td.triggerType = trigger;
        td.traitEffect = effect;
        td.stAmount = stAmt;
        td.affiliationTarget = affTarget;
        AssetDatabase.CreateAsset(td, path);
        return td;
    }

    static void CreateNewTraits()
    {
        GetOrCreateTrait("섭취",
            "공격 적중 시 상대 기물의 Hp가 (최대 Hp의 20%)이하인 경우 발동한다.\n공격 적중 시 상대 기물은 사망하고 그 포인트로 이동한다.\nHp(40), St(30)을 회복한다.",
            TraitTriggerType.OnHit, TraitEffect.absorption);

        GetOrCreateTrait("자바무너",
            "출격 시 [자바무너의 다리](8)을 소환한다.",
            TraitTriggerType.Passive, TraitEffect.javaSpawn);

        GetOrCreateTrait("기계정신",
            "St와 패닉 유형이 없다.\n모든 피스의 가치가 0이 된다.\n무효불가.",
            TraitTriggerType.Passive, TraitEffect.machineSpirit);

        GetOrCreateTrait("스텔스 무너",
            "모든 피스에 [연타]를 부여한다.\n공격 적중 시 15%의 확률로 {동요}(1) 부여.\n공격 적중 시 3%의 확률로 <피해량 감소>(1) 부여.\n공격 적중 시 10%의 확률로 [죄종(분노)](1) 피해.",
            TraitTriggerType.Passive, TraitEffect.stealthTentacle);

        GetOrCreateTrait("비행",
            "기물을 넘어 행마할 수, 공격할 수 있다.\n《포인트》의 효과를 받지 않는다.",
            TraitTriggerType.Passive, TraitEffect.flight);

        GetOrCreateTrait("공중곡예",
            "《방어 특성》\n이 특성은 《회피》로 취급된다.\nSp(4)이하인 기물의 공격을 받지 않는다.",
            TraitTriggerType.Passive, TraitEffect.aerialAcrobatics);

        GetOrCreateTrait("와이번 브레스",
            "[원거리 공격 전용] 적중 시 상대 기물에게 부여된 {화상}(1)을 소모하고 {화상}을 1번 발동시킨다.",
            TraitTriggerType.OnHit, TraitEffect.wyvernBreath);

        GetOrCreateTrait("어린 용",
            "공격 적중 시 20%의 확률로 {화상}(1)을 부여한다.\n공격 적중 시 21%의 확률로 {화상}(1)을 부여한다.\n공격 적중 시 19%의 확률로 {화상}(1)을 부여한다.",
            TraitTriggerType.OnHit, TraitEffect.youngDragon);

        GetOrCreateTrait("용의 비늘",
            "턴 시작 시 <보호막>(40)을 얻는다.",
            TraitTriggerType.TurnStart, TraitEffect.dragonScale, 40);

        GetOrCreateTrait("드래곤 브레스",
            "[원거리 공격 전용] 적중 시 상대 기물에게 부여된 {화상}을 수치만큼 발동하고 전부 소모한다.",
            TraitTriggerType.OnHit, TraitEffect.dragonBreath);

        GetOrCreateTrait("거센 불길",
            "전투 시작 시 ㅡ마나ㅡ(10)을 소모하는 것으로 발동한다.\n다음 턴까지 모든 피스가 [원거리 공격 전용]을 얻고, 공격 속성이 [특수{연소}]가 된다.",
            TraitTriggerType.TurnStart, TraitEffect.ragingFlame);

        GetOrCreateTrait("개미군세",
            "자신을 제외한 아군 {개미왕국} 기물만큼 Sp가 증가한다.",
            TraitTriggerType.Passive, TraitEffect.antArmy, 0, "개미왕국");

        GetOrCreateTrait("거인왕",
            "턴 시작 시 모든 아군 {거인}에게 <ATK 증가>(3)을 부여한다.\n(생존한 아군 {거인}×3)만큼 ATK가 증가한다.",
            TraitTriggerType.TurnStart, TraitEffect.giantKing, 3, "거인");

        GetOrCreateTrait("범람하는 바다의 재앙",
            "{젖음}에 면역이다.\n《바다 포인트》에서 <ATK 증가>(15)를 얻고, 모든 피스가 [확산-가로], [확산-세로], [확산-빗금], [확산-사선]을 얻는다.\n턴 종료 시 자신이 있는 포인트를 중심으로 주변의 포인트까지 《바다 포인트》가 된다.",
            TraitTriggerType.Passive, TraitEffect.floodingSeaDisaster);
    }

    // ── Enemy helpers ──────────────────────────────────────────────────────────

    static SkillData S(string name)
    {
        var sd = AssetDatabase.LoadAssetAtPath<SkillData>(SKILL_PATH + name + ".asset");
        if (sd == null) Debug.LogWarning($"[Batch] Skill not found: {name}");
        return sd;
    }

    static TraitData T(string name)
    {
        var td = AssetDatabase.LoadAssetAtPath<TraitData>(TRAIT_PATH + name + ".asset");
        if (td == null) Debug.LogWarning($"[Batch] Trait not found: {name}");
        return td;
    }

    static EnemyData GetOrCreateEnemy(string assetFileName)
    {
        string path = ENEMY_PATH + assetFileName + ".asset";
        var ed = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (ed == null)
        {
            ed = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(ed, path);
        }
        return ed;
    }

    static void FillEnemyData()
    {
        // ── 야만적인 바다민족 (기존 수정) ─────────────────────────────────────
        {
            var e = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_PATH + "Yaman.asset");
            if (e != null)
            {
                e.traits = new TraitData[] { T("불허") };
                e.skills = new SkillData[] { S("기합") };
                EditorUtility.SetDirty(e);
            }
        }

        // ── 냉정한 바다민족 (기존 빈 파일 채우기) ─────────────────────────────
        {
            var e = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_PATH + "Cold.asset");
            if (e != null)
            {
                e.unitName = "냉정한 바다민족";
                e.maxHp = 12; e.maxSt = 13;
                e.minSp = 1; e.maxSp = 3;
                e.minatk = 2; e.maxatk = 2;
                e.mindef = 2; e.maxdef = 2;
                e.panic = new Panic[] { Panic.panic };
                e.affiliation = "바다민족";
                e.unitTypeKeyword = "변형 체스";
                e.traitKeywords = new List<string> { "남성", "인간", "인간형" };
                e.physicalResist = 2f; e.mentalResist = 1f;
                e.specialResist = 1.5f; e.sinResist = 1.5f;
                e.defensiveCharacteristic = new DefensiveCharacteristic[] { DefensiveCharacteristic.avoidance };
                e.traits = new TraitData[] { T("회피"), T("정신 가다듬기") };
                e.skills = new SkillData[] { S("심호흡") };
                EditorUtility.SetDirty(e);
            }
        }

        // ── 겁쟁이 바다민족 (EnemyData 1 채우기) ──────────────────────────────
        {
            var e = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_PATH + "EnemyData 1.asset");
            if (e != null)
            {
                e.unitName = "겁쟁이 바다민족";
                e.maxHp = 7; e.maxSt = 8;
                e.minSp = 2; e.maxSp = 2;
                e.minatk = 1; e.maxatk = 1;
                e.mindef = 3; e.maxdef = 3;
                e.panic = new Panic[] { Panic.stun };
                e.affiliation = "바다민족";
                e.unitTypeKeyword = "변형 체스";
                e.traitKeywords = new List<string> { "양성", "인간", "인간형" };
                e.physicalResist = 2f; e.mentalResist = 1f;
                e.specialResist = 1.5f; e.sinResist = 1.5f;
                e.traits = new TraitData[] { T("생존발악") };
                e.skills = new SkillData[] { };
                EditorUtility.SetDirty(e);
            }
        }

        // ── 바다민족 거인 (EnemyData 2 채우기) ────────────────────────────────
        {
            var e = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_PATH + "EnemyData 2.asset");
            if (e != null)
            {
                e.unitName = "바다민족 거인";
                e.maxHp = 100; e.maxSt = 25;
                e.minSp = 5; e.maxSp = 8;
                e.minatk = 1; e.maxatk = 1;
                e.mindef = 10; e.maxdef = 10;
                e.panic = new Panic[] { Panic.denial };
                e.affiliation = "바다민족";
                e.unitTypeKeyword = "체스";
                e.traitKeywords = new List<string> { "여성", "인간", "인간형", "초거대" };
                e.physicalResist = 1f; e.mentalResist = 1f;
                e.specialResist = 1f; e.sinResist = 1f;
                e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.boss };
                e.traits = new TraitData[] {
                    T("바다민족의 영웅"), T("민족의 부름"),
                    T("불허"), T("정신 가다듬기"), T("신속"), T("보스")
                };
                e.skills = new SkillData[] { S("기합"), S("심호흡") };
                EditorUtility.SetDirty(e);
            }
        }

        // ── 거인 ───────────────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("Giant");
            e.unitName = "거인";
            e.maxHp = 300; e.maxSt = 150;
            e.minSp = 1; e.maxSp = 3;
            e.minatk = 7; e.maxatk = 7;
            e.mindef = 15; e.maxdef = 15;
            e.panic = new Panic[] { Panic.aggression };
            e.affiliation = "거인연맹";
            e.unitTypeKeyword = "변형 체스";
            e.traitKeywords = new List<string> { "양성", "거인", "인간형", "초거대", "인류의 천적" };
            e.physicalResist = 1f; e.mentalResist = 1.5f;
            e.specialResist = 0.8f; e.sinResist = 1.5f;
            e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.elite };
            e.traits = new TraitData[] { T("바다의 재앙"), T("엘리트") };
            e.skills = new SkillData[] { S("거인의 힘") };
            EditorUtility.SetDirty(e);
        }

        // ── 꿀꺽구리 (기존 수정) ───────────────────────────────────────────────
        {
            var e = AssetDatabase.LoadAssetAtPath<EnemyData>(ENEMY_PATH + "Gulpfrog.asset");
            if (e != null)
            {
                e.affiliation = "소속불명";
                e.unitTypeKeyword = "체스";
                e.traitKeywords = new List<string> { "양성", "인류의 천적" };
                e.panic = new Panic[] { Panic.instinct };
                e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.elite };
                e.defensiveCharacteristic = new DefensiveCharacteristic[] { DefensiveCharacteristic.avoidance };
                e.traits = new TraitData[] { T("회피"), T("섭취"), T("엘리트") };
                e.skills = new SkillData[] { S("혓바닥휘두르기") };
                EditorUtility.SetDirty(e);
            }
        }

        // ── 자바문어 ───────────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("자바문어");
            e.unitName = "자바문어";
            e.maxHp = 100; e.maxSt = 60;
            e.minSp = 3; e.maxSp = 5;
            e.minatk = 6; e.maxatk = 6;
            e.mindef = 30; e.maxdef = 30;
            e.panic = new Panic[] { Panic.instinct };
            e.affiliation = "소속불명";
            e.unitTypeKeyword = "체스";
            e.traitKeywords = new List<string> { "양성", "인류의 천적" };
            e.physicalResist = 0.8f; e.mentalResist = 0.8f;
            e.specialResist = 0.8f; e.sinResist = 0.8f;
            e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.boss };
            e.traits = new TraitData[] { T("자바무너"), T("보스"), T("섭취") };
            e.skills = new SkillData[] { S("신체재생") };
            EditorUtility.SetDirty(e);
        }

        // ── 자바문어의 다리 ────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("자바문어의 다리");
            e.unitName = "자바문어의 다리";
            e.maxHp = 30; e.maxSt = 0;
            e.minSp = 4; e.maxSp = 7;
            e.minatk = 0; e.maxatk = 2;
            e.mindef = 0; e.maxdef = 0;
            e.panic = new Panic[] { };
            e.affiliation = "소속불명";
            e.unitTypeKeyword = "체스";
            e.traitKeywords = new List<string> { "무성", "인류의 천적" };
            e.physicalResist = 2f; e.mentalResist = 2f;
            e.specialResist = 2f; e.sinResist = 2f;
            e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.machinespirit };
            e.traits = new TraitData[] { T("기계정신"), T("스텔스 무너") };
            e.skills = new SkillData[] { };
            EditorUtility.SetDirty(e);
        }

        // ── 와이반 ─────────────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("와이반");
            e.unitName = "와이반";
            e.maxHp = 40; e.maxSt = 30;
            e.minSp = 6; e.maxSp = 10;
            e.minatk = 4; e.maxatk = 4;
            e.mindef = 10; e.maxdef = 10;
            e.panic = new Panic[] { Panic.seizure };
            e.affiliation = "와이반 군락 연맹";
            e.unitTypeKeyword = "체스";
            e.traitKeywords = new List<string> { "양성", "용", "인류의 천적" };
            e.physicalResist = 1f; e.mentalResist = 0.5f;
            e.specialResist = 0.8f; e.sinResist = 1.5f;
            e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.flight };
            e.traits = new TraitData[] { T("비행"), T("공중곡예"), T("와이번 브레스") };
            e.skills = new SkillData[] { S("용언세뇌") };
            EditorUtility.SetDirty(e);
        }

        // ── 린드블룸 ───────────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("린드블룸");
            e.unitName = "린드블룸";
            e.maxHp = 25; e.maxSt = 10;
            e.minSp = 1; e.maxSp = 4;
            e.minatk = 3; e.maxatk = 6;
            e.mindef = 5; e.maxdef = 5;
            e.panic = new Panic[] { Panic.instinct };
            e.affiliation = "와이반 군락 연맹";
            e.unitTypeKeyword = "변형 체스";
            e.traitKeywords = new List<string> { "성별불명", "인간", "용", "인류의 천적" };
            e.physicalResist = 1.5f; e.mentalResist = 1.5f;
            e.specialResist = 1.5f; e.sinResist = 2f;
            e.traits = new TraitData[] { T("어린 용") };
            e.skills = new SkillData[] { };
            EditorUtility.SetDirty(e);
        }

        // ── 드래곤 ─────────────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("드래곤");
            e.unitName = "드래곤";
            e.maxHp = 200; e.maxSt = 100;
            e.minSp = 7; e.maxSp = 9;
            e.minatk = 10; e.maxatk = 15;
            e.mindef = 10; e.maxdef = 10;
            e.panic = new Panic[] { Panic.seizure };
            e.affiliation = "소속없음";
            e.unitTypeKeyword = "변형 체스";
            e.traitKeywords = new List<string> { "성별불명", "용", "초거대", "인류의 천적" };
            e.physicalResist = 1f; e.mentalResist = 0.5f;
            e.specialResist = 1f; e.sinResist = 1f;
            e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.boss };
            e.traits = new TraitData[] {
                T("용의 비늘"), T("드래곤 브레스"), T("거센 불길"), T("보스")
            };
            e.skills = new SkillData[] { S("용의 마력") };
            EditorUtility.SetDirty(e);
        }

        // ── 우앙개미 ───────────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("우앙개미");
            e.unitName = "우앙개미";
            e.maxHp = 44; e.maxSt = 44;
            e.minSp = 4; e.maxSp = 4;
            e.minatk = 4; e.maxatk = 4;
            e.mindef = 4; e.maxdef = 4;
            e.panic = new Panic[] { Panic.cower };
            e.affiliation = "개미왕국";
            e.unitTypeKeyword = "체스";
            e.traitKeywords = new List<string> { "암컷", "벌레", "인류의 천적" };
            e.physicalResist = 1f; e.mentalResist = 0.5f;
            e.specialResist = 1.5f; e.sinResist = 1f;
            e.traits = new TraitData[] { T("개미군세") };
            e.skills = new SkillData[] { S("지원 요청") };
            EditorUtility.SetDirty(e);
        }

        // ── 리엔스 ─────────────────────────────────────────────────────────────
        {
            var e = GetOrCreateEnemy("리엔스");
            e.unitName = "리엔스";
            e.maxHp = 550; e.maxSt = 450;
            e.minSp = 11; e.maxSp = 12;
            e.minatk = 8; e.maxatk = 8;
            e.mindef = 30; e.maxdef = 30;
            e.panic = new Panic[] { Panic.nobility };
            e.affiliation = "거인연맹";
            e.unitTypeKeyword = "변형 체스";
            e.traitKeywords = new List<string> { "양성", "거인", "인간형", "초거대", "마왕현상" };
            e.physicalResist = 1f; e.mentalResist = 1f;
            e.specialResist = 1f; e.sinResist = 1f;
            e.defensiveCharacteristic = new DefensiveCharacteristic[] { DefensiveCharacteristic.parry };
            e.universalCharacteristic = new UniversalCharacteristics[] { UniversalCharacteristics.boss };
            e.traits = new TraitData[] {
                T("거인왕"), T("범람하는 바다의 재앙"), T("보스")
            };
            e.skills = new SkillData[] { S("거인의 힘") };
            EditorUtility.SetDirty(e);
        }
    }
}
