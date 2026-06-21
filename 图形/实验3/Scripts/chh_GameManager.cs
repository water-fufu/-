using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 汉诺塔游戏管理器 — 挂载到空物体（chh_GameManager）上
/// 是整个游戏逻辑的核心，协调圆盘和柱子之间的交互
///
/// 游戏状态机：
///   IDLE          — 空闲，等待玩家选中圆盘
///   DISK_SELECTED — 玩家已选一个圆盘，等待选择目标柱子
///   ANIMATING     — 动画播放中，禁止任何操作
/// </summary>
public class chh_GameManager : MonoBehaviour
{
    // ==================== 游戏状态枚举 ====================

    private enum GameState
    {
        IDLE,           // 空闲：等待玩家点击圆盘
        DISK_SELECTED,  // 圆盘已选中：等待玩家点击目标柱子（或切换选择）
        ANIMATING       // 动画中：禁止一切操作
    }

    // ==================== 公共属性（在 Inspector 中拖拽赋值） ====================

    [Header("柱子引用（在 Inspector 中拖入）")]
    [Tooltip("三根柱子，按索引 0=左(A), 1=中(B), 2=右(C)")]
    public chh_Pillar[] pillars = new chh_Pillar[3];
    // ↑ Size 设为 3，依次拖入 chh_Pillar_Left, chh_Pillar_Mid, chh_Pillar_Right

    [Header("圆盘预制体（在 Inspector 中拖入）")]
    [Tooltip("三个圆盘预制体，按大小：Element 0=大盘, 1=中盘, 2=小盘")]
    public GameObject[] diskPrefabs = new GameObject[3];
    // ↑ Size 设为 3，依次拖入 chh_Disk_1（大盘/蓝）, chh_Disk_2（中盘/绿）, chh_Disk_3（小盘/红）

    [Header("UI 引用（在 Inspector 中拖入）")]
    [Tooltip("胜利信息文本（Canvas 下的 WinText）")]
    public Text winText;
    // ↑ 显示 "恭喜通关！" 等胜利提示

    [Tooltip("步数显示文本（Canvas 下的 StepText）")]
    public Text stepText;
    // ↑ 实时显示当前已用步数，格式："步数: 7"

    // ==================== 私有变量 ====================

    private GameState currentState = GameState.IDLE;
    // ↑ 当前游戏状态，初始为空闲

    private chh_Disk selectedDisk = null;
    // ↑ 当前被选中的圆盘引用（高亮中的那个），未选中时为 null

    private int stepCount = 0;
    // ↑ 步数计数器，每次成功移动 +1，重置时归零

    private List<chh_Disk> allDisks = new List<chh_Disk>();
    // ↑ 场景中所有圆盘的列表，重置时需要遍历销毁

    // ==================== Unity 生命周期 ====================

    void Start()
    {
        // 初始化：在 A 柱（pillars[0]）上生成三张圆盘
        SpawnAllDisks();
        // 更新 UI 显示
        UpdateStepUI();
        winText.text = "";
        // ↑ 开始时隐藏胜利信息
    }

    // ==================== 圆盘生成 ====================

    /// <summary>
    /// 在 A 柱（左柱）上按从大到小的顺序生成三张圆盘
    /// 大盘（prefab[0]）在底部，小盘（prefab[2]）在顶部
    /// </summary>
    private void SpawnAllDisks()
    {
        chh_Pillar pillarA = pillars[0];
        // ↑ A 柱 = 左柱，游戏开始时所有圆盘堆在这里

        for (int i = 0; i < diskPrefabs.Length; i++)
        {
            // 实例化预制体：在场景中生成一个圆盘 GameObject
            GameObject diskObj = Instantiate(diskPrefabs[i]);
            // ↑ diskPrefabs[0]=大盘, [1]=中盘, [2]=小盘

            chh_Disk disk = diskObj.GetComponent<chh_Disk>();
            // ↑ 获取圆盘上的 chh_Disk 脚本组件

            if (disk == null)
            {
                Debug.LogError("预制体 " + i + " 上没有 chh_Disk 组件！请检查预制体。");
                continue;
            }

            // 计算此圆盘在 A 柱上的放置位置
            Vector3 spawnPos = pillarA.GetNextDiskPosition();
            // ↑ 先放的大盘会在底部（Y 最低），后放的小盘在顶部（Y 最高）

            diskObj.transform.position = spawnPos;

            // 建立圆盘与柱子的双向关联
            disk.SetPillar(pillarA);
            // ↑ 告诉圆盘："你现在在 A 柱上"
            pillarA.PushDisk(disk);
            // ↑ 告诉 A 柱："这个圆盘在你上面"

            allDisks.Add(disk);
            // ↑ 加入总列表，方便重置时查找
        }
    }

