using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 汉诺塔游戏管理器 — 挂载到空物体 chh_GameManager
/// 状态机核心：IDLE(空闲) ⇄ DISK_SELECTED(已选盘) → ANIMATING(动画中) → IDLE
/// </summary>
public class chh_GameManager : MonoBehaviour
{
    // ── 游戏状态枚举 ──
    private enum GameState { IDLE, DISK_SELECTED, ANIMATING }

    // ══════════════════════════════════════
    // 公共引用（由 Editor 脚本或手动拖拽赋值）
    // ══════════════════════════════════════

    [Header("柱子引用（拖入三根柱子）")]
    public chh_Pillar[] pillars = new chh_Pillar[3];

    [Header("圆盘预制体（Element 0=大盘, 1=中盘, 2=小盘）")]
    public GameObject[] diskPrefabs = new GameObject[3];

    [Header("UI 引用（拖入 Canvas 下的 UI 对象）")]
    public Text winText;
    // ↑ 胜利提示文字

    public Text stepText;
    // ↑ 实时步数显示

    // ── 运行时私有变量 ──
    private GameState currentState = GameState.IDLE;
    private chh_Disk selectedDisk = null;
    // ↑ 当前选中的圆盘（高亮中的那个），null = 未选中
    private int stepCount = 0;
    private List<chh_Disk> allDisks = new List<chh_Disk>();
    // ↑ 所有活跃圆盘列表，重置时需要遍历销毁

    // ══════════════════════════════════════
    // Unity 生命周期
    // ══════════════════════════════════════

    void Start()
    {
        SpawnAllDisks();
        UpdateStepUI();
        winText.text = "";
    }

    // ══════════════════════════════════════
    // 圆盘生成
    // ══════════════════════════════════════

    /// <summary>
    /// 在 A 柱(pillars[0])上生成三张圆盘，大盘在下小盘在上
    /// </summary>
    private void SpawnAllDisks()
    {
        chh_Pillar pillarA = pillars[0];

        for (int i = 0; i < diskPrefabs.Length; i++)
        {
            GameObject diskObj = Instantiate(diskPrefabs[i]);
            chh_Disk disk = diskObj.GetComponent<chh_Disk>();

            if (disk == null)
            {
                Debug.LogError("预制体 Element " + i + " 上缺少 chh_Disk 组件！");
                continue;
            }

            diskObj.transform.position = pillarA.GetNextDiskPosition();
            disk.SetPillar(pillarA);
            pillarA.PushDisk(disk);
            allDisks.Add(disk);
        }
    }

    // ══════════════════════════════════════
    // 点击事件（由 chh_Disk / chh_Pillar 调用）
    // ══════════════════════════════════════

    /// <summary>玩家点击了某个圆盘</summary>
    public void OnDiskClicked(chh_Disk disk)
    {
        if (currentState == GameState.ANIMATING) return; // 动画中，忽略

        if (currentState == GameState.IDLE)
        {
            // 只能选柱子顶部的圆盘，不能选被压在下面的
            if (disk.GetPillar().PeekTop() != disk) return;

            selectedDisk = disk;
            disk.Select();
            currentState = GameState.DISK_SELECTED;
            return;
        }

        if (currentState == GameState.DISK_SELECTED)
        {
            if (disk == selectedDisk)
            {
                // 点同一个盘 → 取消选择
                selectedDisk.Deselect();
                selectedDisk = null;
                currentState = GameState.IDLE;
            }
            else if (disk.GetPillar().PeekTop() == disk)
            {
                // 点不同的顶部盘 → 切换选择
                selectedDisk.Deselect();
                selectedDisk = disk;
                selectedDisk.Select();
            }
        }
    }

    /// <summary>玩家点击了某根柱子</summary>
    public void OnPillarClicked(chh_Pillar pillar)
    {
        if (currentState != GameState.DISK_SELECTED || selectedDisk == null)
            return;

        chh_Pillar source = selectedDisk.GetPillar();

        // 点击当前所在柱子 = 取消选择
        if (pillar == source)
        {
            selectedDisk.Deselect();
            selectedDisk = null;
            currentState = GameState.IDLE;
            return;
        }

        // 合法性检查：大盘不能压小盘
        if (!IsValidMove(selectedDisk, pillar)) return;

        // ── 执行移动 ──
        currentState = GameState.ANIMATING;
        source.PopDisk();
        Vector3 targetPos = pillar.GetNextDiskPosition();
        selectedDisk.SetPillar(pillar);
        pillar.PushDisk(selectedDisk);
        stepCount++;
        UpdateStepUI();
        selectedDisk.Deselect();
        StartCoroutine(selectedDisk.AnimateMove(targetPos));
    }

    // ══════════════════════════════════════
    // 动画回调
    // ══════════════════════════════════════

    /// <summary>动画播放完毕，由 chh_Disk.AnimateMove 回调</summary>
    public void OnAnimationComplete()
    {
        selectedDisk = null;
        CheckWinCondition();
        if (currentState == GameState.ANIMATING)
            currentState = GameState.IDLE;
    }

    // ══════════════════════════════════════
    // 规则判定
    // ══════════════════════════════════════

    /// <summary>移动合法性：目标为空 或 目标顶部盘比移动盘大</summary>
    private bool IsValidMove(chh_Disk disk, chh_Pillar target)
    {
        chh_Disk top = target.PeekTop();
        if (top == null) return true;                        // 空柱：可以放
        return disk.sizeLevel > top.sizeLevel;
        // ↑ sizeLevel 1=大盘 3=小盘 → 数值越大越可以放
    }

    /// <summary>C 柱满 3 盘 = 胜利</summary>
    private void CheckWinCondition()
    {
        if (pillars[2].GetDiskCount() == diskPrefabs.Length)
        {
            winText.text = "恭喜通关！\n共使用 " + stepCount + " 步";
            currentState = GameState.IDLE;
        }
    }

    // ══════════════════════════════════════
    // UI
    // ══════════════════════════════════════

    /// <summary>重置游戏 → 由 UI 按钮 OnClick 调用</summary>
    public void chh_ResetGame()
    {
        // 销毁所有圆盘
        foreach (chh_Disk d in allDisks)
            if (d != null) Destroy(d.gameObject);
        allDisks.Clear();

        // 清空柱子
        foreach (chh_Pillar p in pillars)
            if (p != null) p.ClearDisks();

        // 重置状态
        selectedDisk = null;
        stepCount = 0;
        currentState = GameState.IDLE;

        // 重新生成
        SpawnAllDisks();
        UpdateStepUI();
        winText.text = "";
    }

    private void UpdateStepUI()
    {
        if (stepText != null)
            stepText.text = "步数: " + stepCount;
    }
}