    // ==================== 点击事件处理（由 chh_Disk / chh_Pillar 调用） ====================

    /// <summary>
    /// 当玩家点击某个圆盘时调用
    /// </summary>
    /// <param name="disk">被点击的圆盘</param>
    public void OnDiskClicked(chh_Disk disk)
    {
        // --- 动画播放中：忽略点击 ---
        if (currentState == GameState.ANIMATING) return;

        // --- 空闲状态：选中此圆盘 ---
        if (currentState == GameState.IDLE)
        {
            // 额外判断：只能选柱子顶部的圆盘（不能选被压在下面的）
            chh_Pillar pillar = disk.GetPillar();
            if (pillar.PeekTop() != disk)
            {
                Debug.Log("不能选中被压在下面的圆盘！只能选择每根柱子最顶部的圆盘。");
                return;
            }

            selectedDisk = disk;
            disk.Select();                             // 高亮显示
            currentState = GameState.DISK_SELECTED;    // 切换到"已选中"状态
            return;
        }

        // --- 已选中状态：判断是否点了同一个圆盘（取消选择）---
        if (currentState == GameState.DISK_SELECTED)
        {
            if (disk == selectedDisk)
            {
                // 点击同一个圆盘 → 取消选择
                selectedDisk.Deselect();
                selectedDisk = null;
                currentState = GameState.IDLE;
                return;
            }
            else
            {
                // 点击不同圆盘 → 切换选择（先取消旧的，再选中新的）
                // 同样需要检查是否在顶部
                chh_Pillar pillar = disk.GetPillar();
                if (pillar.PeekTop() != disk)
                {
                    Debug.Log("不能选中被压在下面的圆盘！");
                    return;
                }
                selectedDisk.Deselect();
                selectedDisk = disk;
                selectedDisk.Select();
                // 状态保持 DISK_SELECTED
                return;
            }
        }
    }

    /// <summary>
    /// 当玩家点击某根柱子时调用
    /// </summary>
    /// <param name="pillar">被点击的柱子</param>
    public void OnPillarClicked(chh_Pillar pillar)
    {
        // --- 只有"已选中圆盘"状态下，点击柱子才有意义 ---
        if (currentState != GameState.DISK_SELECTED) return;
        if (selectedDisk == null) return;

        // --- 检查移动合法性 ---
        chh_Pillar sourcePillar = selectedDisk.GetPillar();
        // ↑ 圆盘当前所在的柱子

        // 如果点击的就是圆盘当前所在的柱子 → 取消选择（不移动）
        if (pillar == sourcePillar)
        {
            selectedDisk.Deselect();
            selectedDisk = null;
            currentState = GameState.IDLE;
            return;
        }

        // 检查目标柱顶部圆盘是否比当前圆盘大（或为空）
        if (!IsValidMove(selectedDisk, pillar))
        {
            Debug.Log("非法移动！大盘不能放在小盘上面。");
            // 非法移动不消耗步数，保持选中状态
            return;
        }

        // --- 合法移动：执行移动 ---
        currentState = GameState.ANIMATING;  // 先锁定操作

        // 从源柱子移除圆盘
        sourcePillar.PopDisk();

        // 计算目标位置
        Vector3 targetPos = pillar.GetNextDiskPosition();
        // ↑ 获取此圆盘在目标柱子上的落脚坐标

        // 将圆盘放入目标柱子（先更新数据，动画再更新视觉）
        selectedDisk.SetPillar(pillar);
        pillar.PushDisk(selectedDisk);

        // 步数 +1
        stepCount++;
        UpdateStepUI();

        // 取消高亮
        selectedDisk.Deselect();

        // 启动移动动画
        StartCoroutine(selectedDisk.AnimateMove(targetPos));
        // ↑ 动画完成后会回调 OnAnimationComplete()
        //   注意：此时 selectedDisk 仍指向该圆盘，回调中会置 null
    }

    // ==================== 动画完成回调 ====================

    /// <summary>
    /// 圆盘动画播完后由 chh_Disk 调用
    /// </summary>
    public void OnAnimationComplete()
    {
        selectedDisk = null;
        // ↑ 清除选中引用

        // 检查胜利条件
        CheckWinCondition();

        // 恢复空闲状态（如果没赢的话）
        if (currentState == GameState.ANIMATING)
        {
            currentState = GameState.IDLE;
        }
    }

    // ==================== 移动合法性判断 ====================

    /// <summary>
    /// 判断将圆盘移动到目标柱子是否合法
    /// 规则：目标为空 OR 目标顶部圆盘比当前圆盘大（sizeLevel 更小 = 更大）
    /// </summary>
    /// <param name="disk">要移动的圆盘</param>
    /// <param name="targetPillar">目标柱子</param>
    /// <returns>true=合法, false=非法</returns>
    private bool IsValidMove(chh_Disk disk, chh_Pillar targetPillar)
    {
        chh_Disk topDisk = targetPillar.PeekTop();
        // ↑ 获取目标柱子最顶部的圆盘（null 表示柱子为空）

        if (topDisk == null)
        {
            return true;  // 目标柱为空，任何圆盘都可以放
        }

        // 关键规则：sizeLevel 越小 = 盘子越大
        // 当前盘必须比目标顶部盘小（即 sizeLevel 更大）
        // 例：sizeLevel=3（小盘）放到 sizeLevel=1（大盘）上：3 > 1 = 合法 ✓
        //     sizeLevel=1（大盘）放到 sizeLevel=3（小盘）上：1 > 3 = 非法 ✗
        return disk.sizeLevel > topDisk.sizeLevel;
    }

    // ==================== 胜利判定 ====================

    /// <summary>
    /// 检查 C 柱（索引 2）上是否集齐了全部 3 个圆盘
    /// </summary>
    private void CheckWinCondition()
    {
        chh_Pillar pillarC = pillars[2];
        // ↑ C 柱 = 右柱，游戏目标是所有盘移到这里

        if (pillarC.GetDiskCount() == diskPrefabs.Length)
        {
            // 胜利！
            winText.text = "恭喜通关！\n共使用 " + stepCount + " 步";
            currentState = GameState.IDLE;
            Debug.Log("游戏胜利！总步数：" + stepCount);
        }
    }

    // ==================== 重置游戏 ====================

    /// <summary>
    /// 一键重置游戏（由 UI 按钮的 OnClick 事件触发）
    /// 销毁所有圆盘 → 清空柱子 → 重新生成 → 步数归零
    /// </summary>
    public void chh_ResetGame()
    {
        // 1. 销毁所有圆盘 GameObject
        foreach (chh_Disk disk in allDisks)
        {
            if (disk != null)
            {
                Destroy(disk.gameObject);
            }
        }
        allDisks.Clear();
        // ↑ 清空列表，准备重新生成

        // 2. 清空所有柱子的堆栈
        foreach (chh_Pillar pillar in pillars)
        {
            if (pillar != null)
            {
                pillar.ClearDisks();
            }
        }

        // 3. 重置状态
        selectedDisk = null;
        stepCount = 0;
        currentState = GameState.IDLE;

        // 4. 重新生成圆盘
        SpawnAllDisks();

        // 5. 更新 UI
        UpdateStepUI();
        winText.text = "";
    }

    // ==================== UI 更新 ====================

    /// <summary>
    /// 刷新步数显示
    /// </summary>
    private void UpdateStepUI()
    {
        if (stepText != null)
        {
            stepText.text = "步数: " + stepCount;
            // ↑ 格式："步数: 0", "步数: 7" 等
        }
    }
}
